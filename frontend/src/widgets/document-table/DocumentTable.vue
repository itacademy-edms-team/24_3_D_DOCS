<template>
	<div class="document-table">
		<table class="document-table__table">
			<thead>
				<tr>
					<th class="document-table__col-title">Название</th>
					<th class="document-table__col-meta" aria-hidden="true"></th>
					<th class="document-table__col-actions">Действия</th>
				</tr>
			</thead>
			<tbody>
				<tr
					v-for="document in sortedDocuments"
					:key="document.id"
					class="document-table__row"
					@click="handleRowClick(document)"
				>
					<td class="document-table__col-title">
						<span class="document-table__name">{{ document.name }}</span>
					</td>
					<td class="document-table__col-meta">
						<div class="document-table__meta-cell">
							<span
								v-if="document.isShared"
								class="document-table__badge document-table__badge--shared"
								:title="document.ownerName ? `Владелец: ${document.ownerName}` : 'Общий документ'"
							>
								<Icon name="share_network" :size="14" decorative />
								<span class="document-table__badge-text">
									Общий<span v-if="document.ownerName"> · {{ document.ownerName }}</span>
								</span>
							</span>
							<span
								v-else-if="document.profileName"
								class="document-table__badge document-table__badge--profile"
							>
								<Icon name="palette" :size="14" decorative />
								<span class="document-table__badge-text">{{ document.profileName }}</span>
							</span>
							<span v-else class="document-table__empty">—</span>
						</div>
					</td>
					<td class="document-table__col-actions" @click.stop>
						<div class="document-table__actions">
							<button
								class="document-table__action-btn"
								@click.stop="handleAction(document, 'open')"
								title="Открыть"
							>
								<Icon name="open_external" :size="18" decorative />
							</button>
							<button
								class="document-table__action-btn"
								@click.stop="handleAction(document, 'export-pdf')"
								title="Экспорт PDF"
							>
								<Icon name="file_pdf" :size="18" decorative />
							</button>
						<button
							v-if="!document.isShared"
							class="document-table__action-btn document-table__action-btn--delete"
							@click.stop="handleAction(document, 'delete')"
							title="Удалить"
						>
							<Icon name="trash" :size="18" decorative />
						</button>
						<button
							v-if="document.isShared"
							class="document-table__action-btn document-table__action-btn--leave"
							@click.stop="handleAction(document, 'leave')"
							title="Покинуть соавторство"
						>
							<Icon name="log_out" :size="18" decorative />
						</button>
						</div>
					</td>
				</tr>
				<tr v-if="sortedDocuments.length === 0" class="document-table__empty-row">
					<td colspan="3" class="document-table__empty-message">
						{{ isLoading ? 'Загрузка...' : 'Нет документов' }}
					</td>
				</tr>
			</tbody>
		</table>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Icon from '@/components/Icon.vue';
import type { DocumentMeta } from '@/entities/document/types';

interface Props {
	documents: DocumentMeta[];
	isLoading?: boolean;
	sortBy?: 'name' | 'updatedAt';
	sortOrder?: 'asc' | 'desc';
}

const props = withDefaults(defineProps<Props>(), {
	isLoading: false,
	sortBy: 'updatedAt',
	sortOrder: 'desc',
});

const emit = defineEmits<{
	'update:sortBy': [field: 'name' | 'updatedAt'];
	'update:sortOrder': [order: 'asc' | 'desc'];
	'row-click': [document: DocumentMeta];
	action: [document: DocumentMeta, action: string];
}>();

const sortedDocuments = computed(() => {
	const docs = [...props.documents];
	docs.sort((a, b) => {
		let aValue: string | number;
		let bValue: string | number;

		if (props.sortBy === 'name') {
			aValue = (a.name || '').toLowerCase();
			bValue = (b.name || '').toLowerCase();
		} else {
			aValue = new Date(a.updatedAt).getTime();
			bValue = new Date(b.updatedAt).getTime();
		}

		if (props.sortOrder === 'asc') {
			return aValue > bValue ? 1 : -1;
		}
		return aValue < bValue ? 1 : -1;
	});

	return docs;
});

const toggleSort = (field: 'name' | 'updatedAt') => {
	if (props.sortBy === field) {
		emit('update:sortOrder', props.sortOrder === 'asc' ? 'desc' : 'asc');
	} else {
		emit('update:sortBy', field);
		emit('update:sortOrder', 'desc');
	}
};

const handleRowClick = (document: DocumentMeta) => {
	emit('row-click', document);
};

const handleAction = (document: DocumentMeta, action: string) => {
	emit('action', document, action);
};
</script>

<style scoped>
.document-table {
	width: 100%;
}

.document-table__table {
	width: 100%;
	border-collapse: collapse;
}

