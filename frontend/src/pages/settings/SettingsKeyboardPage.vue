<template>
	<div class="settings-keyboard-page">
		<header class="settings-header content-header">
			<button class="settings-back" @click="router.push('/settings')">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span>Назад</span>
			</button>
			<h1 class="page-title">Клавиши</h1>
		</header>

		<main class="settings-body content-body">
			<section class="keyboard-section">
				<p class="hint">
					Кликните в область ниже и нажимайте клавиши: события перехватываются только пока фокус в этой области
					(вкладка «Назад» и остальной интерфейс снаружи работают как обычно).
				</p>

				<div
					ref="focusPanel"
					class="capture-panel"
					tabindex="0"
					role="application"
					aria-label="Область перехвата клавиш"
					@keydown="onKeyDown"
					@keyup="onKeyUp"
					@blur="onPanelBlur"
				>
					<div class="capture-panel__chord">
						<span v-if="pressedCodes.length" class="mono">{{ pressedDisplay }}</span>
						<span v-else class="capture-panel__placeholder">Нажмите клавишу…</span>
					</div>

					<dl v-if="lastSnapshot" class="capture-panel__details">
						<div class="row">
							<dt>Событие</dt>
							<dd class="mono">{{ lastSnapshot.type }}</dd>
						</div>
						<div class="row">
							<dt>key</dt>
							<dd class="mono">{{ lastSnapshot.key }}</dd>
						</div>
						<div class="row">
							<dt>code</dt>
							<dd class="mono">{{ lastSnapshot.code }}</dd>
						</div>
						<div class="row">
							<dt>Модификаторы</dt>
							<dd class="mono">{{ lastSnapshot.modifiers }}</dd>
						</div>
					</dl>
				</div>
			</section>
		</main>
	</div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Icon from '@/components/Icon.vue';

const router = useRouter();

const focusPanel = ref<HTMLElement | null>(null);
const pressed = ref(new Set<string>());

type LastSnapshot = {
	type: string;
	key: string;
	code: string;
	modifiers: string;
};

const lastSnapshot = ref<LastSnapshot | null>(null);

const pressedCodes = computed(() => Array.from(pressed.value).sort());

const pressedDisplay = computed(() => pressedCodes.value.join(' + '));

function modifiersLine(e: KeyboardEvent): string {
	const parts: string[] = [];
	if (e.ctrlKey) parts.push('Ctrl');
	if (e.shiftKey) parts.push('Shift');
	if (e.altKey) parts.push('Alt');
	if (e.metaKey) parts.push('Meta');
	return parts.length ? parts.join('+') : '—';
}

function remember(e: KeyboardEvent, type: 'keydown' | 'keyup') {
	lastSnapshot.value = {
		type,
		key: e.key === ' ' ? 'Space' : e.key,
		code: e.code,
		modifiers: modifiersLine(e),
	};
}

function onKeyDown(e: KeyboardEvent) {
	e.preventDefault();
	e.stopPropagation();
	pressed.value = new Set(pressed.value).add(e.code);
	remember(e, 'keydown');
}

function onKeyUp(e: KeyboardEvent) {
	e.preventDefault();
	e.stopPropagation();
	const next = new Set(pressed.value);
	next.delete(e.code);
	pressed.value = next;
	remember(e, 'keyup');
}

function onPanelBlur() {
	pressed.value = new Set();
}

onMounted(() => {
	nextTick(() => focusPanel.value?.focus());
});
</script>

<style scoped>
.settings-keyboard-page {
	min-height: 100vh;
	background: var(--bg-primary);
	color: var(--text-primary);
	display: flex;
	flex-direction: column;
}

.settings-header {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	padding: var(--spacing-xl) var(--spacing-xl) var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
}

.settings-back {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
	padding: 0.6rem 1rem;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	cursor: pointer;
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
	font-family: inherit;
	transition: background 0.2s, border-color 0.2s;
}

.settings-back:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
}

.page-title {
	margin: 0;
	font-size: 28px;
	font-weight: 700;
	color: var(--text-primary);
}

.settings-body {
	flex: 1;
	display: flex;
	justify-content: center;
	align-items: flex-start;
	padding: var(--spacing-xl);
}

.keyboard-section {
	width: 100%;
	max-width: 560px;
}

.hint {
	margin: 0 0 var(--spacing-lg);
	font-size: 14px;
	line-height: 1.55;
	color: var(--text-secondary);
}

.capture-panel {
	min-height: 220px;
	padding: var(--spacing-xl);
	border-radius: var(--radius-md);
	border: 2px dashed var(--border-color);
	background: var(--bg-secondary);
	outline: none;
}

.capture-panel:focus {
	border-color: rgba(var(--accent-rgb, 16, 185, 129), 0.45);
	box-shadow: 0 0 0 3px rgba(var(--accent-rgb, 16, 185, 129), 0.15);
}

.capture-panel__chord {
	min-height: 2.5rem;
	margin-bottom: var(--spacing-lg);
	font-size: 1.25rem;
	font-weight: 600;
}

.capture-panel__placeholder {
	color: var(--text-tertiary);
	font-weight: 500;
}

.mono {
	font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New',
		monospace;
	font-size: 0.95em;
	word-break: break-all;
}

.capture-panel__details {
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.row {
	display: grid;
	grid-template-columns: 8.5rem 1fr;
	gap: var(--spacing-md);
	align-items: baseline;
	font-size: 14px;
}

.row dt {
	margin: 0;
	color: var(--text-secondary);
	font-weight: 600;
}

.row dd {
	margin: 0;
	color: var(--text-primary);
}
</style>
