import { useEffect, useCallback, useRef } from 'react';
import type { TitlePageElementType } from '../../../../shared/src/types';

interface UseKeyboardShortcutsProps {
  selectedElementIds: string[];
  currentTool: TitlePageElementType | null;
  onToolChange: (tool: TitlePageElementType | null) => void;
  onElementDelete: () => void;
  onElementDeselect: () => void;
  onElementMove: (deltaX: number, deltaY: number) => void;
  onCopy?: () => void;
  onCut?: () => void;
  onPaste?: () => void;
  onUndo?: () => void;
  onRedo?: () => void;
  titlePage: { elements: Array<{ id: string; x: number; y: number }> } | null;
}

export function useKeyboardShortcuts({
  selectedElementIds,
  currentTool,
  onToolChange,
  onElementDelete,
  onElementDeselect,
  onElementMove,
  onCopy,
  onCut,
  onPaste,
  onUndo,
  onRedo,
  titlePage,
}: UseKeyboardShortcutsProps) {
  // Use refs to avoid recreating the handler on every render
  const callbacksRef = useRef({ onToolChange, onElementDelete, onElementDeselect, onElementMove, onCopy, onCut, onPaste, onUndo, onRedo });
  
  useEffect(() => {
    callbacksRef.current = { onToolChange, onElementDelete, onElementDeselect, onElementMove, onCopy, onCut, onPaste, onUndo, onRedo };
  }, [onToolChange, onElementDelete, onElementDeselect, onElementMove, onCopy, onCut, onPaste, onUndo, onRedo]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't handle shortcuts when user is typing in an input/textarea/select
      const target = e.target as HTMLElement;
      const tagName = target.tagName?.toUpperCase();
      const isEditable = target.isContentEditable;
      const isInput = tagName === 'INPUT' || tagName === 'TEXTAREA' || tagName === 'SELECT';
      
      // Also check if the target is inside an input/textarea/select
      const isInsideEditable = target.closest('input, textarea, select, [contenteditable="true"]');
      
      if (isInput || isEditable || isInsideEditable) {
        return;
      }

      const { onToolChange, onElementDelete, onElementDeselect, onElementMove, onCopy, onCut, onPaste, onUndo, onRedo } = callbacksRef.current;

      // Use e.code for physical key detection (works with any keyboard layout)
      // Undo/Redo operations
      if ((e.ctrlKey || e.metaKey) && (e.code === 'KeyZ' || e.key === 'z' || e.key === 'я' || e.key === 'Я') && !e.shiftKey) {
        e.preventDefault();
        e.stopPropagation();
        onUndo?.();
        return;
      }

      if ((e.ctrlKey || e.metaKey) && (e.code === 'KeyY' || e.code === 'KeyZ' || e.key === 'y' || e.key === 'н' || e.key === 'Н' || (e.key === 'z' && e.shiftKey) || (e.key === 'я' && e.shiftKey))) {
        if (e.shiftKey && (e.code === 'KeyZ' || e.key === 'z' || e.key === 'я')) {
          e.preventDefault();
          e.stopPropagation();
          onRedo?.();
          return;
        } else if (e.code === 'KeyY' || e.key === 'y' || e.key === 'н') {
          e.preventDefault();
          e.stopPropagation();
          onRedo?.();
          return;
        }
      }

      // Clipboard operations - use e.code for physical key detection
      if ((e.ctrlKey || e.metaKey) && (e.code === 'KeyC' || e.key === 'c' || e.key === 'с' || e.key === 'С') && selectedElementIds.length > 0) {
        e.preventDefault();
        e.stopPropagation();
        onCopy?.();
        return;
      }

      if ((e.ctrlKey || e.metaKey) && (e.code === 'KeyX' || e.key === 'x' || e.key === 'ч' || e.key === 'Ч') && selectedElementIds.length > 0) {
        e.preventDefault();
        e.stopPropagation();
        onCut?.();
        return;
      }

      if ((e.ctrlKey || e.metaKey) && (e.code === 'KeyV' || e.key === 'v' || e.key === 'м' || e.key === 'М')) {
        e.preventDefault();
        e.stopPropagation();
        onPaste?.();
        return;
      }

      // Tool shortcuts
      if (e.key === 'v' || e.key === 'V') {
        e.preventDefault();
        e.stopPropagation();
        onToolChange(null);
      } else if (e.key === 't' || e.key === 'T') {
        e.preventDefault();
        e.stopPropagation();
        onToolChange(currentTool === 'text' ? null : 'text');
      } else if (e.key === 'l' || e.key === 'L') {
        e.preventDefault();
        e.stopPropagation();
        onToolChange(currentTool === 'line' ? null : 'line');
      }
      // Delete/Backspace - delete selected elements
      else if ((e.key === 'Delete' || e.key === 'Backspace') && selectedElementIds.length > 0) {
        e.preventDefault();
        e.stopPropagation();
        onElementDelete();
      }
      // Esc - deselect
      else if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        onElementDeselect();
        onToolChange(null);
      }
      // Arrow keys - move selected elements
      else if (selectedElementIds.length > 0 && titlePage) {
        const moveAmount = e.shiftKey ? 10 : 1; // Shift = 10mm, normal = 1mm
        let deltaX = 0;
        let deltaY = 0;

        if (e.key === 'ArrowLeft') {
          e.preventDefault();
          e.stopPropagation();
          deltaX = -moveAmount;
        } else if (e.key === 'ArrowRight') {
          e.preventDefault();
          e.stopPropagation();
          deltaX = moveAmount;
        } else if (e.key === 'ArrowUp') {
          e.preventDefault();
          e.stopPropagation();
          deltaY = -moveAmount;
        } else if (e.key === 'ArrowDown') {
          e.preventDefault();
          e.stopPropagation();
          deltaY = moveAmount;
        }

        if (deltaX !== 0 || deltaY !== 0) {
          onElementMove(deltaX, deltaY);
        }
      }
    };

    // Use capture phase to catch events earlier
    window.addEventListener('keydown', handleKeyDown, true);
    return () => {
      window.removeEventListener('keydown', handleKeyDown, true);
    };
  }, [selectedElementIds, titlePage, currentTool]);
}

