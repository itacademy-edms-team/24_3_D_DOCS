import HttpClient from '@/shared/api/HttpClient';
import type {
	Document,
	DocumentMeta,
	CreateDocumentDTO,
	UpdateDocumentDTO,
} from '../types';

class DocumentAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(): Promise<DocumentMeta[]> {
		return this._get<DocumentMeta[]>('/api/documents');
	}

	async getById(id: string): Promise<Document> {
		return this._get<Document>(`/api/documents/${id}`);
	}

	async create(data: CreateDocumentDTO): Promise<Document> {
		return this._post<Document, CreateDocumentDTO>('/api/documents', data);
	}

	async update(id: string, data: UpdateDocumentDTO): Promise<Document> {
		return this._put<Document, UpdateDocumentDTO>(`/api/documents/${id}`, data);
	}

	async delete(id: string): Promise<void> {
		return this._delete<void>(`/api/documents/${id}`);
	}
}

export default new DocumentAPI();
