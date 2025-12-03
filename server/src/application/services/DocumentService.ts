import type { Document, DocumentMeta, CreateDocumentDTO, UpdateDocumentDTO } from '../../../../shared/src/types';
import type { IDocumentRepository } from '../../domain/repositories/DocumentRepository';
import type { IProfileRepository } from '../../domain/repositories/ProfileRepository';
import type { ITitlePageRepository } from '../../domain/repositories/TitlePageRepository';
import type { PdfGenerator } from '../../infrastructure/pdf/PdfGenerator';

/**
 * Application service for Document operations
 * Contains business logic and orchestrates domain operations
 */
export class DocumentService {
  constructor(
    private readonly documentRepository: IDocumentRepository,
    private readonly profileRepository: IProfileRepository,
    private readonly titlePageRepository: ITitlePageRepository,
    private readonly pdfGenerator: PdfGenerator
  ) {}

  async getAllDocuments(): Promise<DocumentMeta[]> {
    return this.documentRepository.findAll();
  }

  async getDocumentById(id: string): Promise<Document | null> {
    return this.documentRepository.findById(id);
  }

  async createDocument(data: CreateDocumentDTO): Promise<Document> {
    return this.documentRepository.create(data);
  }

  async updateDocument(id: string, data: UpdateDocumentDTO): Promise<Document | null> {
    return this.documentRepository.update(id, data);
  }

  async deleteDocument(id: string): Promise<boolean> {
    return this.documentRepository.delete(id);
  }

  async generatePdf(id: string, titlePageId?: string | null): Promise<Buffer> {
    const document = await this.documentRepository.findById(id);
    
    if (!document) {
      throw new Error('Document not found');
    }

    const profile = document.profileId 
      ? await this.profileRepository.findById(document.profileId)
      : null;

    const titlePage = titlePageId
      ? await this.titlePageRepository.findById(titlePageId)
      : null;

    const docDir = this.documentRepository.getDocumentPath(id);

    return this.pdfGenerator.generate(
      document.content,
      profile,
      document.overrides,
      docDir,
      titlePage
    );
  }

  async saveImage(documentId: string, filename: string, buffer: Buffer): Promise<string> {
    return this.documentRepository.saveImage(documentId, filename, buffer);
  }
}

