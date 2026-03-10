import { ref, watch, nextTick, provide, type Ref } from 'vue';
import { agentChatScrollContainerKey } from './agentChatKeys';

export function useAgentChatScroll(opts: {
	agentEvents: Ref<unknown>;
	visibleHistoryMessages: Ref<unknown>;
	getActiveChatMessages: () => unknown[] | undefined;
}) {
	const messagesContainerRef = ref<HTMLElement | null>(null);
	provide(agentChatScrollContainerKey, messagesContainerRef);
	const autoScrollEnabled = ref(true);
	const showScrollDownButton = ref(false);

	function checkScrollPosition() {
		const container = messagesContainerRef.value;
		if (!container) return;

		const { scrollTop, scrollHeight, clientHeight } = container;
		const isNearBottom = scrollHeight - scrollTop - clientHeight < 100;

		if (isNearBottom) {
			autoScrollEnabled.value = true;
			showScrollDownButton.value = false;
		} else {
			autoScrollEnabled.value = false;
			showScrollDownButton.value = true;
		}
	}

	watch(
		messagesContainerRef,
		(el, _prev, onCleanup) => {
			if (!el) return;
			el.addEventListener('scroll', checkScrollPosition, { passive: true });
			onCleanup(() => el.removeEventListener('scroll', checkScrollPosition));
		},
		{ flush: 'post' }
	);

	function scrollToBottom() {
		const container = messagesContainerRef.value;
		if (!container) return;

		container.scrollTo({
			top: container.scrollHeight,
			behavior: 'smooth',
		});

		setTimeout(() => {
			autoScrollEnabled.value = true;
			showScrollDownButton.value = false;
		}, 500);
	}

	watch(
		[opts.agentEvents, opts.visibleHistoryMessages],
		() => {
			if (autoScrollEnabled.value) {
				nextTick(() => {
					const container = messagesContainerRef.value;
					if (container) {
						container.scrollTop = container.scrollHeight;
					}
				});
			}
		},
		{ deep: true }
	);

	watch(
		() => opts.getActiveChatMessages(),
		() => {
			if (autoScrollEnabled.value) {
				nextTick(() => {
					const container = messagesContainerRef.value;
					if (container) {
						container.scrollTop = container.scrollHeight;
					}
				});
			}
		},
		{ deep: true }
	);

	return {
		messagesContainerRef,
		autoScrollEnabled,
		showScrollDownButton,
		scrollToBottom,
	};
}
