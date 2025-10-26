import HttpClient from '@api';
import { DocumentLinkDTO, CreateDocumentLinkDTO, ConvertDocumentDTO } from '../model/types';

export class DocumentLinksAPI {
  private httpClient: HttpClient;

  constructor() {
    this.httpClient = new HttpClient({
      baseURL: typeof BASE_URI !== 'undefined' ? BASE_URI : 'http://localhost:5159',
    });
  }

  async getDocuments(): Promise<DocumentLinkDTO[]> {
    return this.httpClient.get<DocumentLinkDTO[]>('/api/DocumentLinks');
  }

  async getDocument(id: string): Promise<DocumentLinkDTO> {
    return this.httpClient.get<DocumentLinkDTO>(`/api/DocumentLinks/${id}`);
  }

  async createDocument(formData: FormData): Promise<DocumentLinkDTO> {
    return this.httpClient.post<DocumentLinkDTO>('/api/DocumentLinks', formData, {
      'Content-Type': 'multipart/form-data',
    });
  }

  async deleteDocument(id: string): Promise<void> {
    return this.httpClient.delete(`/api/DocumentLinks/${id}`);
  }

  async downloadDocument(id: string): Promise<Blob> {
    return this.httpClient.getBlob(`/api/DocumentLinks/${id}/download`);
  }

  async downloadPdf(id: string): Promise<Blob> {
    return this.httpClient.getBlob(`/api/DocumentLinks/${id}/pdf`);
  }

  async convertDocument(id: string, schemaLinkId: string): Promise<void> {
    const convertData: ConvertDocumentDTO = { schemaLinkId };
    return this.httpClient.post(`/api/DocumentLinks/${id}/convert`, convertData);
  }
}

export const documentLinksAPI = new DocumentLinksAPI();
