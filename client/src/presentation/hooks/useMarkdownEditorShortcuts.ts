import { useEffect, RefObject } from 'react';

interface UseMarkdownEditorShortcutsProps {
  textareaRef: RefObject<HTMLTextAreaElement>;
  onContentChange: (content: string) => void;
  onUndo?: () => void;
  onRedo?: () => void;
}

/**
 * Hook for handling markdown editor keyboard shortcuts
 */
export function useMarkdownEditorShortcuts({
  textareaRef,
  onContentChange,
  onUndo,
  onRedo,
}: UseMarkdownEditorShortcutsProps) {
  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) return;

    function handleKeyDown(e: KeyboardEvent) {
      // Only handle shortcuts when textarea is focused
      if (document.activeElement !== textarea) return;

      const isCtrl = e.ctrlKey || e.metaKey;
      const isAlt = e.altKey;

      // Ctrl+B - Bold
      if (isCtrl && !isAlt && e.key === 'b') {
        e.preventDefault();
        const { start, end, text } = getSelection(textarea);
        const prefix = '**';
        const suffix = '**';
        if (text) {
          wrapSelection(textarea, prefix, suffix, onContentChange);
        } else {
          insertAtCursor(textarea, prefix + suffix, onContentChange);
          setTimeout(() => {
            textarea.selectionStart = textarea.selectionEnd = start + prefix.length;
            textarea.focus();
          }, 0);
        }
        return;
      }

      // Ctrl+I - Italic
      if (isCtrl && !isAlt && e.key === 'i') {
        e.preventDefault();
        const { start, end, text } = getSelection(textarea);
        const prefix = '*';
        const suffix = '*';
        if (text) {
          wrapSelection(textarea, prefix, suffix, onContentChange);
        } else {
          insertAtCursor(textarea, prefix + suffix, onContentChange);
          setTimeout(() => {
            textarea.selectionStart = textarea.selectionEnd = start + prefix.length;
            textarea.focus();
          }, 0);
        }
        return;
      }

      // Ctrl+Alt+1-6 - Headings
      if (isCtrl && isAlt && /^[1-6]$/.test(e.key)) {
        e.preventDefault();
        const level = parseInt(e.key);
        handleHeading(textarea, level, onContentChange);
        return;
      }

      // Ctrl+ArrowUp - Decrease heading level
      if (isCtrl && !isAlt && e.key === 'ArrowUp') {
        e.preventDefault();
        changeHeadingLevel(textarea, 'up', onContentChange);
        return;
      }

      // Ctrl+ArrowDown - Increase heading level
      if (isCtrl && !isAlt && e.key === 'ArrowDown') {
        e.preventDefault();
        changeHeadingLevel(textarea, 'down', onContentChange);
        return;
      }

      // Ctrl+Z - Undo
      if (isCtrl && !isAlt && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        onUndo?.();
        return;
      }

      // Ctrl+Shift+Z or Ctrl+Y - Redo
      if (isCtrl && ((e.shiftKey && e.key === 'z') || e.key === 'y')) {
        e.preventDefault();
        onRedo?.();
        return;
      }
    }

    textarea.addEventListener('keydown', handleKeyDown);
    return () => {
      textarea.removeEventListener('keydown', handleKeyDown);
    };
  }, [textareaRef, onContentChange, onUndo, onRedo]);
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
 * Wrap selection with markdown syntax
 */
function wrapSelection(
  textarea: HTMLTextAreaElement,
  prefix: string,
  suffix: string,
  onContentChange: (content: string) => void
) {
  const { start, end, text } = getSelection(textarea);
  const content = textarea.value;
  const newText = prefix + text + suffix;
  const newContent = content.substring(0, start) + newText + content.substring(end);
  
  onContentChange(newContent);
  
  setTimeout(() => {
    const newPosition = start + newText.length;
    textarea.selectionStart = textarea.selectionEnd = newPosition;
    textarea.focus();
  }, 0);
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

/**
 * Handle heading insertion/change
 */
function handleHeading(
  textarea: HTMLTextAreaElement,
  level: number,
  onContentChange: (content: string) => void
) {
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
    
    setTimeout(() => {
      textarea.selectionStart = textarea.selectionEnd = lineStart + newLine.length;
      textarea.focus();
    }, 0);
  } else if (text) {
    // Convert selection to heading
    const newText = `${prefix} ${text}`;
    const newContent = content.substring(0, start) + newText + content.substring(end);
    onContentChange(newContent);
    
    setTimeout(() => {
      const newPosition = start + newText.length;
      textarea.selectionStart = textarea.selectionEnd = newPosition;
      textarea.focus();
    }, 0);
  } else {
    // Insert heading at cursor
    insertAtCursor(textarea, `${prefix} `, onContentChange);
  }
}

