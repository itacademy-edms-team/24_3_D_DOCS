import HttpClient from '@/shared/api/HttpClient';

export type AttachmentItem = {
	id: string;
	creatorId: string;
	documentId?: string | null;
	fileName: string;
	contentType: string;
	size: number;
	storagePath: string;
	versionNumber: number;
	createdAt: string;
	updatedAt: string;
	deletedAt?: string | null;
};

class AttachmentsAPI extends HttpClient {
	constructor() {
		super();
	}

	async list(type?: 'image' | 'pdf' | 'all') {
		const params = [];
		if (type && type !== 'all') params.push(`type=${type}`);
		const qs = params.length ? `?${params.join('&')}` : '';
		return this.get<AttachmentItem[]>(`/api/attachments${qs}`);
	}

	async downloadBlob(id: string): Promise<Blob> {
		return this.getBlob(`/api/attachments/${id}/download`);
	}

	async rename(id: string, name: string): Promise<void> {
		return this.post<void, { name: string }>(`/api/attachments/${id}/rename`, { name });
	}

	async delete(id: string): Promise<void> {
		return super.delete<void>(`/api/attachments/${id}`);
	}

	async presigned(id: string) {
		return this.get<{ url: string }>(`/api/attachments/${id}/presigned`);
	}
}

export default new AttachmentsAPI();

