import type { InjectionKey, Ref } from 'vue';

export const agentChatScrollContainerKey: InjectionKey<Ref<HTMLElement | null>> =
	Symbol('agentChatScrollContainer');
