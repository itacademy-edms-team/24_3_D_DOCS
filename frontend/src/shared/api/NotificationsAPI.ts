import HttpClient from '@/shared/api/HttpClient';

export interface NotificationListItem {
	id: string;
	type: string;
	payload: {
		inviteId?: string;
		documentId?: string;
		documentName?: string;
		inviterName?: string;
		notificationId?: string;
	} | null;
	readAt: string | null;
	createdAt: string;
}

class NotificationsAPI extends HttpClient {
	constructor() {
		super();
	}

	async list(): Promise<NotificationListItem[]> {
		return this.get<NotificationListItem[]>('/api/notifications');
	}

	async markRead(id: string): Promise<void> {
		return this.post<void>(`/api/notifications/${id}/read`, {});
	}
}

export default new NotificationsAPI();
