import HttpClient from '@/shared/api/HttpClient';
import type { Profile, CreateProfileDTO, UpdateProfileDTO } from '../types';

class ProfileAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(): Promise<Profile[]> {
		return this._get<Profile[]>('/api/profiles');
	}

	async getById(id: string): Promise<Profile> {
		return this._get<Profile>(`/api/profiles/${id}`);
	}

	async create(data: CreateProfileDTO): Promise<Profile> {
		return this._post<Profile, CreateProfileDTO>('/api/profiles', data);
	}

	async update(id: string, data: UpdateProfileDTO): Promise<Profile> {
		return this._put<Profile, UpdateProfileDTO>(`/api/profiles/${id}`, data);
	}

	async delete(id: string): Promise<void> {
		return this._delete<void>(`/api/profiles/${id}`);
	}
}

export default new ProfileAPI();
