<template>
	<div class="settings-keyboard-page">
		<header class="settings-header content-header">
			<button class="settings-back" @click="router.push('/settings')">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span>Назад</span>
			</button>
			<h1 class="page-title">Клавиши редактора</h1>
		</header>

		<main class="settings-body content-body">
			<section class="keyboard-section">
				<p class="hint">
					Назначьте сочетания для действий в редакторе (пока только сохранение в профиль, без вызова в
					TipTap). Нажмите «Назначить», затем комбинацию в рамке ниже. Esc — отменить запись.
				</p>

				<p v-if="loadError" class="error-banner">{{ loadError }}</p>
				<p v-if="saveError" class="error-banner">{{ saveError }}</p>

				<div
					ref="focusPanel"
					class="capture-panel"
					tabindex="0"
					role="region"
					:aria-label="recordingActionId ? 'Ввод сочетания клавиш' : 'Область назначения сочетаний'"
					@keydown="onPanelKeyDown"
					@keyup="onPanelKeyUp"
					@blur="onPanelBlur"
				>
					<p v-if="recordingActionId" class="capture-panel__recording">
						Запись для: <strong>{{ recordingLabel }}</strong>
					</p>
					<p v-else class="capture-panel__idle">
						{{ recordingHint }}
					</p>
					<div v-if="recordingActionId && pressedCodes.length" class="mono capture-panel__preview">
						{{ pressedDisplay }}
					</div>
				</div>

				<div class="binds-card">
					<template v-for="(section, si) in EDITOR_HOTKEY_SECTIONS" :key="si">
						<h3 v-if="section.title" class="binds-group-title">{{ section.title }}</h3>
						<ul class="binds-list">
							<li v-for="row in section.rows" :key="row.id" class="binds-row">
								<div class="binds-row__label">{{ row.label }}</div>
								<div class="binds-row__chord mono">
									{{ formatBinding(bindings[row.id]) }}
								</div>
								<div class="binds-row__actions">
									<Button
										variant="secondary"
										class="binds-row__btn"
										:disabled="!!recordingActionId && recordingActionId !== row.id"
										@click="startRecording(row.id, row.label)"
									>
										{{ recordingActionId === row.id ? '…' : 'Назначить' }}
									</Button>
									<Button
										variant="secondary"
										class="binds-row__btn"
										:disabled="!bindings[row.id] || !!recordingActionId"
										@click="clearBinding(row.id)"
									>
										Сбросить
									</Button>
								</div>
							</li>
						</ul>
					</template>
				</div>

				<div class="save-bar">
					<Button variant="primary" :isLoading="isSaving" :disabled="isLoading || !!recordingActionId" @click="save">
						Сохранить
					</Button>
				</div>
			</section>
		</main>
	</div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Icon from '@/components/Icon.vue';
import Button from '@/shared/ui/Button/Button.vue';
import EditorHotkeysAPI from '@/shared/api/EditorHotkeysAPI';
import BadRequest from '@/shared/api/error/BadRequest';
import {
	EDITOR_HOTKEY_SECTIONS,
	catalogIdsForPayload,
	chordFromEvent,
	emptyBindingsPayload,
	formatHotkeyChord,
	isModifierOnlyCode,
	type EditorHotkeyActionId,
	type EditorHotkeyChord,
} from '@/shared/constants/editorHotkeyCatalog';

const router = useRouter();

const focusPanel = ref<HTMLElement | null>(null);
const bindings = ref<Record<string, EditorHotkeyChord | null>>(emptyBindingsPayload());
const isLoading = ref(true);
const isSaving = ref(false);
const loadError = ref('');
const saveError = ref('');
const recordingActionId = ref<EditorHotkeyActionId | null>(null);
const recordingLabel = ref('');
const pressed = ref(new Set<string>());

const recordingHint = computed(() =>
	recordingActionId.value
		? ''
		: 'Сначала нажмите «Назначить» у нужной строки, затем сочетание здесь.',
);

const pressedCodes = computed(() => Array.from(pressed.value).sort());
const pressedDisplay = computed(() => pressedCodes.value.join(' + '));

function formatBinding(c: EditorHotkeyChord | null | undefined): string {
	if (!c) return '—';
	return formatHotkeyChord(c);
}

function startRecording(id: EditorHotkeyActionId, label: string) {
	saveError.value = '';
	if (recordingActionId.value === id) {
		return;
	}
	recordingActionId.value = id;
	recordingLabel.value = label;
	pressed.value = new Set();
	nextTick(() => focusPanel.value?.focus());
}

function stopRecording() {
	recordingActionId.value = null;
	recordingLabel.value = '';
	pressed.value = new Set();
}

function clearBinding(id: EditorHotkeyActionId) {
	saveError.value = '';
	bindings.value = { ...bindings.value, [id]: null };
}

function onPanelKeyDown(e: KeyboardEvent) {
	if (!recordingActionId.value) {
		return;
	}
	e.preventDefault();
	e.stopPropagation();
	pressed.value = new Set(pressed.value).add(e.code);

	if (e.key === 'Escape') {
		stopRecording();
		return;
	}

	if (!isModifierOnlyCode(e.code)) {
		const chord = chordFromEvent(e);
		bindings.value = { ...bindings.value, [recordingActionId.value]: chord };
		stopRecording();
	}
}

