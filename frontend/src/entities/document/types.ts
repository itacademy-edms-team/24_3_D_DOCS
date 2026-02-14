export interface DocumentMeta {
	id: string;
	creatorId: string;
	name: string;
	description?: string;
	profileId?: string;
	profileName?: string;
	titlePageId?: string;
	titlePageName?: string;
	status: string;
	isArchived: boolean;
	deletedAt?: string;
	createdAt: string;
	updatedAt: string;
	hasPdf: boolean;
}

export interface Document extends DocumentMeta {
	content?: string;
	styleOverrides?: Record<string, any>;
	metadata?: DocumentMetadata;
}

export interface DocumentMetadata {
	title?: string;
	author?: string;
	group?: string;
	year?: string;
	city?: string;
	supervisor?: string;
	documentType?: string;
	additionalFields?: Record<string, string>;
}

export interface CreateDocumentDTO {
	name: string;
	description?: string;
	profileId?: string;
	titlePageId?: string;
	metadata?: DocumentMetadata;
	initialContent?: string;
}

export interface UpdateDocumentDTO {
	name?: string;
	description?: string;
	profileId?: string;
	titlePageId?: string;
	metadata?: DocumentMetadata;
}

export interface UpdateDocumentContentDTO {
	content: string;
}

export interface UpdateDocumentOverridesDTO {
	overrides: Record<string, any>;
}

export interface TocItem {
	level: number;
	text: string;
	pageNumber?: number;
	headingId?: string;
	isManual?: boolean;
}
