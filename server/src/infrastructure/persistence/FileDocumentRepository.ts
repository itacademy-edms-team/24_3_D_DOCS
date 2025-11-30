import fs from 'fs';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import type { Document, DocumentMeta, CreateDocumentDTO, UpdateDocumentDTO } from '../../../../shared/src/types';
import type { IDocumentRepository } from '../../domain/repositories/DocumentRepository';

/**
 * File-based implementation of Document Repository
 * Stores documents in folders with meta.json, content.md, and overrides.json
 */
export class FileDocumentRepository implements IDocumentRepository {
  private readonly documentsDir: string;

  constructor(dataDir: string) {
    this.documentsDir = path.join(dataDir, 'documents');
    this.ensureDirectoryExists();
  }

  private ensureDirectoryExists(): void {
    if (!fs.existsSync(this.documentsDir)) {
      fs.mkdirSync(this.documentsDir, { recursive: true });
    }
  }

  getDocumentPath(id: string): string {
    return path.join(this.documentsDir, id);
  }

  private getMetaPath(id: string): string {
    return path.join(this.getDocumentPath(id), 'meta.json');
  }

  private getContentPath(id: string): string {
    return path.join(this.getDocumentPath(id), 'content.md');
  }

  private getOverridesPath(id: string): string {
    return path.join(this.getDocumentPath(id), 'overrides.json');
  }

  async findAll(): Promise<DocumentMeta[]> {
    const dirs = fs.readdirSync(this.documentsDir).filter(d => {
      const stat = fs.statSync(path.join(this.documentsDir, d));
      return stat.isDirectory();
    });

    return dirs
      .map(dir => {
        const metaPath = path.join(this.documentsDir, dir, 'meta.json');
        if (fs.existsSync(metaPath)) {
          return JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as DocumentMeta;
        }
        return null;
      })
      .filter((doc): doc is DocumentMeta => doc !== null);
  }

  async findById(id: string): Promise<Document | null> {
    const docDir = this.getDocumentPath(id);
    
    if (!fs.existsSync(docDir)) {
      return null;
    }

    const metaPath = this.getMetaPath(id);
    const contentPath = this.getContentPath(id);
    const overridesPath = this.getOverridesPath(id);

    const meta = JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as DocumentMeta;
    const content = fs.existsSync(contentPath) ? fs.readFileSync(contentPath, 'utf-8') : '';
    const overrides = fs.existsSync(overridesPath) 
      ? JSON.parse(fs.readFileSync(overridesPath, 'utf-8')) 
      : {};

    return { ...meta, content, overrides };
  }

  async create(data: CreateDocumentDTO): Promise<Document> {
    const id = uuidv4();
    const now = new Date().toISOString();
    const docDir = this.getDocumentPath(id);

    fs.mkdirSync(docDir, { recursive: true });

    const meta: DocumentMeta = {
      id,
      name: data.name || 'Новый документ',
      profileId: data.profileId || '',
      createdAt: now,
      updatedAt: now,
    };

    const content = data.content || '';
    const overrides = {};

    fs.writeFileSync(this.getMetaPath(id), JSON.stringify(meta, null, 2));
    fs.writeFileSync(this.getContentPath(id), content);
    fs.writeFileSync(this.getOverridesPath(id), JSON.stringify(overrides, null, 2));

    return { ...meta, content, overrides };
  }

  async update(id: string, data: UpdateDocumentDTO): Promise<Document | null> {
    const docDir = this.getDocumentPath(id);
    
    if (!fs.existsSync(docDir)) {
      return null;
    }

    const metaPath = this.getMetaPath(id);
    const existing = JSON.parse(fs.readFileSync(metaPath, 'utf-8')) as DocumentMeta;

    const { content, overrides, ...metaUpdates } = data;

    const updatedMeta: DocumentMeta = {
      ...existing,
      ...metaUpdates,
      id: existing.id,
      createdAt: existing.createdAt,
      updatedAt: new Date().toISOString(),
    };

    fs.writeFileSync(metaPath, JSON.stringify(updatedMeta, null, 2));

    if (content !== undefined) {
      fs.writeFileSync(this.getContentPath(id), content);
    }

    if (overrides !== undefined) {
      fs.writeFileSync(this.getOverridesPath(id), JSON.stringify(overrides, null, 2));
    }

    const finalContent = fs.readFileSync(this.getContentPath(id), 'utf-8');
    const finalOverrides = JSON.parse(fs.readFileSync(this.getOverridesPath(id), 'utf-8'));

    return { ...updatedMeta, content: finalContent, overrides: finalOverrides };
  }

  async delete(id: string): Promise<boolean> {
    const docDir = this.getDocumentPath(id);
    
    if (!fs.existsSync(docDir)) {
      return false;
    }

    fs.rmSync(docDir, { recursive: true });
    return true;
  }

  async saveImage(documentId: string, filename: string, buffer: Buffer): Promise<string> {
    const imagesDir = path.join(this.getDocumentPath(documentId), 'images');
    
    if (!fs.existsSync(imagesDir)) {
      fs.mkdirSync(imagesDir, { recursive: true });
    }

    const ext = path.extname(filename) || '.jpg';
    const imageId = uuidv4();
    const imageName = `${imageId}${ext}`;
    const imagePath = path.join(imagesDir, imageName);

    fs.writeFileSync(imagePath, buffer);

    return `/uploads/${documentId}/images/${imageName}`;
  }
}

