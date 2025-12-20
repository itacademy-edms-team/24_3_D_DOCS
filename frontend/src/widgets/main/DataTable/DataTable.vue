<template>
	<div class="table-container">
		<table v-if="items.length > 0" class="projects-table">
			<thead>
				<tr>
					<th class="title-col">Название</th>
					<th class="modified-col">Последнее изменение</th>
				</tr>
			</thead>
			<tbody>
				<tr
					v-for="item in items"
					:key="item.id"
					class="table-row"
					@click="$emit('item-click', item)"
				>
					<td class="title-col">
						<span class="item-name">{{ item.name || 'Без названия' }}</span>
					</td>
					<td class="modified-col">{{ formatDate(item.updatedAt) }}</td>
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

defineEmits<{
	'item-click': [item: ProfileMeta | DocumentMeta];
}>();
</script>

<style scoped>
.table-container {
	flex: 1;
	overflow-y: auto;
	padding: 0;
	width: 100%;
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

.item-name {
	font-size: 14px;
	color: #e4e4e7;
	font-weight: 500;
}

.modified-col {
	width: 50%;
	font-size: 13px;
	color: #a1a1aa;
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
