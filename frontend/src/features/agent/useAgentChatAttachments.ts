import { ref, computed, type ComputedRef } from 'vue';
import AIAPI from '@/shared/api/AIAPI';
import type { ChatMessageAttachment } from '@/shared/api/ChatAPI';

export function useAgentChatAttachments(opts: {
	scope: ComputedRef<'global' | 'document'>;
	documentId: ComputedRef<string | null | undefined>;
	activeChatId: ComputedRef<string | null | undefined>;
}) {
	const pendingAttachment = ref<{ sessionId: string; label: string } | null>(null);
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
				contentType: 'application/octet-stream',
			},
		];
	});

	function clearPendingAttachment() {
		pendingAttachment.value = null;
	}

	async function onAttachmentFileSelected(ev: Event) {
		const input = ev.target as HTMLInputElement;
		const file = input.files?.[0];
		input.value = '';
		if (!file || !opts.activeChatId.value) return;
		if (opts.scope.value === 'document' && !opts.documentId.value) return;
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
			};
		} catch (e) {
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