.document-table__table thead {
	position: sticky;
	top: 0;
	background: var(--bg-primary);
	z-index: 10;
}

.document-table__table th {
	padding: var(--spacing-md);
	text-align: left;
	font-size: 13px;
	font-weight: 600;
	color: var(--text-secondary);
	text-transform: uppercase;
	letter-spacing: 0.5px;
	border-bottom: 2px solid var(--border-color);
}

.document-table__table td {
	padding: var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
}

.document-table__row {
	cursor: pointer;
	transition: all 0.2s ease;
}

.document-table__row:hover {
	background: var(--bg-secondary);
	transform: scale(1.002);
}

.document-table__row:active {
	transform: scale(1);
}

.document-table__col-title {
	min-width: 200px;
}

.document-table__col-title--sortable {
	cursor: pointer;
	user-select: none;
}

.document-table__col-title--sortable:hover {
	color: var(--accent);
}

.document-table__name {
	font-size: 14px;
	color: var(--text-primary);
	font-weight: 500;
}

.document-table__col-meta {
	width: 200px;
	min-width: 160px;
	vertical-align: middle;
}

.document-table__meta-cell {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	min-height: 36px;
}

.document-table__badge {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	max-width: 100%;
	padding: 6px 10px;
	border-radius: var(--radius-md);
	font-size: 12px;
	font-weight: 600;
	border: 1px solid transparent;
}

.document-table__badge-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.document-table__badge--profile {
	background: var(--accent-light);
	color: var(--accent);
	border-color: rgba(var(--accent-rgb, 37, 99, 235), 0.2);
}

.document-table__badge--shared {
	background: var(--bg-secondary);
	color: var(--text-primary);
	border-color: var(--border-color);
	box-shadow: var(--shadow-sm);
}

.document-table__badge--shared :deep(.icon) {
	flex-shrink: 0;
	color: var(--accent);
}

.document-table__empty {
	color: var(--text-tertiary);
	font-size: 13px;
}

.document-table__col-actions {
	width: 160px;
}

.document-table__actions {
	display: flex;
	gap: var(--spacing-xs);
}

.document-table__action-btn {
	width: 36px;
	height: 36px;
	background: transparent;
	border: 1px solid var(--border-color);
	border-radius: 8px;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--text-primary);
	transition: all 0.2s ease;
	position: relative;
}

.document-table__action-btn:hover {
	background: var(--bg-tertiary);
	color: var(--text-primary);
	border-color: var(--border-hover);
	transform: translateY(-1px);
}

.document-table__action-btn:active {
	transform: translateY(0) scale(0.95);
}

.document-table__action-btn:not(:disabled):active {
	background: var(--bg-tertiary);
}

.document-table__action-btn--delete:hover,
.document-table__action-btn--leave:hover {
	background: rgba(239, 68, 68, 0.1);
	color: var(--danger);
	border-color: var(--danger);
}

.document-table__action-btn--generating {
	background: var(--accent-light) !important;
	border-color: var(--accent) !important;
	color: var(--accent) !important;
	cursor: not-allowed;
	pointer-events: none;
	animation: generatingPulse 2s ease-in-out infinite;
	position: relative;
	overflow: hidden;
}

.document-table__action-btn--generating::before {
	content: '';
	position: absolute;
	top: 0;
	left: -100%;
	width: 100%;
	height: 100%;
	background: linear-gradient(
		90deg,
		transparent,
		rgba(var(--accent-rgb, 16, 185, 129), 0.3),
		transparent
	);
	animation: shimmer 2s infinite;
}

.document-table__action-btn:disabled {
	cursor: not-allowed;
}

.document-table__action-btn:disabled:hover {
	transform: none;
	background: transparent;
}

.document-table__spinner {
	display: inline-block;
	width: 18px;
	height: 18px;
	border: 2px solid var(--accent-light);
	border-top-color: var(--accent);
	border-right-color: var(--accent);
	border-radius: 50%;
	animation: spin 0.8s linear infinite;
}

@keyframes spin {
	to {
		transform: rotate(360deg);
	}
}

@keyframes generatingPulse {
	0%, 100% {
		box-shadow: 0 0 0 0 rgba(var(--accent-rgb, 16, 185, 129), 0.4);
	}
	50% {
		box-shadow: 0 0 0 4px rgba(var(--accent-rgb, 16, 185, 129), 0);
	}
}

@keyframes shimmer {
	0% {
		left: -100%;
	}
	100% {
		left: 100%;
	}
}

.document-table__empty-row {
	pointer-events: none;
}

.document-table__empty-message {
	text-align: center;
	padding: var(--spacing-xl);
	color: var(--text-tertiary);
	font-size: 14px;
}
</style>
