import HttpClient from '@/shared/api/HttpClient';

interface TitlePage {
	id: string;
	creatorId: string;
	name: string;
	description?: string;
	createdAt: string;
	updatedAt: string;
}

export interface TitlePageWithData extends TitlePage {
	data: {
		elements: any[];
	};
}

interface CreateTitlePageDTO {
	name: string;
	description?: string;
	data?: { elements: any[] };
}

interface UpdateTitlePageDTO {
	name?: string;
	description?: string;
	data?: { elements: any[] };
}

class TitlePageAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(): Promise<TitlePage[]> {
		return this.get<TitlePage[]>('/api/title-pages');
	}

	async getById(id: string): Promise<TitlePageWithData> {
		return this.get<TitlePageWithData>(`/api/title-pages/${id}`);
	}

	async create(data: CreateTitlePageDTO): Promise<TitlePage> {
		return this.post<TitlePage, CreateTitlePageDTO>('/api/title-pages', data);
	}

	async update(id: string, data: UpdateTitlePageDTO): Promise<TitlePage> {
		return this.put<TitlePage, UpdateTitlePageDTO>(`/api/title-pages/${id}`, data);
	}

	async delete(id: string): Promise<void> {
		return super.delete<void>(`/api/title-pages/${id}`);
	}

	async generatePdf(id: string, variables?: Record<string, string>): Promise<Blob> {
		return this.post<Blob>(`/api/title-pages/${id}/pdf`, variables || {}, {
			responseType: 'blob',
		});
	}
}

export default new TitlePageAPI();
