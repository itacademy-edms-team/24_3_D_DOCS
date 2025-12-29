import HttpClient from '@/shared/api/HttpClient';
import type {
	Profile,
	ProfileWithData,
	CreateProfileDTO,
	UpdateProfileDTO,
} from '../types';

class ProfileAPI extends HttpClient {
	constructor() {
		super();
	}

	async getAll(includePublic = true): Promise<Profile[]> {
		const params = includePublic ? '?includePublic=true' : '';
		return this.get<Profile[]>(`/api/profiles${params}`);
	}

	async getById(id: string): Promise<ProfileWithData> {
		return this.get<ProfileWithData>(`/api/profiles/${id}`);
	}

	async create(data: CreateProfileDTO): Promise<Profile> {
		return this.post<Profile, CreateProfileDTO>('/api/profiles', data);
	}

	async update(id: string, data: UpdateProfileDTO): Promise<Profile> {
		return this.put<Profile, UpdateProfileDTO>(`/api/profiles/${id}`, data);
	}

	async delete(id: string): Promise<void> {
		return super.delete<void>(`/api/profiles/${id}`);
	}

	async duplicate(id: string, name?: string): Promise<Profile> {
		return this.post<Profile>(`/api/profiles/${id}/duplicate`, { name });
	}
}

export default new ProfileAPI();
