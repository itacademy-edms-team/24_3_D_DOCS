import type { TitlePage, TitlePageMeta, CreateTitlePageDTO, UpdateTitlePageDTO } from '../../../../shared/src/types';

const API_BASE = '/api/title-pages';

/**
 * TitlePage API client
 */
export const titlePageApi = {
  /**
   * Get all title pages (metadata only)
   */
  async getAll(): Promise<TitlePageMeta[]> {
    const res = await fetch(API_BASE);
    if (!res.ok) throw new Error('Failed to get title pages');
    return res.json();
  },

  /**
   * Get title page by ID
   */
  async getById(id: string): Promise<TitlePage> {
    const res = await fetch(`${API_BASE}/${id}`);
    if (!res.ok) throw new Error('Failed to get title page');
    return res.json();
  },

  /**
   * Create new title page
   */
  async create(data: CreateTitlePageDTO): Promise<TitlePage> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create title page');
    return res.json();
  },

  /**
   * Update title page
   */
  async update(id: string, data: UpdateTitlePageDTO): Promise<TitlePage> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update title page');
    return res.json();
  },

  /**
   * Delete title page
   */
  async delete(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Failed to delete title page');
  },

  /**
   * Generate PDF
   */
  async generatePdf(id: string): Promise<Blob> {
    const res = await fetch(`${API_BASE}/${id}/pdf`, { method: 'POST' });
    if (!res.ok) throw new Error('Failed to generate PDF');
    return res.blob();
  },
};

