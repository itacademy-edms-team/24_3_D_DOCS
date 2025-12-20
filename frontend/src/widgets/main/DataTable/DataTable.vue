<template>
	<div class="table-container">
		<table v-if="items.length > 0" class="projects-table">
			<thead>
				<tr>
					<th class="title-col">Название</th>
					<th class="modified-col">Последнее изменение</th>
					<th class="actions-col"></th>
				</tr>
			</thead>
			<tbody>
				<tr
					v-for="item in items"
					:key="item.id"
					class="table-row"
				>
					<td class="title-col" @click.stop="handleItemClick(item)">
						<div v-if="editingId === item.id" class="edit-container">
							<input
								ref="editInput"
								v-model="editingName"
								class="edit-input"
								@blur="handleSaveEdit(item)"
								@keydown.enter="handleSaveEdit(item)"
								@keydown.esc="handleCancelEdit"
							/>
						</div>
						<span
							v-else
							class="item-name"
							@click.stop="handleNameClick(item)"
						>
							{{ item.name || 'Без названия' }}
						</span>
					</td>
					<td class="modified-col" @click.stop="handleItemClick(item)">
						{{ formatDate(item.updatedAt) }}
					</td>
					<td class="actions-col">
						<div class="actions-wrapper">
							<button
								class="menu-button"
								@click.stop="toggleMenu(item.id)"
								:aria-label="'Меню для ' + (item.name || 'элемента')"
							>
								<svg
									class="menu-icon"
									viewBox="0 0 24 24"
									fill="none"
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
								>
									<circle cx="12" cy="12" r="1" />
									<circle cx="12" cy="5" r="1" />
									<circle cx="12" cy="19" r="1" />
								</svg>
							</button>
							<div
								v-if="openMenuId === item.id"
								class="menu-dropdown"
								@click.stop
							>
								<button
									class="menu-item delete-item"
									@click="handleDelete(item)"
								>
									<svg
										class="delete-icon"
										viewBox="0 0 24 24"
										fill="none"
										stroke="currentColor"
										stroke-width="2"
										stroke-linecap="round"
										stroke-linejoin="round"
									>
										<polyline points="3 6 5 6 21 6" />
										<path
											d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
										/>
									</svg>
									Удалить
								</button>
							</div>
						</div>
					</td>
				</tr>
			</tbody>
		</table>
		<div v-else class="empty-state">
			<p class="empty-message">
				{{ isLoading ? 'Загрузка...' : 'Нет элементов для отображения' }}
			</p>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, nextTick, onMounted, onUnmounted } from 'vue';
import { formatDate } from '@/shared/utils/date';
import type { ProfileMeta } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';

interface Props {
	items: (ProfileMeta | DocumentMeta)[];
	isLoading?: boolean;
}

withDefaults(defineProps<Props>(), {
	isLoading: false,
});

const emit = defineEmits<{
	'item-click': [item: ProfileMeta | DocumentMeta];
	delete: [item: ProfileMeta | DocumentMeta];
	update: [item: ProfileMeta | DocumentMeta, name: string];
}>();

const openMenuId = ref<string | null>(null);
const editingId = ref<string | null>(null);
const editingName = ref('');
const editInput = ref<HTMLInputElement | null>(null);

function toggleMenu(itemId: string) {
	if (openMenuId.value === itemId) {
		openMenuId.value = null;
	} else {
		openMenuId.value = itemId;
	}
}

function handleItemClick(item: ProfileMeta | DocumentMeta) {
	emit('item-click', item);
}

function handleNameClick(item: ProfileMeta | DocumentMeta) {
	editingId.value = item.id;
	editingName.value = item.name || '';
	openMenuId.value = null;
	nextTick(() => {
		editInput.value?.focus();
		editInput.value?.select();
	});
}

function handleSaveEdit(item: ProfileMeta | DocumentMeta) {
	if (editingName.value.trim() && editingName.value !== item.name) {
		emit('update', item, editingName.value.trim());
	}
	editingId.value = null;
	editingName.value = '';
	openMenuId.value = null;
}

