<template>
	<div class="maf-tool">
		<div class="maf-tool__summary">
			<span>{{ getToolLabel(toolName) }}</span>
		</div>
		<div class="maf-tool__result">{{ formatToolResult(result, toolName) }}</div>
	</div>
</template>

<script setup lang="ts">
import { getToolLabel, formatToolResult } from './agentChatUtils';

defineProps<{
	toolName: string;
	result?: string;
}>();
</script>

<style scoped>
.maf-tool {
	border: 1px solid var(--chat-border);
	border-radius: 12px;
	background: var(--chat-assistant-bubble-solid);
	padding: 10px 12px;
}

.maf-tool__summary {
	font-weight: 600;
	font-size: 14px;
	line-height: 1.35;
}

.maf-tool__result {
	margin-top: 6px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--chat-foreground);
	opacity: 0.72;
}
</style>
