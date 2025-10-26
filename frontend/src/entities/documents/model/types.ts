export interface DocumentLinkDTO {
  id: string;
  creatorId: string;
  name: string | null;
  description: string | null;
  mdMinioPath: string | null;
  pdfMinioPath: string | null;
  status: string | null;
  conversionLog: string | null;
  createdAt: string;
  updatedAt: string;
  creator: UserDTO | null;
}

export interface SchemaLinkDTO {
  id: string;
  creatorId: string;
  name: string | null;
  description: string | null;
  minioPath: string | null;
  pandocOptions: string | null;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
  creator: UserDTO | null;
}

export interface UserDTO {
  id: string;
  email: string | null;
  name: string | null;
  role: string | null;
  createdAt: string;
}

export interface CreateDocumentLinkDTO {
  name: string;
  description?: string;
}

export interface CreateSchemaLinkDTO {
  name: string;
  description?: string;
  pandocOptions?: string;
  isPublic?: boolean;
}

export interface ConvertDocumentDTO {
  schemaLinkId: string;
}

export type DocumentStatus = 'draft' | 'processing' | 'completed' | 'failed';
export type ContentType = 'documents' | 'templates';
