import type { Document, DocumentMeta, CreateDocumentDTO, UpdateDocumentDTO } from '../../../../shared/src/types';

/**
 * Repository interface for Document entity
 * Follows Repository Pattern from Clean Architecture
 */
export interface IDocumentRepository {
  findAll(): Promise<DocumentMeta[]>;
  findById(id: string): Promise<Document | null>;
  create(data: CreateDocumentDTO): Promise<Document>;
  update(id: string, data: UpdateDocumentDTO): Promise<Document | null>;
  delete(id: string): Promise<boolean>;
  getDocumentPath(id: string): string;
  saveImage(documentId: string, filename: string, buffer: Buffer): Promise<string>;
}

