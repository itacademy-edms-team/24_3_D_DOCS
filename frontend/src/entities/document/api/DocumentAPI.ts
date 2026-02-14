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
} from '../types';

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

	async restore(id: string): Promise<void> {
		return this.post<void>(`/api/documents/${id}/restore`);
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

	async generatePdf(id: string, titlePageId?: string): Promise<Blob> {
		const params = titlePageId ? `?titlePageId=${titlePageId}` : '';
		return this.post<Blob>(
			`/api/documents/${id}/pdf${params}`,
			{},
			{
				responseType: 'blob',
				timeout: 10 * 60 * 1000, // 10 minutes timeout for PDF generation
			},
		);
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
