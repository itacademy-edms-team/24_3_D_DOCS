using System.Linq;
using RusalProject.Models.DTOs.Agent;
using RusalProject.Models.DTOs.Chat;
using RusalProject.Services.Agent.Core;
using RusalProject.Services.Agent.Tools.DocumentTools;
using RusalProject.Services.Chat;
using RusalProject.Services.Ollama;

namespace RusalProject.Services.Agent;

public class DocumentAgent : IDocumentAgent
{
    private const int MaxToolIterations = 16;

    private readonly IChatService _chatService;
    private readonly IAgentAttachmentContextService _attachmentContext;
    private readonly AgentLoopRunner _runner;
    private readonly IReadOnlyList<IAgentTool> _tools;

    public DocumentAgent(
        IChatService chatService,
        IAgentAttachmentContextService attachmentContext,
        AgentLoopRunner runner,
        ReadDocumentTool readDocumentTool,
        ProposeInsertTool proposeInsertTool,
        ProposeDeleteTool proposeDeleteTool,
        ProposeReplaceTool proposeReplaceTool,
        QueryAttachmentTextTool queryAttachmentTextTool,
        QueryAttachmentImageTool queryAttachmentImageTool)
    {
        _chatService = chatService;
        _attachmentContext = attachmentContext;
        _runner = runner;
        _tools = new IAgentTool[]
        {
            readDocumentTool,
            proposeInsertTool,
            proposeDeleteTool,
            proposeReplaceTool,
            queryAttachmentTextTool,
            queryAttachmentImageTool
        };
    }

