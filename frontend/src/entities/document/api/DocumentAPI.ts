import HttpClient from '@/shared/api/HttpClient';
import type {
	Document,
	DocumentMeta,
	CreateDocumentDTO,
	UpdateDocumentDTO,
	UpdateDocumentContentDTO,
	UpdateDocumentOverridesDTO,
	DocumentMetadata,
	TocItem,
	DocumentVersion,
	SaveDocumentVersionDTO,
} from '../types';

export interface GeneratePdfRequest {
	/** Титульник в PDF и вложение titlepage.json (если включён титульник). */
	titlePageId?: string;
	/** Профиль для вёрстки PDF и profile.json при включённом профиле. */
	profileId?: string;
	includeDocument?: boolean;
	includeStyleProfile?: boolean;
	includeTitlePage?: boolean;
}

export interface PdfImportBundleParts {
	hasDocument: boolean;
	documentSize: number;
	hasStyleProfile: boolean;
	styleProfileSize: number;
	hasTitlePage: boolean;
	titlePageSize: number;
}

class DocumentAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(status?: string, search?: string): Promise<DocumentMeta[]> {
		const params = new URLSearchParams();
		if (status) params.append('status', status);
		if (search) params.append('search', search);
		const query = params.toString();
		return this.get<DocumentMeta[]>(`/api/documents${query ? `?${query}` : ''}`);
	}

	async getById(id: string): Promise<Document> {
		return this.get<Document>(`/api/documents/${id}`);
	}

	async create(data: CreateDocumentDTO): Promise<DocumentMeta> {
		return this.post<DocumentMeta, CreateDocumentDTO>('/api/documents', data);
	}

	async update(id: string, data: UpdateDocumentDTO): Promise<DocumentMeta> {
		return this.put<DocumentMeta, UpdateDocumentDTO>(`/api/documents/${id}`, data);
	}

	async updateContent(id: string, content: string): Promise<void> {
		const dto: UpdateDocumentContentDTO = { content };
		return this.put<void, UpdateDocumentContentDTO>(`/api/documents/${id}/content`, dto);
	}

	async acceptAiChange(id: string, changeId: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/ai-changes/${changeId}/accept`, {});
	}

	async rejectAiChange(id: string, changeId: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/ai-changes/${changeId}/reject`, {});
	}

	async updateOverrides(id: string, overrides: Record<string, any>): Promise<void> {
		const dto: UpdateDocumentOverridesDTO = { overrides };
		return this.put<void, UpdateDocumentOverridesDTO>(`/api/documents/${id}/overrides`, dto);
	}

	async updateMetadata(id: string, metadata: DocumentMetadata): Promise<void> {
		return this.put<void, DocumentMetadata>(`/api/documents/${id}/metadata`, metadata);
	}

	async delete(id: string): Promise<void> {
		return super.delete<void>(`/api/documents/${id}`);
	}

	async archive(id: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/archive`);
	}

	async unarchive(id: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/unarchive`);
	}

	async getTableOfContents(id: string): Promise<TocItem[]> {
		const result = await this.get<TocItem[] | null>(`/api/documents/${id}/table-of-contents`);
		return result ?? [];
	}

	async generateTableOfContents(id: string): Promise<TocItem[]> {
		return this.post<TocItem[]>(`/api/documents/${id}/table-of-contents/generate`, {});
	}

	async updateTableOfContents(id: string, items: TocItem[]): Promise<void> {
		return this.put<void, TocItem[]>(`/api/documents/${id}/table-of-contents`, items);
	}

	async resetTableOfContents(id: string): Promise<TocItem[]> {
		return this.post<TocItem[]>(`/api/documents/${id}/table-of-contents/reset`, {});
	}

	async saveVersion(id: string, name: string): Promise<DocumentVersion> {
		return this.post<DocumentVersion, SaveDocumentVersionDTO>(
			`/api/documents/${id}/versions`,
			{ name },
		);
	}

	async getVersions(id: string): Promise<DocumentVersion[]> {
		return this.get<DocumentVersion[]>(`/api/documents/${id}/versions`);
	}

	async getVersionContent(id: string, versionId: string): Promise<string> {
		const result = await this.get<{ content: string }>(
			`/api/documents/${id}/versions/${versionId}`,
		);
		return result.content ?? '';
	}

	async restoreVersion(id: string, versionId: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/versions/${versionId}/restore`);
	}

	async deleteVersion(id: string, versionId: string): Promise<void> {
		return super.delete<void>(`/api/documents/${id}/versions/${versionId}`);
	}

	async generatePdf(
		id: string,
		options?: string | GeneratePdfRequest,
	): Promise<Blob> {
		const req: GeneratePdfRequest =
			typeof options === 'string' || options === undefined
				? { titlePageId: typeof options === 'string' ? options : undefined }
				: options;
		const params = new URLSearchParams();
		if (req.titlePageId) params.set('titlePageId', req.titlePageId);
		if (req.profileId) params.set('profileId', req.profileId);
		if (req.includeDocument !== undefined)
			params.set('includeDocument', String(req.includeDocument));
		if (req.includeStyleProfile !== undefined)
			params.set('includeStyleProfile', String(req.includeStyleProfile));
		if (req.includeTitlePage !== undefined)
			params.set('includeTitlePage', String(req.includeTitlePage));
		const q = params.toString();
		return this.post<Blob>(
			`/api/documents/${id}/pdf${q ? `?${q}` : ''}`,
			{},
			{
				responseType: 'blob',
				timeout: 10 * 60 * 1000,
			},
		);
	}

	async previewPdfImport(file: File): Promise<{
		files: { name: string; size: number; kind: string }[];
		bundleParts: PdfImportBundleParts | null;
	}> {
		const form = new FormData();
		form.append('file', file);
		return this.post<{
			files: { name: string; size: number; kind: string }[];
			bundleParts: PdfImportBundleParts | null;
		}>(
			'/api/documents/pdf-import/preview',
			form,
			{ headers: { 'Content-Type': 'multipart/form-data' } },
		);
	}

	async importFromPdf(
		file: File,
		name?: string,
		selectedFileName?: string,
		options?: {
			includeDocument?: boolean;
			includeStyleProfile?: boolean;
			includeTitlePage?: boolean;
		},
	): Promise<DocumentMeta> {
		const form = new FormData();
		form.append('file', file);
		if (name) form.append('name', name);
		if (selectedFileName) form.append('selectedFileName', selectedFileName);
		if (options?.includeDocument !== undefined)
			form.append('includeDocument', String(options.includeDocument));
		if (options?.includeStyleProfile !== undefined)
			form.append('includeStyleProfile', String(options.includeStyleProfile));
		if (options?.includeTitlePage !== undefined)
			form.append('includeTitlePage', String(options.includeTitlePage));
		return this.post<DocumentMeta, FormData>('/api/documents/pdf-import', form, {
			headers: { 'Content-Type': 'multipart/form-data' },
		});
	}

	async downloadPdf(id: string): Promise<Blob> {
		return this.getBlob(`/api/documents/${id}/pdf`);
	}

	async exportDocument(id: string): Promise<Blob> {
		return this.getBlob(`/api/documents/${id}/export`);
	}

	async importDocument(file: File, name?: string): Promise<DocumentMeta> {
		const formData = new FormData();
		formData.append('file', file);
		if (name) {
			formData.append('name', name);
		}
		return this.post<DocumentMeta, FormData>('/api/documents/import', formData, {
			headers: {
				'Content-Type': 'multipart/form-data',
			},
		});
	}
}

export default new DocumentAPI();
