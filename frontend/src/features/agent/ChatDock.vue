<template>
	<div v-if="open" class="chat-dock" :style="{ width: `${chatDockWidth}px` }">
		<div 
			class="chat-dock__resizer"
			@mousedown="startResize"
		></div>
		<AgentChat
			:documentId="documentId"
			:startLine="startLine"
			:endLine="endLine"
			@close="emit('update:open', false)"
			@clearSelection="handleClearSelection"
			@documentUpdated="handleDocumentUpdated"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted, onMounted } from 'vue';
import { useStorage } from '@vueuse/core';
import AgentChat from './AgentChat.vue';

interface Props {
	open: boolean;
	documentId: string;
	startLine?: number;
	endLine?: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:open': [value: boolean];
	'width-changed': [width: number];
	clearSelection: [];
	documentUpdated: [];
}>();

// Chat dock width with persistence
const storedWidth = useStorage<number>('chat-dock-width', 400);
const chatDockWidth = ref(storedWidth.value);

// Emit width changes to parent
watch(chatDockWidth, (newWidth) => {
	emit('width-changed', newWidth);
});

// Emit initial width when panel opens
watch(() => props.open, (isOpen) => {
	if (isOpen) {
		emit('width-changed', chatDockWidth.value);
	}
}, { immediate: true });

// Resize handling
let isResizing = false;
let startX = 0;
let startWidth = 0;

function startResize(e: MouseEvent) {
	e.preventDefault();
	e.stopPropagation();
	isResizing = true;
	startX = e.clientX;
	startWidth = chatDockWidth.value;
	
	document.addEventListener('mousemove', handleResize);
	document.addEventListener('mouseup', stopResize);
	document.body.style.cursor = 'col-resize';
	document.body.style.userSelect = 'none';
}

function handleResize(e: MouseEvent) {
	if (!isResizing) return;
	
	const deltaX = startX - e.clientX; // Inverted because we're resizing from left
	const newWidth = Math.max(300, Math.min(800, startWidth + deltaX));
	chatDockWidth.value = newWidth;
	storedWidth.value = newWidth;
}

function stopResize() {
	isResizing = false;
	document.removeEventListener('mousemove', handleResize);
	document.removeEventListener('mouseup', stopResize);
	document.body.style.cursor = '';
	document.body.style.userSelect = '';
}

onUnmounted(() => {
	stopResize();
});

const handleClearSelection = () => {
	emit('clearSelection');
};

const handleDocumentUpdated = () => {
	emit('documentUpdated');
};

</script>

<style scoped>
.chat-dock {
	position: fixed;
	right: 0;
	top: 0;
	bottom: 0;
	background: var(--bg-primary);
	border-left: 1px solid var(--border-color);
	z-index: 1000;
	display: flex;
	flex-direction: column;
}

.chat-dock__resizer {
	position: absolute;
	left: 0;
	top: 0;
	bottom: 0;
	width: 4px;
	cursor: col-resize;
	background: transparent;
	z-index: 10;
	transition: background 0.2s ease;
}

.chat-dock__resizer:hover {
	background: var(--accent);
}
</style>
