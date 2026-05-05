import * as signalR from '@microsoft/signalr';
import { getAccessToken } from '@/shared/auth/tokenStorage';

const apiBase =
	typeof BASE_URI !== 'undefined' ? BASE_URI.replace(/\/$/, '') : 'http://localhost:5159';

export type DocumentContentChangedPayload = {
	documentId: string;
	editorUserId: string;
	editorDisplayName: string;
	updatedAt: string;
};

/**
 * Подключение к хабу редактора: после сохранения контента любым участником приходит событие.
 * Возвращает функцию остановки соединения.
 */
export async function startDocumentEditorRealtime(
	documentId: string,
	onContentChanged: (payload: DocumentContentChangedPayload) => void,
): Promise<() => Promise<void>> {
	const url = `${apiBase}/hubs/document-editor`;
	const connection = new signalR.HubConnectionBuilder()
		.withUrl(url, {
			accessTokenFactory: () => getAccessToken() ?? '',
		})
		.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
		.build();

	connection.on('documentContentChanged', onContentChanged);

	connection.onreconnected(async () => {
		try {
			await connection.invoke('JoinDocument', documentId);
		} catch {
			// ignore
		}
	});

	await connection.start();
	await connection.invoke('JoinDocument', documentId);

	return async () => {
		try {
			if (connection.state !== signalR.HubConnectionState.Disconnected) {
				await connection.stop();
			}
		} catch {
			// ignore
		}
	};
}
