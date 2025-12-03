import { RefObject } from 'react';

interface MarkdownFormattingToolbarProps {
  textareaRef: RefObject<HTMLTextAreaElement>;
  onContentChange: (content: string) => void;
  onOpenVariablesEditor?: () => void;
}

/**
 * Get selection range from textarea
 */
function getSelection(textarea: HTMLTextAreaElement): { start: number; end: number; text: string } {
  const start = textarea.selectionStart;
  const end = textarea.selectionEnd;
  const text = textarea.value.substring(start, end);
  return { start, end, text };
}

/**
 * Replace selection in textarea
 */
function replaceSelection(
  textarea: HTMLTextAreaElement,
  newText: string,
  onContentChange: (content: string) => void
) {
  const { start, end } = getSelection(textarea);
  const content = textarea.value;
  const newContent = content.substring(0, start) + newText + content.substring(end);
  
  onContentChange(newContent);
  
  // Restore cursor position
  setTimeout(() => {
    const newPosition = start + newText.length;
    textarea.selectionStart = textarea.selectionEnd = newPosition;
    textarea.focus();
  }, 0);
}

/**
 * Wrap selection with markdown syntax
 */
function wrapSelection(
  textarea: HTMLTextAreaElement,
  prefix: string,
  suffix: string,
  onContentChange: (content: string) => void
) {
  const { start, end, text } = getSelection(textarea);
  
  if (text) {
    // Wrap selected text
    const newText = prefix + text + suffix;
    replaceSelection(textarea, newText, onContentChange);
  } else {
    // Insert at cursor position
    const content = textarea.value;
    const newContent = content.substring(0, start) + prefix + suffix + content.substring(end);
    onContentChange(newContent);
    
    setTimeout(() => {
      textarea.selectionStart = textarea.selectionEnd = start + prefix.length;
      textarea.focus();
    }, 0);
  }
}

/**
 * Insert text at cursor position
 */
function insertAtCursor(
  textarea: HTMLTextAreaElement,
  text: string,
  onContentChange: (content: string) => void
) {
  const { start, end } = getSelection(textarea);
  const content = textarea.value;
  const newContent = content.substring(0, start) + text + content.substring(end);
  
  onContentChange(newContent);
  
  setTimeout(() => {
    const newPosition = start + text.length;
    textarea.selectionStart = textarea.selectionEnd = newPosition;
    textarea.focus();
  }, 0);
}

/**
 * Detect heading level at current line
 */
