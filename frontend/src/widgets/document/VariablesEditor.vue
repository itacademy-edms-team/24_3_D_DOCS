<template>
	<div class="variables-editor">
		<div class="variables-editor-header">
			<h3 class="variables-editor-title">Переменные титульного листа</h3>
		</div>

		<div class="variables-content">
			<div
				v-if="titlePageVariableKeys.length === 0"
				class="empty-state"
			>
				В титульном листе нет переменных.
			</div>
			<div
				v-else
				class="variables-list"
			>
				<div
					v-for="key in titlePageVariableKeys"
					:key="key"
					class="variable-row"
				>
					<label class="variable-label">{{ key }}</label>
					<input
						type="text"
						class="variable-input variable-value"
						:value="variables[key] || ''"
						placeholder="Значение"
						@input="handleValueChange(key, ($event.target as HTMLInputElement).value)"
					/>
				</div>
			</div>
		</div>

		<div class="variables-actions">
			<button
				class="btn-save"
				:disabled="saving"
				@click="handleSave"
			>
				{{ saving ? 'Сохранение...' : 'Сохранить' }}
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

interface Props {
	variables: Record<string, string>;
	titlePageVariableKeys: string[];
	saving?: boolean;
}

interface Emits {
	(e: 'update', variables: Record<string, string>): void;
}

const props = withDefaults(defineProps<Props>(), {
	saving: false,
});

const emit = defineEmits<Emits>();

const localValues = ref<Record<string, string>>({ ...props.variables });

watch(
	() => props.variables,
	(newVariables) => {
		localValues.value = { ...newVariables };
	},
	{ deep: true }
);

function handleValueChange(key: string, value: string) {
	localValues.value[key] = value;
}

function handleSave() {
	emit('update', { ...localValues.value });
}
</script>

<style scoped>
.variables-editor {
	padding: 1.5rem;
	border: 1px solid #27272a;
	border-radius: 8px;
	background: #18181b;
	width: 100%;
	max-width: 800px;
}

.variables-editor-header {
	margin-bottom: 1.5rem;
}

.variables-editor-title {
	margin: 0;
	font-size: 1.1rem;
	font-weight: 600;
	color: #e4e4e7;
}

.variables-content {
	margin-bottom: 1.5rem;
}

.empty-state {
	color: #71717a;
	font-size: 0.875rem;
	padding: 1rem;
	text-align: center;
}

.variables-list {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.variable-row {
	display: flex;
	flex-direction: row;
	align-items: center;
	gap: 1rem;
}

.variable-label {
	color: #e4e4e7;
	font-size: 0.875rem;
	font-weight: 500;
	flex: 0 0 150px;
}

.variable-input {
	padding: 0.5rem 0.75rem;
	background: #0a0a0a;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 14px;
	font-family: inherit;
	outline: none;
	transition: border-color 0.2s;
	width: 100%;
	box-sizing: border-box;
}

.variable-input:focus {
	border-color: #6366f1;
}

.variable-value {
	flex: 1;
}

.variables-actions {
	display: flex;
	justify-content: flex-end;
	padding-top: 1rem;
	border-top: 1px solid #27272a;
}

.btn-save {
	padding: 0.75rem 1.5rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.2s;
	font-family: inherit;
}

.btn-save:hover:not(:disabled) {
	background: #4f46e5;
}

.btn-save:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}
</style>
