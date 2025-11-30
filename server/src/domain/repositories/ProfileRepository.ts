import type { Profile, CreateProfileDTO, UpdateProfileDTO } from '../../../../shared/src/types';

/**
 * Repository interface for Profile entity
 * Follows Repository Pattern from Clean Architecture
 */
export interface IProfileRepository {
  findAll(): Promise<Profile[]>;
  findById(id: string): Promise<Profile | null>;
  create(data: CreateProfileDTO): Promise<Profile>;
  update(id: string, data: UpdateProfileDTO): Promise<Profile | null>;
  delete(id: string): Promise<boolean>;
}