function handleCancelEdit() {
	editingId.value = null;
	editingName.value = '';
}

function handleDelete(item: ProfileMeta | DocumentMeta) {
	emit('delete', item);
	openMenuId.value = null;
}

function handleDocumentClick(event: MouseEvent) {
	const target = event.target as HTMLElement;
	if (openMenuId.value && !target.closest('.menu-dropdown') && !target.closest('.menu-button')) {
		openMenuId.value = null;
	}
}

onMounted(() => {
	document.addEventListener('click', handleDocumentClick);
});

onUnmounted(() => {
	document.removeEventListener('click', handleDocumentClick);
});
</script>

<style scoped>
.table-container {
	flex: 1;
	overflow-y: auto;
	padding: 0;
	width: 100%;
	position: relative;
}

.projects-table {
	width: 100%;
	border-collapse: collapse;
	margin-top: 1rem;
	table-layout: fixed;
}

.projects-table thead {
	position: sticky;
	top: 0;
	background: #0a0a0a;
	z-index: 10;
}

.projects-table th {
	padding: 1rem;
	text-align: center;
	font-size: 13px;
	font-weight: 600;
	color: #71717a;
	text-transform: uppercase;
	letter-spacing: 0.5px;
	border-bottom: 1px solid #27272a;
}

.projects-table td {
	padding: 1rem;
	border-bottom: 1px solid #27272a;
	text-align: center;
	position: relative;
}

.table-row {
	cursor: pointer;
	transition: background 0.15s;
}

.table-row:hover {
	background: rgba(99, 102, 241, 0.05);
}

.title-col {
	width: 50%;
}

.modified-col {
	width: 40%;
}

.actions-col {
	width: 10%;
	padding: 0.5rem !important;
}

.item-name {
	font-size: 14px;
	color: #e4e4e7;
	font-weight: 500;
	cursor: text;
	display: inline-block;
	padding: 2px 4px;
	border-radius: 4px;
	transition: background 0.2s;
}

.item-name:hover {
	background: rgba(99, 102, 241, 0.1);
}

.edit-container {
	display: flex;
	justify-content: center;
	align-items: center;
}

.edit-input {
	width: 100%;
	max-width: 300px;
	padding: 6px 10px;
	font-size: 14px;
	color: #e4e4e7;
	background: #18181b;
	border: 2px solid #6366f1;
	border-radius: 6px;
	outline: none;
	text-align: center;
	font-family: inherit;
	font-weight: 500;
}

.edit-input:focus {
	border-color: #818cf8;
	box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}

.actions-wrapper {
	position: relative;
	display: flex;
	justify-content: center;
	align-items: center;
}

.menu-button {
	background: transparent;
	border: none;
	cursor: pointer;
	padding: 4px 8px;
	border-radius: 6px;
	display: flex;
	align-items: center;
	justify-content: center;
	color: #71717a;
	transition: all 0.2s;
}

.menu-button:hover {
	background: rgba(99, 102, 241, 0.1);
	color: #a5b4fc;
}

.menu-icon {
	width: 20px;
	height: 20px;
}

.menu-dropdown {
	position: absolute;
	top: 100%;
	right: 0;
	margin-top: 4px;
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
	z-index: 100;
	min-width: 140px;
	overflow: hidden;
}

.menu-item {
	width: 100%;
	padding: 10px 16px;
	background: transparent;
	border: none;
	cursor: pointer;
	display: flex;
	align-items: center;
	gap: 8px;
	color: #e4e4e7;
	font-size: 14px;
	text-align: left;
	transition: background 0.2s;
	font-family: inherit;
}

.menu-item:hover {
	background: #27272a;
}

.delete-item {
	color: #f87171;
}

.delete-item:hover {
	background: rgba(248, 113, 113, 0.1);
	color: #ef4444;
}

.delete-icon {
	width: 16px;
	height: 16px;
	flex-shrink: 0;
}

.empty-state {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 4rem 2rem;
	width: 100%;
}

.empty-message {
	text-align: center;
	color: #71717a;
	font-size: 14px;
	margin: 0;
}
</style>
