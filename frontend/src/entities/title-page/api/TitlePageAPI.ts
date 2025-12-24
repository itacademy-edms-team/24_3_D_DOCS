import HttpClient from '@/shared/api/HttpClient';
import type {
	TitlePage,
	TitlePageMeta,
	CreateTitlePageDTO,
	UpdateTitlePageDTO,
} from '../types';

class TitlePageAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(): Promise<TitlePageMeta[]> {
		return this._get<TitlePageMeta[]>('/api/title-pages');
	}

	async getById(id: string): Promise<TitlePage> {
		return this._get<TitlePage>(`/api/title-pages/${id}`);
	}

	async create(data: CreateTitlePageDTO): Promise<TitlePage> {
		return this._post<TitlePage, CreateTitlePageDTO>(
			'/api/title-pages',
			data
		);
	}

	async update(id: string, data: UpdateTitlePageDTO): Promise<TitlePage> {
		return this._put<TitlePage, UpdateTitlePageDTO>(
			`/api/title-pages/${id}`,
			data
		);
	}

	async delete(id: string): Promise<void> {
		return this._delete<void>(`/api/title-pages/${id}`);
	}
}

export default new TitlePageAPI();
