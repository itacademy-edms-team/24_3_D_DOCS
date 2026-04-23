import HttpClient from './HttpClient';
import type { EditorHotkeyChord } from '@/shared/constants/editorHotkeyCatalog';

export interface EditorHotkeysResponseDTO {
	bindings: Record<string, EditorHotkeyChord | null>;
}

export interface SetEditorHotkeysDTO {
	bindings: Record<string, EditorHotkeyChord | null>;
}

class EditorHotkeysAPI extends HttpClient {
	async getEditorHotkeys(): Promise<EditorHotkeysResponseDTO> {
		return this.get<EditorHotkeysResponseDTO>('/api/user/editor-hotkeys');
	}

	async putEditorHotkeys(body: SetEditorHotkeysDTO): Promise<EditorHotkeysResponseDTO> {
		return this.put<EditorHotkeysResponseDTO, SetEditorHotkeysDTO>(
			'/api/user/editor-hotkeys',
			body,
		);
	}
}

export default new EditorHotkeysAPI();
