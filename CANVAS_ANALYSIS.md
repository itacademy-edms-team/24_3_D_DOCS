# Анализ Canvas листов и переменных в проектах

## Обзор

Canvas листы (титульные страницы) - это визуальные редакторы для создания титульных страниц документов. Они используют HTML5 Canvas для отрисовки элементов на странице формата A4 (210mm × 297mm).

## Что такое Canvas листы (Title Pages)?

**Canvas листы** - это интерактивные редакторы титульных страниц, которые:
- Используют HTML5 Canvas для визуализации элементов
- Работают в системе координат миллиметров (mm)
- Поддерживают три типа элементов: `text`, `variable`, `line`
- Позволяют создавать шаблоны титульных страниц с переменными

## Структура данных

### TitlePageElement

Элемент титульной страницы может быть одного из трех типов:

```typescript
interface TitlePageElement {
  id: string;
  type: 'text' | 'variable' | 'line';
  x: number;  // мм - координата X точки привязки
  y: number;  // мм - координата Y верхнего края
  
  // Для text/variable:
  content?: string;           // Текст (для type='text')
  variableKey?: string;       // Ключ переменной (для type='variable')
  fontSize?: number;          // Размер шрифта в pt
  fontFamily?: string;        // Название шрифта
  fontWeight?: 'normal' | 'bold';
  fontStyle?: 'normal' | 'italic';
  lineHeight?: number;        // Множитель междустрочного интервала
  textAlign?: 'left' | 'center' | 'right';
  
  // Для line:
  length?: number;    // мм - длина линии
  thickness?: number; // мм - толщина линии
}
```

### TitlePage

```typescript
interface TitlePage {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  elements: TitlePageElement[];      // Элементы на странице
  variables: Record<string, string>; // Переменные для подстановки
}
```

## Что такое переменные (variables)?

**Переменные** - это ключ-значение пары, которые используются для динамической подстановки текста в элементы типа `variable`.

### Как работают переменные:

1. **В редакторе Canvas**: Элементы типа `variable` отображаются как `{variableKey}`
   - Например, если `variableKey = "institute"`, на Canvas будет показано `{institute}`

2. **Хранение**: Переменные хранятся в `TitlePage.variables` как объект:
   ```typescript
   {
     "institute": "СФУ",
     "department": "Кафедра информатики",
     "title": "Курсовая работа",
     ...
   }
   ```

3. **Использование в элементах**: Элемент с `type: 'variable'` и `variableKey: 'institute'` будет подставлять значение из `variables['institute']`

4. **Приоритет переменных**:
   - Переменные из документа (frontmatter) имеют приоритет
   - Затем используются переменные из титульного листа (`TitlePage.variables`)
   - Если переменная не найдена, используется пустая строка

### Пример использования:

```json
{
  "type": "variable",
  "variableKey": "institute",
  "x": 116.28,
  "y": 51.30,
  "fontSize": 14,
  "fontFamily": "Times New Roman",
  "textAlign": "center"
}
```

При рендеринге в HTML/PDF значение `variables['institute']` будет подставлено вместо `{institute}`.

## Сравнение проектов

### ReportLab site (старый проект)

**Хранение данных:**
- Файловая система: `data/title-pages/{id}/`
  - `meta.json` - метаданные (id, name, createdAt, updatedAt)
  - `canvas-data.json` - элементы и переменные вместе
- Без БД для титульных страниц

**Структура canvas-data.json:**
```json
{
  "elements": [...],
  "variables": {...}
}
```

### 24_3_D_DOCS (новый проект)

**Хранение данных:**
- **БД (PostgreSQL)**: Метаданные (id, creator_id, updated_at)
- **MinIO**: Файлы в структуре `user-{userId}/TitlePage/{id}/`
  - `meta.json` - метаданные
  - `elements.json` - элементы
  - `variables.json` - переменные отдельно
- **Авторизация**: Каждый пользователь имеет свой bucket в MinIO

**Преимущества новой архитектуры:**
1. ✅ Раздельное хранение элементов и переменных
2. ✅ Авторизация и изоляция данных пользователей
3. ✅ Масштабируемость через MinIO
4. ✅ Метаданные в БД для быстрого поиска

## Canvas рендеринг

### Система координат

- **Размер страницы**: A4 = 210mm × 297mm
- **Конвертация**: 1mm ≈ 3.7795275591px (при 96dpi)
- **Хранение координат**: в миллиметрах (mm)
- **Canvas отрисовка**: конвертация mm → px для Canvas API

### Рендеринг элементов

**На Canvas (редактор):**
- `text`: отображается как есть (из `content`)
- `variable`: отображается как `{variableKey}` (например, `{institute}`)
- `line`: прямоугольник с заданными длиной и толщиной

**В HTML/PDF (финальный рендеринг):**
- `text`: отображается как есть
- `variable`: подставляется значение из `variables[variableKey]`
- `line`: прямоугольник

### Логика выравнивания текста

Canvas всегда рисует с `textAlign = 'left'`, но вычисляет смещение:

```javascript
// Для center:
const metrics = ctx.measureText(line);
textX = x - metrics.width / 2;  // Смещение влево на половину ширины

// Для right:
const metrics = ctx.measureText(line);
textX = x - metrics.width;  // Смещение влево на всю ширину

// Для left:
textX = x;  // Без смещения
```

## Различия в реализации Canvas

### Реализация Canvas идентична в обоих проектах:

1. **Отрисовка элементов** (`canvasRenderer.ts`):
   - Одинаковая логика рендеринга text/variable/line
   - Одинаковая система координат (mm → px)
   - Одинаковая логика выравнивания текста

2. **Отображение переменных на Canvas**:
   ```typescript
   const content = element.type === 'variable' 
     ? `{${element.variableKey || ''}}`
     : element.content || '';
   ```

3. **Интерактивность**:
   - Одинаковые возможности: перемещение, выбор, изменение размеров
   - Одинаковая система линеек и сетки

### Единственные различия:

1. **Фреймворк**:
   - ReportLab site: React + TypeScript
   - 24_3_D_DOCS: Vue 3 + TypeScript

2. **Организация кода**:
   - ReportLab site: более простая структура
   - 24_3_D_DOCS: более модульная структура (widgets, shared)

3. **Хранение данных**:
   - ReportLab site: файловая система
   - 24_3_D_DOCS: MinIO + БД

## Резюме

### Canvas листы:
- ✅ Работают одинаково в обоих проектах
- ✅ Используют HTML5 Canvas для визуализации
- ✅ Система координат в миллиметрах (A4 = 210mm × 297mm)
- ✅ Три типа элементов: text, variable, line

### Переменные:
- ✅ Хранятся в `TitlePage.variables` как `Record<string, string>`
- ✅ Используются элементами типа `variable` через `variableKey`
- ✅ На Canvas отображаются как `{variableKey}`
- ✅ В HTML/PDF подставляются значения из `variables[variableKey]`
- ✅ Переменные из документа имеют приоритет над переменными титульного листа

### Основное различие:
**Архитектура хранения данных:**
- Старый проект: файловая система, все в одном файле
- Новый проект: MinIO + БД, раздельное хранение, авторизация
