import { onMounted, onUnmounted, type Ref } from 'vue';

interface UseMarkdownEditorShortcutsOptions {
	editorElementRef?: Ref<HTMLElement | undefined>;
	onContentChange?: (content: string) => void;
}

/**
 * Composable for handling markdown editor keyboard shortcuts
 * Uses KeyboardEvent.code for physical key detection (works with any keyboard layout)
 */
export function useMarkdownEditorShortcuts({
	editorElementRef,
	onContentChange,
}: UseMarkdownEditorShortcutsOptions) {
	let textarea: HTMLTextAreaElement | null = null;

	function getTextarea(): HTMLTextAreaElement | null {
		if (textarea) return textarea;

		// Try to find textarea in editor element
		if (editorElementRef?.value) {
			const found = editorElementRef.value.querySelector('textarea');
			if (found) {
				textarea = found;
				return textarea;
			}
		}

		// Fallback: try to find active textarea in document
		const activeElement = document.activeElement;
		if (activeElement instanceof HTMLTextAreaElement) {
			textarea = activeElement;
			return textarea;
		}

		return null;
	}

	function getSelection(el: HTMLTextAreaElement): {
		start: number;
		end: number;
		text: string;
	} {
		const start = el.selectionStart;
		const end = el.selectionEnd;
		const text = el.value.substring(start, end);
		return { start, end, text };
	}

	function wrapSelection(
		el: HTMLTextAreaElement,
		prefix: string,
		suffix: string,
		callback?: (content: string) => void
	) {
		const { start, end, text } = getSelection(el);
		const content = el.value;
		const newText = text ? prefix + text + suffix : prefix + suffix;
		const newContent =
			content.substring(0, start) + newText + content.substring(end);

		if (callback) {
			callback(newContent);
		} else {
			el.value = newContent;
			const newPosition = start + (text ? newText.length : prefix.length);
			el.setSelectionRange(newPosition, newPosition);
		}
	}

	function insertAtCursor(
		el: HTMLTextAreaElement,
		insertText: string,
		callback?: (content: string) => void
	) {
		const { start, end } = getSelection(el);
		const content = el.value;
		const newContent =
			content.substring(0, start) + insertText + content.substring(end);

		if (callback) {
			callback(newContent);
		} else {
			el.value = newContent;
			const newPosition = start + insertText.length;
			el.setSelectionRange(newPosition, newPosition);
		}
	}

	function handleHeading(el: HTMLTextAreaElement, level: number) {
		const { start, end, text } = getSelection(el);
		const content = el.value;
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
			const newContent =
				content.substring(0, lineStart) + newLine + content.substring(lineEndPos);

			if (onContentChange) {
				onContentChange(newContent);
			} else {
				el.value = newContent;
				el.setSelectionRange(lineStart + newLine.length, lineStart + newLine.length);
			}
		} else if (text) {
			// Convert selection to heading
			const newText = `${prefix} ${text}`;
			const newContent =
				content.substring(0, start) + newText + content.substring(end);

			if (onContentChange) {
				onContentChange(newContent);
			} else {
				el.value = newContent;
				el.setSelectionRange(start + newText.length, start + newText.length);
			}
		} else {
			// Insert heading at cursor
			insertAtCursor(el, `${prefix} `, onContentChange);
		}
	}

	function changeHeadingLevel(
		el: HTMLTextAreaElement,
		direction: 'up' | 'down'
	) {
		const { start } = getSelection(el);
		const content = el.value;
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
		const newContent =
			content.substring(0, lineStart) + newLine + content.substring(lineEndPos);

		if (onContentChange) {
			onContentChange(newContent);
		} else {
			el.value = newContent;
			el.setSelectionRange(
				lineStart + newLine.length,
				lineStart + newLine.length
			);
		}
	}

	function handleKeyDown(e: KeyboardEvent) {
		// Only handle shortcuts when a textarea in editor is focused
		const activeElement = document.activeElement;
		if (!(activeElement instanceof HTMLTextAreaElement)) {
			return;
		}

		// Check if textarea is inside the editor
		if (editorElementRef?.value) {
			if (!editorElementRef.value.contains(activeElement)) {
				return;
			}
		}

		const isCtrl = e.ctrlKey || e.metaKey;
		const isAlt = e.altKey;

		// Ctrl+B or Cmd+B - Bold (using code for physical key, works with any layout)
		if (isCtrl && !isAlt && e.code === 'KeyB') {
			e.preventDefault();
			e.stopPropagation();
			const el = getTextarea();
			if (el) {
				wrapSelection(el, '**', '**', onContentChange);
			}
			return;
		}

		// Ctrl+I or Cmd+I - Italic
		if (isCtrl && !isAlt && e.code === 'KeyI') {
			e.preventDefault();
			e.stopPropagation();
			const el = getTextarea();
			if (el) {
				wrapSelection(el, '*', '*', onContentChange);
			}
			return;
		}

		// Ctrl+Alt+1-6 - Headings (using code for digit keys)
		if (
			isCtrl &&
			isAlt &&
			(e.code.startsWith('Digit') || e.code.startsWith('Numpad'))
		) {
			e.preventDefault();
			e.stopPropagation();
			const el = getTextarea();
			if (el) {
				let level: number;
				if (e.code.startsWith('Digit')) {
					level = parseInt(e.code.replace('Digit', ''), 10);
				} else {
					level = parseInt(e.code.replace('Numpad', ''), 10);
				}
				if (level >= 1 && level <= 6) {
					handleHeading(el, level);
				}
			}
			return;
		}

		// Ctrl+ArrowUp - Decrease heading level
		if (isCtrl && !isAlt && e.code === 'ArrowUp') {
			e.preventDefault();
			e.stopPropagation();
			const el = getTextarea();
			if (el) {
				changeHeadingLevel(el, 'up');
			}
			return;
		}

		// Ctrl+ArrowDown - Increase heading level
		if (isCtrl && !isAlt && e.code === 'ArrowDown') {
			e.preventDefault();
			e.stopPropagation();
			const el = getTextarea();
			if (el) {
				changeHeadingLevel(el, 'down');
			}
			return;
		}
	}

	onMounted(() => {
		// Use capture phase to catch events early
		document.addEventListener('keydown', handleKeyDown, true);
	});

	onUnmounted(() => {
		document.removeEventListener('keydown', handleKeyDown, true);
		textarea = null;
	});

	return {
		getTextarea,
	};
}
