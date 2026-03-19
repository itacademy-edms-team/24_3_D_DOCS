import { ref, computed, onUnmounted, type ComputedRef } from 'vue';
import AIAPI from '@/shared/api/AIAPI';
import type { ChatMessageAttachment } from '@/shared/api/ChatAPI';

export type PendingAgentAttachment = {
	sessionId: string;
	label: string;
	contentType: string;
	/** Локальный preview до/после ingest; отозвать через URL.revokeObjectURL */
	previewObjectUrl: string | null;
};

function guessContentTypeFromName(fileName: string): string {
	const ext = fileName.split('.').pop()?.toLowerCase() ?? '';
	const map: Record<string, string> = {
		pdf: 'application/pdf',
		png: 'image/png',
		jpg: 'image/jpeg',
		jpeg: 'image/jpeg',
		gif: 'image/gif',
		webp: 'image/webp',
		txt: 'text/plain',
		md: 'text/markdown',
	};
	return map[ext] ?? 'application/octet-stream';
}

export function useAgentChatAttachments(opts: {
	scope: ComputedRef<'global' | 'document'>;
	documentId: ComputedRef<string | null | undefined>;
	activeChatId: ComputedRef<string | null | undefined>;
}) {
	const pendingAttachment = ref<PendingAgentAttachment | null>(null);
	const isUploadingAttachment = ref(false);

	const showAttachmentUi = computed(
		() => !!opts.activeChatId.value && (opts.scope.value === 'global' || !!opts.documentId.value)
	);

	const pendingAttachmentCards = computed((): ChatMessageAttachment[] => {
		const p = pendingAttachment.value;
		if (!p) return [];
		return [
			{
				sourceSessionId: p.sessionId,
				fileName: p.label,
				contentType: p.contentType,
				previewObjectUrl: p.previewObjectUrl,
			},
		];
	});

	function clearPendingAttachment() {
		const url = pendingAttachment.value?.previewObjectUrl;
		if (url) URL.revokeObjectURL(url);
		pendingAttachment.value = null;
	}

	onUnmounted(() => {
		const url = pendingAttachment.value?.previewObjectUrl;
		if (url) URL.revokeObjectURL(url);
	});

	async function onAttachmentFileSelected(ev: Event) {
		const input = ev.target as HTMLInputElement;
		const file = input.files?.[0];
		input.value = '';
		if (!file || !opts.activeChatId.value) return;
		if (opts.scope.value === 'document' && !opts.documentId.value) return;

		const contentType = file.type?.trim() || guessContentTypeFromName(file.name);
		const previewObjectUrl = URL.createObjectURL(file);

		try {
			isUploadingAttachment.value = true;
			const res = await AIAPI.ingestAgentSource(
				opts.activeChatId.value,
				file,
				opts.scope.value === 'document' ? opts.documentId.value ?? undefined : undefined
			);
			pendingAttachment.value = {
				sessionId: res.sourceSessionId,
				label: res.originalFileName || file.name,
				contentType,
				previewObjectUrl,
			};
		} catch (e) {
			URL.revokeObjectURL(previewObjectUrl);
			console.error('ingestAgentSource failed', e);
		} finally {
			isUploadingAttachment.value = false;
		}
	}

	return {
		pendingAttachment,
		isUploadingAttachment,
		showAttachmentUi,
		pendingAttachmentCards,
		clearPendingAttachment,
		onAttachmentFileSelected,
	};
}
