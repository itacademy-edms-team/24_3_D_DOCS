import HttpClient from '@api';
import { SchemaLinkDTO, CreateSchemaLinkDTO } from '../model/types';

export class SchemaLinksAPI {
  private httpClient: HttpClient;

  constructor() {
    this.httpClient = new HttpClient({
      baseURL: typeof BASE_URI !== 'undefined' ? BASE_URI : 'http://localhost:5159',
    });
  }

  async getSchemas(): Promise<SchemaLinkDTO[]> {
    return this.httpClient.get<SchemaLinkDTO[]>('/api/SchemaLinks');
  }

  async getSchema(id: string): Promise<SchemaLinkDTO> {
    return this.httpClient.get<SchemaLinkDTO>(`/api/SchemaLinks/${id}`);
  }

  async createSchema(formData: FormData): Promise<SchemaLinkDTO> {
    return this.httpClient.post<SchemaLinkDTO>('/api/SchemaLinks', formData, {
      'Content-Type': 'multipart/form-data',
    });
  }

  async deleteSchema(id: string): Promise<void> {
    return this.httpClient.delete(`/api/SchemaLinks/${id}`);
  }

  async downloadSchema(id: string): Promise<Blob> {
    return this.httpClient.getBlob(`/api/SchemaLinks/${id}/download`);
  }
}

export const schemaLinksAPI = new SchemaLinksAPI();
