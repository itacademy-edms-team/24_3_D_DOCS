import HttpClient from '@/shared/api/HttpClient';

class CollabAPI extends HttpClient {
	constructor() {
		super();
	}

	async acceptInvite(inviteId: string): Promise<void> {
		return this.post<void>(`/api/collab/invites/${inviteId}/accept`, {});
	}

	async declineInvite(inviteId: string): Promise<void> {
		return this.post<void>(`/api/collab/invites/${inviteId}/decline`, {});
	}

	async leaveCollab(documentId: string): Promise<void> {
		return this.post<void>(`/api/collab/documents/${documentId}/leave`, {});
	}
}

export default new CollabAPI();
