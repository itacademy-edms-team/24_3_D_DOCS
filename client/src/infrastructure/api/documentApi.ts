import type { Document, DocumentMeta, CreateDocumentDTO, UpdateDocumentDTO } from '../../../../shared/src/types';

const API_BASE = '/api/documents';

/**
 * Document API client
 */
export const documentApi = {
  /**
   * Get all documents (metadata only)
   */
  async getAll(): Promise<DocumentMeta[]> {
    const res = await fetch(API_BASE);
    if (!res.ok) throw new Error('Failed to fetch documents');
    return res.json();
  },

  /**
   * Get document by ID (full data with content)
   */
  async getById(id: string): Promise<Document> {
    const res = await fetch(`${API_BASE}/${id}`);
    if (!res.ok) throw new Error('Failed to fetch document');
    return res.json();
  },

  /**
   * Create new document
   */
  async create(data: CreateDocumentDTO): Promise<Document> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create document');
    return res.json();
  },

  /**
   * Update document
   */
  async update(id: string, data: UpdateDocumentDTO): Promise<Document> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to update document');
    return res.json();
  },

  /**
   * Delete document
   */
  async delete(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Failed to delete document');
  },

  /**
   * Generate PDF
   */
  async generatePdf(id: string, titlePageId?: string | null): Promise<Blob> {
    const body: { titlePageId?: string } = {};
    if (titlePageId) {
      body.titlePageId = titlePageId;
    }
    
    const res = await fetch(`${API_BASE}/${id}/pdf`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error('Failed to generate PDF');
    return res.blob();
  },

  /**
   * Upload image to document
   */
  async uploadImage(documentId: string, file: File): Promise<{ url: string }> {
    const formData = new FormData();
    formData.append('image', file);

    const res = await fetch(`/api/upload?documentId=${documentId}`, {
      method: 'POST',
      body: formData,
    });
    if (!res.ok) throw new Error('Failed to upload image');
    return res.json();
  },
};

