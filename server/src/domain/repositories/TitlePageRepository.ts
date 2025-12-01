import type { TitlePage, TitlePageMeta, CreateTitlePageDTO, UpdateTitlePageDTO } from '../../../../shared/src/types';

/**
 * Repository interface for TitlePage entity
 * Follows Repository Pattern from Clean Architecture
 */
export interface ITitlePageRepository {
  findAll(): Promise<TitlePageMeta[]>;
  findById(id: string): Promise<TitlePage | null>;
  create(data: CreateTitlePageDTO): Promise<TitlePage>;
  update(id: string, data: UpdateTitlePageDTO): Promise<TitlePage | null>;
  delete(id: string): Promise<boolean>;
  getTitlePagePath(id: string): string;
}

