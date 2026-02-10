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
	variables?: Record<string, string>;
}

export interface CreateDocumentDTO {
	name: string;
	description?: string;
	profileId?: string;
	titlePageId?: string;
	variables?: Record<string, string>;
	initialContent?: string;
}

export interface UpdateDocumentDTO {
	name?: string;
	description?: string;
	profileId?: string;
	titlePageId?: string;
	variables?: Record<string, string>;
}

export interface UpdateDocumentContentDTO {
	content: string;
}

export interface UpdateDocumentOverridesDTO {
	overrides: Record<string, any>;
}

export interface LineEmbeddingStatus {
	lineNumber: number; // 0-based
	isCovered: boolean;
	blockId?: string;
	isEmpty: boolean;
}

export interface EmbeddingStatus {
	coveragePercentage: number;
	totalLines: number;
	coveredLines: number;
	lineStatuses: LineEmbeddingStatus[];
}