    public async Task<AgentResponseDTO> RunAsync(
        AgentRequestDTO request,
        Guid userId,
        Func<AgentStepDTO, Task>? onStepUpdate = null,
        Func<int, Task>? onDocumentChange = null,
        Func<string, Task>? onStatusCheck = null,
        CancellationToken cancellationToken = default)
    {
        if (!request.ChatId.HasValue)
            throw new InvalidOperationException("ChatId обязателен.");
        if (!request.DocumentId.HasValue)
            throw new InvalidOperationException("DocumentId обязателен для Document Agent.");

        var chat = await _chatService.GetChatByIdAsync(request.ChatId.Value, userId)
            ?? throw new InvalidOperationException("Чат не найден.");

        var messages = chat.Messages.OrderBy(x => x.CreatedAt).ToList();

        var history = messages
            .Where(m => !m.StepNumber.HasValue && m.Role is "user" or "assistant" or "system")
            .Select(m => new OllamaMessageInput
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();

        var lastUserMessage = history.LastOrDefault(m => m.Role == "user")?.Content?.Trim();
        var currentUserMessage = request.UserMessage?.Trim();
        if (!string.IsNullOrWhiteSpace(currentUserMessage) &&
            !string.Equals(lastUserMessage, currentUserMessage, StringComparison.Ordinal))
        {
            history.Add(new OllamaMessageInput
            {
                Role = "user",
                Content = currentUserMessage
            });
        }

        var sourceSessionIdForContext = await _attachmentContext.ResolveAndInjectCatalogAsync(
            userId,
            request.ChatId.Value,
            AgentAttachmentContextScope.Document,
            request.DocumentId,
            request.SourceSessionId,
            messages,
            history,
            cancellationToken);

        var loopResult = await _runner.RunAsync(
            new AgentExecutionContext
            {
                UserId = userId,
                DocumentId = request.DocumentId.Value,
                ChatSessionId = request.ChatId.Value,
                SourceSessionId = sourceSessionIdForContext
            },
            GetSystemPrompt(),
            _tools,
            history,
            MaxToolIterations,
            onStepUpdate,
            onStatusCheck,
            cancellationToken);

        var finalMessage = loopResult.FinalMessage;
        if (string.IsNullOrWhiteSpace(finalMessage))
        {
            finalMessage = loopResult.Steps.Any(step => step.DocumentChanges is { Count: > 0 })
                ? "Готово! Изменения предложены. Проверьте их и примите или отклоните."
                : "Готово.";
        }

        await _chatService.AddMessageAsync(request.ChatId.Value, new ChatMessageDTO
        {
            Role = "assistant",
            Content = finalMessage
        }, userId);

        if (loopResult.Steps.Count > 0 && onStatusCheck != null && !string.IsNullOrWhiteSpace(finalMessage))
        {
            await onStatusCheck("\n\n");
            await onStatusCheck(finalMessage);
        }

        return new AgentResponseDTO
        {
            FinalMessage = finalMessage,
            Steps = loopResult.Steps,
            IsComplete = true
        };
    }

    private static string GetSystemPrompt()
    {
        return """
IMPORTANT: Отвечай пользователю на русском языке.

Ты агент одного markdown-документа.

Ты умеешь:
- читать документ по диапазону строк;
- предлагать вставку нового контента;
- предлагать удаление диапазона строк;
- предлагать замену диапазона строк;
- при наличии вложения в чате: query_attachment_text (вопрос по текстовой части) и query_attachment_image (вопрос по картинке из вложения).

Правила:
- Если в сообщениях есть блок «[Контекст вложений…]», сначала ориентируйся на индексы частей; для вопросов к тексту вложения вызывай query_attachment_text(part_index, question), для изображений — query_attachment_image(part_index, question).
- Ты работаешь только с содержимым текущего документа.
- Не создавай и не удаляй документы.
- Если контекст документа непонятен, сначала используй read_document.
- Не применяй изменения окончательно: только предлагай их через propose_insert, propose_delete и propose_replace.
- Если пользователь просит написать, добавить, заполнить, составить, создать внутри документа отчёт, план, статью, раздел, список или любой новый контент, ты обязан изменить документ через инструменты, а не отвечать только сообщением в чат.
- Если документ пустой и нужно создать содержимое с нуля, сразу используй propose_insert со start_line = 0.
- Если задача явно требует правки документа, ответ без вызова инструмента недопустим.
- Для больших изменений разбивай задачу по сущностям документа, а не одним огромным блоком.
- Если пользователь просто здоровается или задаёт вопрос без необходимости менять документ, ответь коротко без вызова инструментов.
- Когда задача закончена, дай короткий итоговый ответ.

Формат markdown для этого редактора (соблюдай при любом content в propose_insert / propose_replace):

Заголовки:
- Крупные разделы задач: ## (например «Задача №23»).
- Подразделы внутри задачи: ### (например «Условие», «Решение», «Вывод», заголовок сводной таблицы).
- Подпункты решения: ####.

Пустые строки и блоки:
- Между соседними логическими блоками (заголовок, абзац, таблица целиком, отдельный блок display-формулы, список) оставляй минимум одну пустую строку.
- Между крупными разделами (например конец одной задачи и начало следующей) — две пустые строки.
- Не слепляй заголовок с таблицей или списком без пустой строки, если иначе разметка «прилипает».

Запрет вложенной и ломающей разметки:
- Не ставь *, _, `, $ внутри одного слова (недопустимо: с*лов*о).
- Жирный и курсив (** **, * * или _ _) — только вокруг целого слова или целой фразы, целиком: **693 мг/м³**, **5 л/с**.
- Не вкладывай акценты друг в друга (избегай **...*...*, ***...).
- Внутри формул KaTeX акценты markdown не используй; выделяй текст командой \text{...}, при необходимости \mathbf{...}.

Таблицы (GitHub-стиль):
- Первая строка — заголовки ячеек, вторая — разделитель |---|---| (столько же столбцов).
- Формулы в ячейках — только через inline $...$.

Формулы (KaTeX):
- Inline: $...$ или \(...\) — без переноса строки внутри пары разделителей.
- Display: $$...$$ или \[...\] — на отдельных строках; многострочное содержимое внутри допустимо.
- Единицы и поясняющий текст в формулах: \text{ м}^3, \text{ л/с}, \text{ МПа} и т.п.
- Индексы и нижние индексы: V_г, C_{НКПР}, \rho и т.д.; проверяй баланс скобок { } в LaTeX.

Списки:
- Маркеры: - с пробелом; подпункты с отступом.
- Перед списком добавь пустую строку, если он идёт сразу после абзаца и иначе сливается с текстом.

Код:
- Блок: три обратные кавычки в отдельной строке, идентификатор языка (например haskell, python, csharp), код, закрывающие три кавычки; для нейтрального текста — text или plaintext.
- Инлайн-код: одинарные ` вокруг целого фрагмента, не разрывая слово.

Подписи (отдельная строка, с пустыми строками как у прочих блоков):
- После таблицы: [TABLE-CAPTION: текст подписи]
- После блока display-формулы $$...$$ (следующей строкой под формулой): [FORMULA-CAPTION: текст подписи]
- После markdown-изображения ![alt](url): [IMAGE-CAPTION: текст подписи]

Инструменты:
- В propose_insert / propose_replace передавай готовый фрагмент исходного markdown, как он должен лежать в файле.
- Номера строк для вставки и замены бери из вывода read_document.

Мини-пример структуры (ориентир, не копируй слепо):

### Сводная таблица исходных данных (Вариант №N)

|Задача|Параметр|Обозначение|Значение|
|---|---|---|---|
|**№23**|Вещество (ЛВЖ)|-|Ацетон ($C_3H_6O$)|
||Объем паров|$V_г$|$40 \text{ м}^3$|
||Объем помещения|$V$|$1600 \text{ м}^3$|

[TABLE-CAPTION: Исходные данные для расчётов по варианту]

## Задача №23

### Условие

При заданных $V_г$, м$^3$, и $V$, м$^3$, рассчитать величину $C_{НКПР}$, %.

### Решение

#### Расчёт коэффициента ($\beta$)

$$\beta = n_C + \frac{n_H - n_X}{4} - \frac{n_O}{2}$$

[FORMULA-CAPTION: Формула стехиометрического коэффициента]

#### Список допущений

- Первое допущение с числом **5 л/с**.
- Второе: значение $\rho$ взято из справочника.

Пример кода (язык на той же строке, что и открывающие кавычки):

``` Haskell
 дарова
```

Инлайн-код: вызов функции `main`.

![Схема объекта](https://example.com/scheme.png)

[IMAGE-CAPTION: Рис. 1 — условное обозначение]

### Вывод

Итоговое значение **2.46%**; объём смеси **1626 м³** превышает допустимый запас.
""";
    }
}
