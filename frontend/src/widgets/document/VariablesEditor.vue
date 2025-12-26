<template>
	<div class="variables-editor">
		<div class="variables-editor-header">
			<h3 class="variables-editor-title">Переменные титульного листа</h3>
			<button
				v-if="titlePageVariableKeys.length > 0"
				class="btn-insert-template"
				@click="handleInsertAllFromTemplate"
			>
				+ Вставить из шаблона
			</button>
		</div>

		<div
			v-if="titlePageVariableKeys.length > 0"
			class="variables-info"
		>
			<strong>Переменные из титульного листа:</strong>
			<ul class="variables-list">
				<li
					v-for="key in titlePageVariableKeys"
					:key="key"
					:class="{ 'defined': isVariableDefined(key) }"
				>
					{{ key }} {{ isVariableDefined(key) ? '✓' : '(не определена)' }}
				</li>
			</ul>
		</div>

		<div class="variables-content">
			<div
				v-if="localVariables.length === 0"
				class="empty-state"
			>
				Нет переменных. Добавьте переменную для использования в титульном листе.
			</div>
			<div
				v-else
				class="variables-list"
			>
				<div
					v-for="(variable, index) in localVariables"
					:key="index"
					class="variable-row"
				>
					<input
						type="text"
						class="variable-input variable-key"
						:value="variable.key"
						placeholder="Ключ"
						@input="handleUpdate(index, { key: ($event.target as HTMLInputElement).value })"
					/>
					<input
						type="text"
						class="variable-input variable-value"
						:value="variable.value"
						placeholder="Значение"
						@input="handleUpdate(index, { value: ($event.target as HTMLInputElement).value })"
					/>
					<button
						class="btn-delete"
						@click="handleDelete(index)"
					>
						Удалить
					</button>
				</div>
			</div>
		</div>

		<div class="variables-actions">
			<button
				class="btn-add"
				@click="handleAdd"
			>
				+ Добавить переменную
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';

interface Props {
	variables: Record<string, string>;
	titlePageVariableKeys: string[];
}

interface Emits {
	(e: 'update', variables: Record<string, string>): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localVariables = ref<Array<{ key: string; value: string }>>(() => {
	return Object.entries(props.variables).map(([key, value]) => ({ key, value }));
});

watch(
	() => props.variables,
	(newVariables) => {
		localVariables.value = Object.entries(newVariables).map(([key, value]) => ({ key, value }));
	},
	{ deep: true }
);

function isVariableDefined(key: string): boolean {
	return props.variables[key] !== undefined && props.variables[key] !== '';
}

function handleAdd() {
	localVariables.value = [...localVariables.value, { key: '', value: '' }];
	updateVariables();
}

function handleUpdate(index: number, updates: Partial<{ key: string; value: string }>) {
	const newVars = [...localVariables.value];
	newVars[index] = { ...newVars[index], ...updates };
	localVariables.value = newVars;
	updateVariables();
}

function handleDelete(index: number) {
	const newVars = localVariables.value.filter((_, i) => i !== index);
	localVariables.value = newVars;
	updateVariables();
}

function handleInsertAllFromTemplate() {
	const newVariables: Record<string, string> = { ...props.variables };

	props.titlePageVariableKeys.forEach((key) => {
		if (!newVariables[key]) {
			newVariables[key] = '';
		}
	});

	emit('update', newVariables);
}

function updateVariables() {
	const result: Record<string, string> = {};
	localVariables.value.forEach(({ key, value }) => {
		if (key.trim()) {
			result[key.trim()] = value;
		}
	});
	emit('update', result);
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
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1rem;
}

.variables-editor-title {
	margin: 0;
	font-size: 1.1rem;
	font-weight: 600;
	color: #e4e4e7;
}

.btn-insert-template {
	padding: 0.5rem 1rem;
	background: #28a745;
	color: white;
	border: none;
	border-radius: 6px;
	cursor: pointer;
	font-size: 0.875rem;
	font-weight: 500;
	transition: background 0.2s;
	font-family: inherit;
}

.btn-insert-template:hover {
	background: #218838;
}

.variables-info {
	margin-bottom: 1rem;
	padding: 0.75rem;
	background: rgba(59, 130, 246, 0.1);
	border: 1px solid rgba(59, 130, 246, 0.2);
	border-radius: 6px;
	font-size: 0.875rem;
	color: #e4e4e7;
}

.variables-info strong {
	display: block;
	margin-bottom: 0.5rem;
}

.variables-info ul {
	margin: 0.5rem 0 0 0;
	padding-left: 1.5rem;
}

.variables-info li {
	margin-bottom: 0.25rem;
}

.variables-info li.defined {
	color: #28a745;
}

.variables-info li:not(.defined) {
	color: #dc3545;
}

.variables-content {
	margin-bottom: 1rem;
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
	gap: 0.5rem;
}

.variable-row {
	display: flex;
	gap: 0.5rem;
	align-items: center;
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
}

.variable-input:focus {
	border-color: #6366f1;
}

.variable-key {
	flex: 1;
}

.variable-value {
	flex: 2;
}

.btn-delete {
	padding: 0.5rem 0.75rem;
	background: #dc3545;
	color: white;
	border: none;
	border-radius: 6px;
	cursor: pointer;
	font-size: 0.875rem;
	transition: background 0.2s;
	font-family: inherit;
}

.btn-delete:hover {
	background: #c82333;
}

.variables-actions {
	display: flex;
	justify-content: flex-start;
}

.btn-add {
	padding: 0.5rem 1rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 6px;
	cursor: pointer;
	font-size: 0.875rem;
	font-weight: 500;
	transition: background 0.2s;
	font-family: inherit;
}

.btn-add:hover {
	background: #4f46e5;
}
</style>
