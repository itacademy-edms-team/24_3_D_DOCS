import fs from 'fs';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import type { TitlePage, TitlePageMeta, CreateTitlePageDTO, UpdateTitlePageDTO } from '../../../../shared/src/types';
import type { ITitlePageRepository } from '../../domain/repositories/TitlePageRepository';

/**
 * File-based implementation of TitlePage Repository
 * Stores title pages in folders with meta.json and canvas-data.json
 */
export class FileTitlePageRepository implements ITitlePageRepository {
  private readonly titlePagesDir: string;

  constructor(dataDir: string) {
    this.titlePagesDir = path.join(dataDir, 'title-pages');
    this.ensureDirectoryExists();
  }

  private ensureDirectoryExists(): void {
    if (!fs.existsSync(this.titlePagesDir)) {
      fs.mkdirSync(this.titlePagesDir, { recursive: true });
    }
  }

  getTitlePagePath(id: string): string {
    return path.join(this.titlePagesDir, id);
  }

  private getMetaPath(id: string): string {
    return path.join(this.getTitlePagePath(id), 'meta.json');
  }

  private getCanvasDataPath(id: string): string {
    return path.join(this.getTitlePagePath(id), 'canvas-data.json');
  }

  async findAll(): Promise<TitlePageMeta[]> {
    const dirs = fs.readdirSync(this.titlePagesDir).filter(d => {
      const stat = fs.statSync(path.join(this.titlePagesDir, d));
      return stat.isDirectory();
    });

    return dirs
      .map(dir => {
        const metaPath = path.join(this.titlePagesDir, dir, 'meta.json');
        if (fs.existsSync(metaPath)) {
          return JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as TitlePageMeta;
        }
        return null;
      })
      .filter((page): page is TitlePageMeta => page !== null);
  }

  async findById(id: string): Promise<TitlePage | null> {
    const pageDir = this.getTitlePagePath(id);
    
    if (!fs.existsSync(pageDir)) {
      return null;
    }

    const metaPath = this.getMetaPath(id);
    const canvasDataPath = this.getCanvasDataPath(id);

    const meta = JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as TitlePageMeta;
    const canvasData = fs.existsSync(canvasDataPath)
      ? JSON.parse(fs.readFileSync(canvasDataPath, 'utf-8'))
      : { elements: [], variables: {} };

    return {
      ...meta,
      elements: canvasData.elements || [],
      variables: canvasData.variables || {},
    };
  }

  async create(data: CreateTitlePageDTO): Promise<TitlePage> {
    const id = uuidv4();
    const now = new Date().toISOString();
    const pageDir = this.getTitlePagePath(id);

    fs.mkdirSync(pageDir, { recursive: true });

    const meta: TitlePageMeta = {
      id,
      name: data.name || 'Новый титульный лист',
      createdAt: now,
      updatedAt: now,
    };

    const canvasData = {
      elements: [],
      variables: {},
    };

    fs.writeFileSync(this.getMetaPath(id), JSON.stringify(meta, null, 2));
    fs.writeFileSync(this.getCanvasDataPath(id), JSON.stringify(canvasData, null, 2));

    return {
      ...meta,
      elements: [],
      variables: {},
    };
  }

  async update(id: string, data: UpdateTitlePageDTO): Promise<TitlePage | null> {
    const pageDir = this.getTitlePagePath(id);
    
    if (!fs.existsSync(pageDir)) {
      return null;
    }

    const metaPath = this.getMetaPath(id);
    const existing = JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as TitlePageMeta;

    const { elements, variables, ...metaUpdates } = data;

    const updatedMeta: TitlePageMeta = {
      ...existing,
      ...metaUpdates,
      id: existing.id,
      createdAt: existing.createdAt,
      updatedAt: new Date().toISOString(),
    };

    fs.writeFileSync(metaPath, JSON.stringify(updatedMeta, null, 2));

    if (elements !== undefined || variables !== undefined) {
      const canvasDataPath = this.getCanvasDataPath(id);
      const existingCanvasData = fs.existsSync(canvasDataPath)
        ? JSON.parse(fs.readFileSync(canvasDataPath, 'utf-8'))
        : { elements: [], variables: {} };

      const updatedCanvasData = {
        elements: elements !== undefined ? elements : existingCanvasData.elements,
        variables: variables !== undefined ? variables : existingCanvasData.variables,
      };

      fs.writeFileSync(canvasDataPath, JSON.stringify(updatedCanvasData, null, 2));
    }

    const finalCanvasData = JSON.parse(fs.readFileSync(this.getCanvasDataPath(id), 'utf-8'));

    return {
      ...updatedMeta,
      elements: finalCanvasData.elements || [],
      variables: finalCanvasData.variables || {},
    };
  }

  async delete(id: string): Promise<boolean> {
    const pageDir = this.getTitlePagePath(id);
    
    if (!fs.existsSync(pageDir)) {
      return false;
    }

    fs.rmSync(pageDir, { recursive: true });
    return true;
  }
}