function onPanelKeyUp(e: KeyboardEvent) {
	if (!recordingActionId.value) {
		return;
	}
	e.preventDefault();
	e.stopPropagation();
	const next = new Set(pressed.value);
	next.delete(e.code);
	pressed.value = next;
}

function onPanelBlur() {
	pressed.value = new Set();
}

function payloadFromBindings(): Record<string, EditorHotkeyChord | null> {
	const out: Record<string, EditorHotkeyChord | null> = {};
	for (const id of catalogIdsForPayload()) {
		out[id] = bindings.value[id] ?? null;
	}
	return out;
}

async function load() {
	loadError.value = '';
	isLoading.value = true;
	try {
		const res = await EditorHotkeysAPI.getEditorHotkeys();
		const next = emptyBindingsPayload();
		for (const id of catalogIdsForPayload()) {
			const v = res.bindings[id];
			next[id] = v && typeof v === 'object' && v.code ? v : null;
		}
		bindings.value = next;
	} catch (err) {
		loadError.value =
			err instanceof BadRequest ? err.message : 'Не удалось загрузить сочетания клавиш.';
	} finally {
		isLoading.value = false;
		nextTick(() => focusPanel.value?.focus());
	}
}

async function save() {
	saveError.value = '';
	isSaving.value = true;
	try {
		const res = await EditorHotkeysAPI.putEditorHotkeys({ bindings: payloadFromBindings() });
		const next = emptyBindingsPayload();
		for (const id of catalogIdsForPayload()) {
			const v = res.bindings[id];
			next[id] = v && typeof v === 'object' && v.code ? v : null;
		}
		bindings.value = next;
	} catch (err) {
		saveError.value =
			err instanceof BadRequest ? err.message : 'Не удалось сохранить сочетания клавиш.';
	} finally {
		isSaving.value = false;
	}
}

onMounted(() => {
	load();
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
	max-width: 640px;
}

.hint {
	margin: 0 0 var(--spacing-md);
	font-size: 14px;
	line-height: 1.55;
	color: var(--text-secondary);
}

.error-banner {
	margin: 0 0 var(--spacing-md);
	padding: 0.65rem 1rem;
	border-radius: var(--radius-md);
	background: rgba(239, 68, 68, 0.12);
	color: var(--danger, #ef4444);
	border: 1px solid rgba(239, 68, 68, 0.35);
	font-size: 14px;
}

.capture-panel {
	margin-bottom: var(--spacing-xl);
	padding: var(--spacing-lg);
	border-radius: var(--radius-md);
	border: 2px dashed var(--border-color);
	background: var(--bg-secondary);
	outline: none;
}

.capture-panel:focus {
	border-color: rgba(var(--accent-rgb, 16, 185, 129), 0.45);
	box-shadow: 0 0 0 3px rgba(var(--accent-rgb, 16, 185, 129), 0.15);
}

.capture-panel__recording,
.capture-panel__idle {
	margin: 0;
	font-size: 14px;
	line-height: 1.5;
	color: var(--text-secondary);
}

.capture-panel__recording strong {
	color: var(--text-primary);
}

.capture-panel__preview {
	margin-top: var(--spacing-sm);
	font-size: 1rem;
	font-weight: 600;
	color: var(--text-primary);
}

.mono {
	font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New',
		monospace;
	font-size: 0.9em;
	word-break: break-all;
}

.binds-card {
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	background: var(--bg-secondary);
	padding: var(--spacing-md) var(--spacing-lg);
}

.binds-group-title {
	margin: var(--spacing-lg) 0 var(--spacing-sm);
	font-size: 0.85rem;
	font-weight: 700;
	text-transform: uppercase;
	letter-spacing: 0.04em;
	color: var(--text-tertiary);
}

.binds-group-title:first-child {
	margin-top: 0;
}

.binds-list {
	list-style: none;
	padding: 0;
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}

.binds-row {
	display: grid;
	grid-template-columns: 1fr minmax(7rem, 9.5rem) auto;
	gap: var(--spacing-md);
	align-items: center;
	padding: var(--spacing-sm) 0;
	border-bottom: 1px solid var(--border-color);
}

.binds-row:last-child {
	border-bottom: none;
}

.binds-row__label {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.binds-row__chord {
	font-size: 13px;
	color: var(--text-secondary);
	text-align: right;
}

.binds-row__actions {
	display: flex;
	gap: var(--spacing-xs);
	flex-wrap: wrap;
	justify-content: flex-end;
}

.binds-row__btn {
	padding: 0.45rem 0.85rem;
	font-size: 13px;
}

.save-bar {
	margin-top: var(--spacing-xl);
	display: flex;
	justify-content: flex-end;
}

@media (max-width: 560px) {
	.binds-row {
		grid-template-columns: 1fr;
		align-items: stretch;
	}

	.binds-row__chord {
		text-align: left;
	}

	.binds-row__actions {
		justify-content: flex-start;
	}
}
</style>
