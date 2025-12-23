import type { EntityStyle } from '@/shared/services/markdown/renderUtils';

export interface DocumentMeta {
	id: string;
	name?: string;
	createdAt: string;
	updatedAt: string;
	profileId?: string;
}

export interface Document extends DocumentMeta {
	content: string;
	profileId?: string;
	overrides?: Record<string, EntityStyle>;
}

export interface CreateDocumentDTO {
	name?: string;
	profileId?: string;
}

export interface UpdateDocumentDTO {
	name?: string;
	content?: string;
	profileId?: string;
	overrides?: Record<string, EntityStyle>;
}
