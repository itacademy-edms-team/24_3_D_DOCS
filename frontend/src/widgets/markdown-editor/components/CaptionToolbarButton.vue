<template>
	<div class="caption-toolbar-button" ref="containerRef" @mouseenter="updateDropdownPosition">
		<NormalToolbar :title="title" :disabled="disabled">
			<svg
				xmlns="http://www.w3.org/2000/svg"
				width="24"
				height="24"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				stroke-linecap="round"
				stroke-linejoin="round"
				class="caption-icon"
			>
				<polyline points="4 7 4 4 20 4 20 7" />
				<line x1="9" y1="20" x2="15" y2="20" />
				<line x1="12" y1="4" x2="12" y2="20" />
			</svg>
			<span v-if="showToolbarName" class="toolbar-item-name">{{ title }}</span>
		</NormalToolbar>
		<div ref="dropdownRef" class="caption-dropdown" @click.stop @mouseenter="updateDropdownPosition" @mouseleave="updateDropdownPosition">
			<ul class="caption-menu" role="menu">
				<li
					v-for="captionType in captionTypes"
					:key="captionType.value"
					class="caption-menu-item"
					role="menuitem"
					tabindex="0"
					@click="handleCaptionSelect(captionType)"
				>
					{{ captionType.label }}
				</li>
			</ul>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue';
import { NormalToolbar } from 'md-editor-v3';
import type { InsertContentGenerator } from 'md-editor-v3/es/types';

interface Props {
	title?: string;
	disabled?: boolean;
	showToolbarName?: boolean;
	insert?: (generate: InsertContentGenerator) => void;
}

const props = withDefaults(defineProps<Props>(), {
	title: 'Подпись',
	disabled: false,
	showToolbarName: false,
});

const containerRef = ref<HTMLElement>();
const dropdownRef = ref<HTMLElement>();

const captionTypes = [
	{ value: 'image', label: 'Подпись к рисункам', text: '[IMAGE-CAPTION: текст подписи]' },
	{ value: 'table', label: 'Подпись к таблицам', text: '[TABLE-CAPTION: текст подписи]' },
	{ value: 'formula', label: 'Подпись к формулам', text: '[FORMULA-CAPTION: текст подписи]' },
];

const updateDropdownPosition = async () => {
	if (!containerRef.value || !dropdownRef.value) return;
	
	// Сначала показываем dropdown, чтобы получить его размеры
	if (dropdownRef.value.style.display === 'none') {
		dropdownRef.value.style.display = 'block';
		dropdownRef.value.style.visibility = 'hidden';
	}
	
	await nextTick();
	
	const rect = containerRef.value.getBoundingClientRect();
	const dropdownRect = dropdownRef.value.getBoundingClientRect();
	
	// Центрируем выпадающий список относительно кнопки
	const leftOffset = rect.left + (rect.width / 2) - (dropdownRect.width / 2);
	dropdownRef.value.style.top = `${rect.bottom + 2}px`;
	dropdownRef.value.style.left = `${leftOffset}px`;
	dropdownRef.value.style.visibility = 'visible';
};

onMounted(() => {
	// Обновляем позицию при изменении размера окна или скролле
	const handleUpdate = () => {
		if (dropdownRef.value?.style.display === 'block') {
			updateDropdownPosition();
		}
	};
	
	window.addEventListener('resize', handleUpdate);
	window.addEventListener('scroll', handleUpdate, true);
});

const handleCaptionSelect = (captionType: typeof captionTypes[0]) => {
	if (!props.insert || props.disabled) return;
	
	props.insert((selectedText: string) => {
		const placeholder = selectedText || 'текст подписи';
		const text = captionType.text.replace('текст подписи', placeholder);
		return {
			targetValue: text,
			select: !selectedText, // Select placeholder if no text selected
			deviationStart: selectedText ? 0 : 0,
			deviationEnd: selectedText ? 0 : -15, // -15 to select "текст подписи"
		};
	});
};
</script>

<style scoped>
.caption-toolbar-button {
	position: relative;
	display: inline-block;
}

.caption-toolbar-button:hover {
	z-index: 10000;
}

.caption-icon {
	width: 24px;
	height: 24px;
	padding: 2px;
	color: currentColor;
}

.toolbar-item-name {
	font-size: 12px;
	margin-top: 2px;
}

.caption-dropdown {
	display: none;
	position: fixed;
	z-index: 9999;
	margin-top: 0;
}

.caption-toolbar-button:hover .caption-dropdown,
.caption-dropdown:hover {
	display: block;
}

/* Добавляем невидимую прокладку сверху для плавного перехода мыши */
.caption-dropdown::before {
	content: '';
	position: absolute;
	top: -2px;
	left: 0;
	right: 0;
	height: 2px;
	background: transparent;
	pointer-events: none;
}

.caption-menu {
	margin: 0;
	padding: 0;
	border-radius: 3px;
	border: 1px solid var(--md-border-color);
	background-color: var(--md-bk-color);
	list-style: none;
}

.caption-menu-item {
	list-style: none;
	font-size: 12px;
	color: var(--md-color);
	padding-block: 4px;
	padding-inline: 10px;
	cursor: pointer;
	line-height: 16px;
	transition: background-color 0.2s;
}

.caption-menu-item:first-of-type {
	padding-block-start: 8px;
}

.caption-menu-item:last-of-type {
	padding-block-end: 8px;
}

.caption-menu-item:hover {
	background-color: var(--md-bk-hover-color);
}
</style>

