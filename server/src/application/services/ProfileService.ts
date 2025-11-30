import type { Profile, CreateProfileDTO, UpdateProfileDTO } from '../../../../shared/src/types';
import type { IProfileRepository } from '../../domain/repositories/ProfileRepository';

/**
 * Application service for Profile operations
 * Contains business logic and orchestrates domain operations
 */
export class ProfileService {
  constructor(private readonly repository: IProfileRepository) {}

  async getAllProfiles(): Promise<Profile[]> {
    return this.repository.findAll();
  }

  async getProfileById(id: string): Promise<Profile | null> {
    return this.repository.findById(id);
  }

  async createProfile(data: CreateProfileDTO): Promise<Profile> {
    return this.repository.create(data);
  }

  async updateProfile(id: string, data: UpdateProfileDTO): Promise<Profile | null> {
    return this.repository.update(id, data);
  }

  async deleteProfile(id: string): Promise<boolean> {
    return this.repository.delete(id);
  }
}