function detectHeadingLevel(textarea: HTMLTextAreaElement): number | null {
  const { start } = getSelection(textarea);
  const content = textarea.value;
  const lineStart = content.lastIndexOf('\n', start - 1) + 1;
  const line = content.substring(lineStart, content.indexOf('\n', lineStart) || content.length);
  
  const match = line.match(/^(#{1,6})\s/);
  return match ? match[1].length : null;
}

/**
 * Change heading level (increase or decrease)
 */
function changeHeadingLevel(
  textarea: HTMLTextAreaElement,
  direction: 'up' | 'down',
  onContentChange: (content: string) => void
) {
  const { start, end } = getSelection(textarea);
  const content = textarea.value;
  const lineStart = content.lastIndexOf('\n', start - 1) + 1;
  const lineEnd = content.indexOf('\n', lineStart);
  const lineEndPos = lineEnd === -1 ? content.length : lineEnd;
  const line = content.substring(lineStart, lineEndPos);
  
  const match = line.match(/^(#{1,6})\s(.+)$/);
  if (!match) return;
  
  const currentLevel = match[1].length;
  const headingText = match[2];
  
  let newLevel: number;
  if (direction === 'up') {
    newLevel = Math.max(1, currentLevel - 1);
  } else {
    newLevel = Math.min(6, currentLevel + 1);
  }
  
  const newPrefix = '#'.repeat(newLevel);
  const newLine = `${newPrefix} ${headingText}`;
  const newContent = content.substring(0, lineStart) + newLine + content.substring(lineEndPos);
  
  onContentChange(newContent);
  
  setTimeout(() => {
    textarea.selectionStart = textarea.selectionEnd = lineStart + newLine.length;
    textarea.focus();
  }, 0);
}

export function MarkdownFormattingToolbar({
  textareaRef,
  onContentChange,
  onOpenVariablesEditor,
}: MarkdownFormattingToolbarProps) {
  function handleBold() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    wrapSelection(textarea, '**', '**', onContentChange);
  }

  function handleItalic() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    wrapSelection(textarea, '*', '*', onContentChange);
  }

  function handleHeading(level: number) {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const { start, end, text } = getSelection(textarea);
    const content = textarea.value;
    const lineStart = content.lastIndexOf('\n', start - 1) + 1;
    const lineEnd = content.indexOf('\n', lineStart);
    const lineEndPos = lineEnd === -1 ? content.length : lineEnd;
    const line = content.substring(lineStart, lineEndPos);
    
    const prefix = '#'.repeat(level);
    
    // Check if line is already a heading
    const headingMatch = line.match(/^(#{1,6})\s(.+)$/);
    if (headingMatch) {
      // Replace existing heading
      const newLine = `${prefix} ${headingMatch[2]}`;
      const newContent = content.substring(0, lineStart) + newLine + content.substring(lineEndPos);
      onContentChange(newContent);
    } else if (text) {
      // Convert selection to heading
      const newText = `${prefix} ${text}`;
      replaceSelection(textarea, newText, onContentChange);
    } else {
      // Insert heading at cursor
      insertAtCursor(textarea, `${prefix} `, onContentChange);
    }
  }

  function handleIncreaseHeading() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    changeHeadingLevel(textarea, 'down', onContentChange);
  }

  function handleDecreaseHeading() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    changeHeadingLevel(textarea, 'up', onContentChange);
  }

  function handleUnorderedList() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const { start, end, text } = getSelection(textarea);
    if (text) {
      // Convert each line to list item
      const lines = text.split('\n');
      const listItems = lines.map(line => line.trim() ? `- ${line.trim()}` : '').join('\n');
      replaceSelection(textarea, listItems, onContentChange);
    } else {
      insertAtCursor(textarea, '- ', onContentChange);
    }
  }

  function handleOrderedList() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const { start, end, text } = getSelection(textarea);
    if (text) {
      // Convert each line to numbered list item
      const lines = text.split('\n').filter(line => line.trim());
      const listItems = lines.map((line, index) => `${index + 1}. ${line.trim()}`).join('\n');
      replaceSelection(textarea, listItems, onContentChange);
    } else {
      insertAtCursor(textarea, '1. ', onContentChange);
    }
  }

  function handleTaskList() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const { start, end, text } = getSelection(textarea);
    if (text) {
      // Convert each line to task list item
      const lines = text.split('\n');
      const listItems = lines.map(line => line.trim() ? `- [ ] ${line.trim()}` : '').join('\n');
      replaceSelection(textarea, listItems, onContentChange);
    } else {
      insertAtCursor(textarea, '- [ ] ', onContentChange);
    }
  }

  function handleTable() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const tableTemplate = `| Заголовок 1 | Заголовок 2 | Заголовок 3 |
|-------------|-------------|-------------|
| Ячейка 1    | Ячейка 2    | Ячейка 3    |
| Ячейка 4    | Ячейка 5    | Ячейка 6    |`;
    
    insertAtCursor(textarea, '\n' + tableTemplate + '\n', onContentChange);
  }

  function handleLink() {
    const textarea = textareaRef.current;
    if (!textarea) return;
    
    const { start, end, text } = getSelection(textarea);
    if (text) {
      // Convert selection to link
      const linkText = `[${text}](url)`;
      replaceSelection(textarea, linkText, onContentChange);
      
      // Select "url" part
      setTimeout(() => {
        const urlStart = start + text.length + 3; // [text](
        const urlEnd = urlStart + 3; // url
        textarea.selectionStart = urlStart;
        textarea.selectionEnd = urlEnd;
        textarea.focus();
      }, 0);
    } else {
      insertAtCursor(textarea, '[текст ссылки](url)', onContentChange);
      
      // Select "текст ссылки" part
      setTimeout(() => {
        const textStart = start + 1; // [
        const textEnd = textStart + 13; // текст ссылки
        textarea.selectionStart = textStart;
        textarea.selectionEnd = textEnd;
        textarea.focus();
      }, 0);
    }
  }

  return (
    <div className="toolbar-group" style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}>
      {/* Text formatting */}
      <div style={{ display: 'flex', gap: '0.25rem', paddingRight: '0.5rem', borderRight: '1px solid var(--border-color)' }}>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleBold}
          title="Жирный (Ctrl+B)"
          style={{ padding: '0.25rem 0.5rem', fontWeight: 'bold' }}
        >
          <strong>B</strong>
        </button>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleItalic}
          title="Курсив (Ctrl+I)"
          style={{ padding: '0.25rem 0.5rem', fontStyle: 'italic' }}
        >
          <em>I</em>
        </button>
      </div>

      {/* Structure */}
      <div style={{ display: 'flex', gap: '0.25rem', paddingRight: '0.5rem', borderRight: '1px solid var(--border-color)' }}>
        <select
          className="btn btn-ghost btn-sm"
          onChange={(e) => {
            const level = parseInt(e.target.value);
            if (level > 0) handleHeading(level);
            e.target.value = '';
          }}
          title="Заголовки (Ctrl+Alt+1-6)"
          style={{
            padding: '0.25rem 0.5rem',
            fontSize: '0.875rem',
            background: 'transparent',
            border: '1px solid transparent',
            borderRadius: 'var(--radius-sm)',
            cursor: 'pointer',
          }}
        >
          <option value="">Заголовок</option>
          <option value="1">H1</option>
          <option value="2">H2</option>
          <option value="3">H3</option>
          <option value="4">H4</option>
          <option value="5">H5</option>
          <option value="6">H6</option>
        </select>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleDecreaseHeading}
          title="Уменьшить заголовок (Ctrl+↑)"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          ↑
        </button>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleIncreaseHeading}
          title="Увеличить заголовок (Ctrl+↓)"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          ↓
        </button>
      </div>

      {/* Lists */}
      <div style={{ display: 'flex', gap: '0.25rem', paddingRight: '0.5rem', borderRight: '1px solid var(--border-color)' }}>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleUnorderedList}
          title="Маркированный список"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          •
        </button>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleOrderedList}
          title="Нумерованный список"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          1.
        </button>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleTaskList}
          title="Список задач"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          ☐
        </button>
      </div>

      {/* Insert */}
      <div style={{ display: 'flex', gap: '0.25rem', paddingRight: '0.5rem', borderRight: '1px solid var(--border-color)' }}>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleTable}
          title="Вставить таблицу"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          ⧉
        </button>
        <button
          className="btn btn-ghost btn-sm"
          onClick={handleLink}
          title="Преобразовать в ссылку"
          style={{ padding: '0.25rem 0.5rem' }}
        >
          🔗
        </button>
      </div>

      {/* Variables */}
      {onOpenVariablesEditor && (
        <div style={{ display: 'flex', gap: '0.25rem' }}>
          <button
            className="btn btn-ghost btn-sm"
            onClick={onOpenVariablesEditor}
            title="Редактировать переменные для титульного листа"
            style={{ padding: '0.25rem 0.5rem' }}
          >
            📝 Переменные
          </button>
        </div>
      )}
    </div>
  );
}



