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
const liveLatex = ref('');

function getVirtualKeyboard() {
	return typeof window !== 'undefined' ? window.mathVirtualKeyboard : undefined;
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
		getVirtualKeyboard()?.hide();
	}
}

function onWindowKeydown(e: KeyboardEvent) {
	if (!props.modelValue) return;
	if (e.key === 'Escape') close();
}

async function syncFieldWhenOpen() {
	if (!props.modelValue) return;
	await nextTick();
	const field = mathFieldRef.value;
	if (!field) return;
	field.value = props.initialLatex;
	liveLatex.value = props.initialLatex;
	field.focus();
	requestAnimationFrame(() => {
		getVirtualKeyboard()?.show();
	});
}

watch(
	() => props.modelValue,
	(open) => {
		if (!open) {
			liveLatex.value = '';
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
	getVirtualKeyboard()?.hide();
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
</style>

<style>
math-virtual-keyboard {
	z-index: 1400 !important;
}
</style>
