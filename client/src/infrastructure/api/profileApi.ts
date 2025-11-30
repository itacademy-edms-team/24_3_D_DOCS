import type { Profile, CreateProfileDTO, UpdateProfileDTO } from '../../../../shared/src/types';

const API_BASE = '/api/profiles';

/**
 * Profile API client
 */
export const profileApi = {
  /**
   * Get all profiles
   */
  async getAll(): Promise<Profile[]> {
    const res = await fetch(API_BASE);
    if (!res.ok) throw new Error('Failed to fetch profiles');
    return res.json();
  },

  /**
   * Get profile by ID
   */
  async getById(id: string): Promise<Profile> {
    const res = await fetch(`${API_BASE}/${id}`);
    if (!res.ok) throw new Error('Failed to fetch profile');
    return res.json();
  },

  /**
   * Create new profile
   */
  async create(data: CreateProfileDTO): Promise<Profile> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create profile');
    return res.json();
  },

  /**
   * Update profile
   */
  async update(id: string, data: UpdateProfileDTO): Promise<Profile> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update profile');
    return res.json();
  },

  /**
   * Delete profile
   */
  async delete(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Failed to delete profile');
  },
};

