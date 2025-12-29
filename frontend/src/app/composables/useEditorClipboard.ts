/**
 * Composable for getting selected text and line numbers from editor
 * Simple approach: find editor on page and get selection
 */

export interface EditorSelection {
	startLine: number;
	endLine: number;
	selectedText: string;
}

/**
 * Get selected text and line numbers from CodeMirror editor
 * Finds editor on page and extracts selection
 */
export function getEditorSelection(): EditorSelection | null {
	try {
		// Find markdown editor container on page
		const editorContainer = document.querySelector('.markdown-editor') as HTMLElement;
		if (!editorContainer) return null;

		// Find CodeMirror editor instance
		const cmEditor = editorContainer.querySelector('.cm-editor');
		if (!cmEditor) return null;

		// Try multiple ways to access CodeMirror view
		// @ts-ignore - accessing internal CodeMirror API
		let view = (cmEditor as any).cmView?.view;
		
		// Alternative: try accessing via __cm_view property
		if (!view) {
			// @ts-ignore
			view = (cmEditor as any).__cm_view;
		}
		
		if (!view) return null;

		// Get current selection
		const selection = view.state.selection.main;
		if (!selection || selection.empty) return null;

		// Get document
		const doc = view.state.doc;

		// Calculate line numbers (1-based)
		const startLine = doc.lineAt(selection.from).number;
		const endLine = doc.lineAt(selection.to).number;

		// Get selected text from CodeMirror
		const selectedText = doc.sliceString(selection.from, selection.to);

		if (!selectedText.trim()) return null;

		return {
			startLine,
			endLine,
			selectedText,
		};
	} catch (error) {
		console.error('Error getting editor selection:', error);
		return null;
	}
}
