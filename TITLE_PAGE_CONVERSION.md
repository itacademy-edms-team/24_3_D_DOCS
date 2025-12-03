# Конвертация титульных листов в PDF: Детальное описание

## Обзор системы

Система состоит из двух основных компонентов:
1. **Canvas рендеринг** (клиент) - отображение элементов на HTML5 Canvas
2. **PDF генерация** (сервер) - конвертация элементов в PDF через Puppeteer

## Структура данных

### TitlePageElement

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

**Важно**: Координаты `x` и `y` хранятся в миллиметрах (mm).

## Canvas рендеринг (TitlePageCanvas.tsx)

### Система координат

- **Размер страницы**: A4 = 210mm × 297mm
- **Конвертация**: 1mm ≈ 3.7795275591px (при 96dpi)
- Canvas имеет физический размер в пикселях, но координаты элементов в мм

### Рендеринг текста

#### Логика выравнивания в Canvas:

```javascript
// Для каждой строки текста:
lines.forEach((line, i) => {
  let textX = x; // x в пикселях (mmToPx(element.x))
  
  if (textAlign === 'center') {
    const metrics = ctx.measureText(line);
    textX = x - metrics.width / 2;  // Смещаем влево на половину ширины
    ctx.textAlign = 'left';
    ctx.fillText(line, textX, y + i * lineHeight);
  } else if (textAlign === 'right') {
    const metrics = ctx.measureText(line);
    textX = x - metrics.width;  // Смещаем влево на всю ширину
    ctx.textAlign = 'left';
    ctx.fillText(line, textX, y + i * lineHeight);
  } else {
    // left
    ctx.textAlign = 'left';
    ctx.fillText(line, x, y + i * lineHeight);
  }
});
```

**Ключевой момент**: 
- `element.x` - это **точка привязки** (anchor point):
  - `left`: левый край текста
  - `center`: центр текста
  - `right`: правый край текста

- Canvas всегда рисует с `textAlign = 'left'`, но вычисляет смещение `textX` в зависимости от выравнивания

#### Многострочный текст

- Текст разбивается по `\n`
- Каждая строка рендерится отдельно
- `lineHeight = fontSize * (element.lineHeight || 1.2)`
- Для center/right каждая строка выравнивается независимо

#### Определение границ для выделения

```javascript
// Находим максимальную ширину среди всех строк
let maxWidth = 0;
lines.forEach(line => {
  const metrics = ctx.measureText(line);
  if (metrics.width > maxWidth) maxWidth = metrics.width;
});

// Вычисляем левую границу в зависимости от выравнивания
let textLeft = el.x; // в мм
if (textAlign === 'center') {
  textLeft = el.x - maxWidth / 2;
} else if (textAlign === 'right') {
  textLeft = el.x - maxWidth;
}
```

## PDF генерация (TitlePagePdfGenerator.ts)

### Текущая реализация

#### HTML структура:

```html
<div class="element text-element" 
     style="left: Xmm; top: Ymm; transform: translateX(...); font-size: ...pt; ...">
  Текст
</div>
```

#### CSS стили:

```css
.element {
  position: absolute;
}

.text-element, .variable-element {
  white-space: pre-wrap;
  display: inline-block;
  width: fit-content;
  max-width: 100%;
}
```

#### Логика выравнивания (текущая - НЕ РАБОТАЕТ):

```typescript
private getAlignmentStyle(element: TitlePageElement): string {
  const textAlign = element.textAlign || 'left';
  
  if (textAlign === 'center') {
    return ' transform: translateX(-50%);';  // Центр элемента в точке x
  } else if (textAlign === 'right') {
    return ' transform: translateX(-100%);'; // Правый край в точке x
  }
  return '';
}
```

### Проблема

**Текущая реализация НЕ работает правильно** для center/right выравнивания.

#### Почему не работает:

1. `transform: translateX(-50%)` работает только если элемент имеет фиксированную ширину
2. Для многострочного текста `width: fit-content` может давать неправильную ширину
3. `text-align` в CSS не работает для абсолютно позиционированных элементов без ширины
4. Разные строки могут иметь разную ширину, но transform применяется ко всему блоку

#### Ожидаемое поведение (как в Canvas):

- **left**: левый край текста в точке `x`
- **center**: центр текста (по самой широкой строке) в точке `x`
- **right**: правый край текста (по самой широкой строке) в точке `x`

#### Текущее поведение в PDF:

- Текст съезжает, позиция не соответствует Canvas

## Детали координатной системы

### Единицы измерения

- **Хранение**: миллиметры (mm)
- **Canvas**: пиксели (px) с конвертацией через `MM_TO_PX = 3.7795275591`
- **PDF**: миллиметры (mm) напрямую в CSS

### Точка привязки (anchor point)

