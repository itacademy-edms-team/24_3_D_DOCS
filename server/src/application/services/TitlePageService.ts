import type { TitlePage, TitlePageMeta, CreateTitlePageDTO, UpdateTitlePageDTO } from '../../../../shared/src/types';
import type { ITitlePageRepository } from '../../domain/repositories/TitlePageRepository';
import type { TitlePagePdfGenerator } from '../../infrastructure/pdf/TitlePagePdfGenerator';

/**
 * Application service for TitlePage operations
 * Contains business logic and orchestrates domain operations
 */
export class TitlePageService {
  constructor(
    private readonly titlePageRepository: ITitlePageRepository,
    private readonly pdfGenerator: TitlePagePdfGenerator
  ) {}

  async getAllTitlePages(): Promise<TitlePageMeta[]> {
    return this.titlePageRepository.findAll();
  }

  async getTitlePageById(id: string): Promise<TitlePage | null> {
    return this.titlePageRepository.findById(id);
  }

  async createTitlePage(data: CreateTitlePageDTO): Promise<TitlePage> {
    return this.titlePageRepository.create(data);
  }

  async updateTitlePage(id: string, data: UpdateTitlePageDTO): Promise<TitlePage | null> {
    return this.titlePageRepository.update(id, data);
  }

  async deleteTitlePage(id: string): Promise<boolean> {
    return this.titlePageRepository.delete(id);
  }

  async generatePdf(id: string): Promise<Buffer> {
    const titlePage = await this.titlePageRepository.findById(id);
    
    if (!titlePage) {
      throw new Error('Title page not found');
    }

    return this.pdfGenerator.generate(titlePage);
  }
}

