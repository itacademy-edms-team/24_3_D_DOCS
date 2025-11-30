import fs from 'fs';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import type { Profile, CreateProfileDTO, UpdateProfileDTO } from '../../../../shared/src/types';
import { DEFAULT_PAGE_SETTINGS, DEFAULT_ENTITY_STYLES } from '../../../../shared/src/types';
import type { IProfileRepository } from '../../domain/repositories/ProfileRepository';

/**
 * File-based implementation of Profile Repository
 * Stores profiles as JSON files in the filesystem
 */
export class FileProfileRepository implements IProfileRepository {
  private readonly profilesDir: string;

  constructor(dataDir: string) {
    this.profilesDir = path.join(dataDir, 'profiles');
    this.ensureDirectoryExists();
  }

  private ensureDirectoryExists(): void {
    if (!fs.existsSync(this.profilesDir)) {
      fs.mkdirSync(this.profilesDir, { recursive: true });
    }
  }

  private getFilePath(id: string): string {
    return path.join(this.profilesDir, `${id}.json`);
  }

  async findAll(): Promise<Profile[]> {
    const files = fs.readdirSync(this.profilesDir).filter(f => f.endsWith('.json'));
    
    return files.map(file => {
      const content = fs.readFileSync(path.join(this.profilesDir, file), 'utf-8');
      return JSON.parse(content) as Profile;
    });
  }

  async findById(id: string): Promise<Profile | null> {
    const filePath = this.getFilePath(id);
    
    if (!fs.existsSync(filePath)) {
      return null;
    }
    
    const content = fs.readFileSync(filePath, 'utf-8');
    return JSON.parse(content) as Profile;
  }

  async create(data: CreateProfileDTO): Promise<Profile> {
    const id = uuidv4();
    const now = new Date().toISOString();

    const profile: Profile = {
      id,
      name: data.name || 'Новый профиль',
      createdAt: now,
      updatedAt: now,
      page: DEFAULT_PAGE_SETTINGS,
      entities: { ...DEFAULT_ENTITY_STYLES },
    };

    const filePath = this.getFilePath(id);
    fs.writeFileSync(filePath, JSON.stringify(profile, null, 2));

    return profile;
  }

  async update(id: string, data: UpdateProfileDTO): Promise<Profile | null> {
    const existing = await this.findById(id);
    
    if (!existing) {
      return null;
    }

    const updated: Profile = {
      ...existing,
      ...data,
      id: existing.id,
      createdAt: existing.createdAt,
      updatedAt: new Date().toISOString(),
    };

    const filePath = this.getFilePath(id);
    fs.writeFileSync(filePath, JSON.stringify(updated, null, 2));

    return updated;
  }

  async delete(id: string): Promise<boolean> {
    const filePath = this.getFilePath(id);
    
    if (!fs.existsSync(filePath)) {
      return false;
    }

    fs.unlinkSync(filePath);
    return true;
  }
}