`element.x` и `element.y` определяют точку привязки элемента:

- **Для текста**:
  - `y`: верхний край первой строки
  - `x`: зависит от `textAlign`
    - `left`: левый край
    - `center`: центр (по самой широкой строке)
    - `right`: правый край (по самой широкой строке)

- **Для линии**:
  - `x`, `y`: верхний левый угол прямоугольника линии

### Многострочный текст

- Разделитель: `\n`
- Каждая строка рендерится отдельно
- `lineHeight` применяется как множитель к `fontSize`
- Для center/right выравнивания:
  - В Canvas: каждая строка выравнивается независимо относительно точки `x`
  - В PDF: нужно выровнять весь блок относительно самой широкой строки

## Параметры шрифта

### fontSize
- Единица: pt (points)
- По умолчанию: 14pt
- Используется для расчета `lineHeight`

### fontFamily
- По умолчанию: 'Times New Roman'
- Должен быть доступен в браузере и в Puppeteer

### fontWeight
- `'normal'` или `'bold'`
- По умолчанию: `'normal'`

### fontStyle
- `'normal'` или `'italic'`
- По умолчанию: `'normal'`

### lineHeight
- Множитель (без единиц)
- По умолчанию: 1.2
- Вычисление: `actualLineHeight = fontSize * lineHeight`

## Проблема с выравниванием: Детальный анализ

### Canvas (работает правильно)

```javascript
// Для center:
const metrics = ctx.measureText(line);
textX = x - metrics.width / 2;  // x в пикселях
ctx.fillText(line, textX, y);

// Для right:
const metrics = ctx.measureText(line);
textX = x - metrics.width;  // x в пикселях
ctx.fillText(line, textX, y);
```

Canvas знает точную ширину каждой строки через `measureText()`.

### PDF (не работает)

```html
<div style="left: 105mm; transform: translateX(-50%);">
  Многострочный
  текст
</div>
```

Проблемы:
1. `translateX(-50%)` работает от ширины элемента, но ширина определяется содержимым
2. Для многострочного текста ширина = ширина самой широкой строки
3. Но `fit-content` может работать некорректно в разных браузерах/Puppeteer
4. Нет точного контроля над шириной каждой строки

### Что нужно исправить

Нужно реализовать логику, которая:
1. Вычисляет ширину каждой строки текста (как в Canvas)
2. Находит максимальную ширину
3. Правильно позиционирует элемент с учетом выравнивания

**Варианты решения:**

1. **Вычислять ширину на сервере** (сложно, нужна библиотека для измерения текста)
2. **Использовать JavaScript в Puppeteer** для измерения и позиционирования
3. **Использовать CSS с правильной шириной** (нужно знать ширину заранее)
4. **Использовать таблицу/флексбокс** для выравнивания (может не работать для абсолютного позиционирования)

## Технические детали

### Puppeteer настройки

```javascript
const browser = await puppeteer.launch({
  headless: true,
  args: ['--no-sandbox', '--disable-setuid-sandbox'],
});

await page.setContent(fullHtml, { waitUntil: 'networkidle0' });

await page.pdf({
  format: 'A4',
  landscape: false,
  margin: { top: '0mm', right: '0mm', bottom: '0mm', left: '0mm' },
  printBackground: true,
});
```

### HTML структура для PDF

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    @page { size: A4; margin: 0; }
    body {
      width: 210mm;
      height: 297mm;
      position: relative;
      font-family: 'Times New Roman', Times, serif;
    }
    .element { position: absolute; }
  </style>
</head>
<body>
  <!-- Элементы -->
</body>
</html>
```

## Переменные (variables)

- Элементы типа `variable` используют `variableKey` для поиска значения в `TitlePage.variables`
- Значение подставляется при рендеринге
- Все остальные параметры (шрифт, выравнивание) работают так же, как для `text`

## Линии (lines)

- Простые прямоугольники с заданными `length` и `thickness`
- Позиционирование: `x, y` - верхний левый угол
- Не имеют проблем с выравниванием

## Резюме проблемы

**Текущая ситуация:**
- Canvas рендеринг работает правильно для всех типов выравнивания
- PDF генерация работает правильно только для `left` выравнивания
- Для `center` и `right` текст съезжает в PDF

**Причина:**
- В Canvas мы знаем точную ширину каждой строки через `measureText()`
- В PDF мы пытаемся использовать CSS `transform`, но не можем точно вычислить ширину элемента
- `width: fit-content` не гарантирует правильную ширину для многострочного текста

**Что нужно:**
- Реализовать логику, которая точно вычисляет ширину текста и правильно позиционирует элементы в PDF
- Либо использовать JavaScript в Puppeteer для измерения текста перед рендерингом
- Либо найти способ точно задать ширину элемента в CSS

