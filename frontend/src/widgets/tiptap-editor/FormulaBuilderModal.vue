<template>
	<Modal
		:model-value="modelValue"
		title="Конструктор формулы"
		size="lg"
		@update:model-value="onModalUpdate"
	>
		<div class="formula-builder-modal__body">
			<p v-if="isBlock" class="formula-builder-modal__hint">Блочная формула</p>
			<p v-else class="formula-builder-modal__hint">Встроенная формула</p>
			<math-field
				ref="mathFieldRef"
				class="formula-builder-modal__field"
				virtual-keyboard-mode="manual"
				@input="onMathInput"
			/>
		</div>
		<template #footer>
			<div class="formula-builder-modal__footer">
				<Button variant="secondary" type="button" @click="close">Отмена</Button>
				<Button type="button" :disabled="!canApply" @click="apply">Применить</Button>
			</div>
		</template>
		<template #keyboard>
			<div ref="kbContainerRef" class="formula-builder-modal__keyboard-slot" />
		</template>
	</Modal>
</template>

<script setup lang="ts">
import 'mathlive';
import 'mathlive/static.css';
import type { MathfieldElement } from 'mathlive';
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import Button from '@/shared/ui/Button/Button.vue';

const props = defineProps<{
	modelValue: boolean;
	initialLatex: string;
	isBlock: boolean;
}>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	apply: [latex: string];
}>();

const mathFieldRef = ref<MathfieldElement | null>(null);
const kbContainerRef = ref<HTMLElement | null>(null);
const liveLatex = ref('');

function getVirtualKeyboard() {
	return typeof window !== 'undefined' ? window.mathVirtualKeyboard : undefined;
}

let keyboardGeometryListener: ((ev: Event) => void) | null = null;

function setKeyboardSlotHeightFromEvent(ev?: Event) {
	const el = kbContainerRef.value;
	const kb = getVirtualKeyboard();
	if (!el || !kb) return;
	const detail = ev && 'detail' in ev ? (ev as CustomEvent<{ boundingRect?: DOMRect }>).detail : undefined;
	const h = detail?.boundingRect?.height ?? kb.boundingRect?.height ?? 0;
	el.style.height = h > 0 ? `${Math.ceil(h)}px` : '';
}

function detachKeyboardGeometryListener() {
	const kb = getVirtualKeyboard();
	if (kb && keyboardGeometryListener) {
		kb.removeEventListener('geometrychange', keyboardGeometryListener);
		keyboardGeometryListener = null;
	}
}

function attachKeyboardGeometryListener() {
	const kb = getVirtualKeyboard();
	if (!kb) return;
	detachKeyboardGeometryListener();
	keyboardGeometryListener = (ev) => setKeyboardSlotHeightFromEvent(ev);
	kb.addEventListener('geometrychange', keyboardGeometryListener);
}

function teardownVirtualKeyboard() {
	detachKeyboardGeometryListener();
	if (kbContainerRef.value) {
		kbContainerRef.value.style.height = '';
	}
	const kb = getVirtualKeyboard();
	if (kb) {
		kb.hide();
		kb.container = document.body;
	}
}

const canApply = computed(() => liveLatex.value.trim().length > 0);

function onMathInput(event: Event) {
	const field = event.target as MathfieldElement;
	liveLatex.value = field.value;
}

function close() {
	emit('update:modelValue', false);
}

function apply() {
	const v = liveLatex.value.trim();
	if (!v) return;
	emit('apply', v);
	emit('update:modelValue', false);
}

function onModalUpdate(open: boolean) {
	emit('update:modelValue', open);
	if (!open) {
		teardownVirtualKeyboard();
	}
}

function onWindowKeydown(e: KeyboardEvent) {
	if (!props.modelValue) return;
	if (e.key === 'Escape') close();
}

async function syncFieldWhenOpen() {
	if (!props.modelValue) return;
	await nextTick();
	await nextTick();
	const field = mathFieldRef.value;
	if (!field) return;
	field.value = props.initialLatex;
	liveLatex.value = props.initialLatex;
	const kb = getVirtualKeyboard();
	if (!kb) return;
	const slot = kbContainerRef.value;
	kb.container = slot ?? document.body;
	attachKeyboardGeometryListener();
	field.focus();
	requestAnimationFrame(() => {
		kb.show();
		setKeyboardSlotHeightFromEvent();
		requestAnimationFrame(() => setKeyboardSlotHeightFromEvent());
	});
}

watch(
	() => props.modelValue,
	(open) => {
		if (!open) {
			liveLatex.value = '';
			teardownVirtualKeyboard();
			return;
		}
		void syncFieldWhenOpen();
	},
);

watch(
	() => props.initialLatex,
	() => {
		if (props.modelValue) void syncFieldWhenOpen();
	},
);

onMounted(() => {
	window.addEventListener('keydown', onWindowKeydown);
});

onBeforeUnmount(() => {
	window.removeEventListener('keydown', onWindowKeydown);
	teardownVirtualKeyboard();
});
</script>

<style scoped>
.formula-builder-modal__body {
	display: flex;
	flex-direction: column;
	gap: 10px;
}

.formula-builder-modal__hint {
	margin: 0;
	font-size: 13px;
	color: var(--text-secondary, #64748b);
}

.formula-builder-modal__field {
	width: 100%;
	min-height: 3.5rem;
	padding: 10px 12px;
	border-radius: 8px;
	border: 1px solid var(--border-color, #cbd5e1);
	background: var(--bg-primary, #fff);
	color: var(--text-primary, #0f172a);
	font-size: 16px;
}

.formula-builder-modal__footer {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
}

/* MathLive positions the panel with position:absolute inside this container */
.formula-builder-modal__keyboard-slot {
	position: relative;
	width: 100%;
	flex-shrink: 0;
	overflow: visible;
}
</style>
