<template>
	<div class="document-table">
		<table class="document-table__table">
			<thead>
				<tr>
					<th class="document-table__col-checkbox">
						<input
							type="checkbox"
							:checked="allSelected"
							@change="toggleSelectAll"
							class="document-table__checkbox"
						/>
					</th>
					<th class="document-table__col-title">
						Название
						<button
							class="document-table__sort-btn"
							@click="toggleSort('name')"
						>
							<Icon :name="sortBy === 'name' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
						</button>
					</th>
					<th class="document-table__col-profile">Профиль стиля</th>
					<th class="document-table__col-modified">
						Изменён
						<button
							class="document-table__sort-btn"
							@click="toggleSort('updatedAt')"
						>
							<Icon :name="sortBy === 'updatedAt' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
						</button>
					</th>
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
					<td class="document-table__col-checkbox" @click.stop>
						<input
							type="checkbox"
							:checked="selectedIds.has(document.id)"
							@change="toggleSelect(document.id)"
							class="document-table__checkbox"
						/>
					</td>
					<td class="document-table__col-title">
						<span class="document-table__name">{{ document.name }}</span>
					</td>
					<td class="document-table__col-profile">
						<span
							v-if="document.profileName"
							class="document-table__badge document-table__badge--profile"
						>
							{{ document.profileName }}
						</span>
						<span v-else class="document-table__empty">—</span>
					</td>
					<td class="document-table__col-modified">
						{{ formatDate(document.updatedAt) }}
					</td>
					<td class="document-table__col-actions" @click.stop>
						<div class="document-table__actions">
							<button
								class="document-table__action-btn"
								@click.stop="handleAction(document, 'open')"
								title="Открыть"
							>
								<Icon name="folder_open" size="18" />
							</button>
							<button
								class="document-table__action-btn"
								:class="{ 'document-table__action-btn--generating': isGeneratingPdf(document.id) }"
								@click.stop="handleAction(document, 'export-pdf')"
								:disabled="isGeneratingPdf(document.id)"
								title="Экспорт PDF"
							>
								<span v-if="isGeneratingPdf(document.id)" class="document-table__spinner"></span>
								<Icon v-else name="description" size="18" />
							</button>
							<button
								class="document-table__action-btn"
								:class="{ 'document-table__action-btn--generating': isGeneratingDdoc(document.id) }"
								@click.stop="handleAction(document, 'export-ddoc')"
								:disabled="isGeneratingDdoc(document.id)"
								title="Экспорт .ddoc"
							>
								<span v-if="isGeneratingDdoc(document.id)" class="document-table__spinner"></span>
								<Icon v-else name="archive" size="18" />
							</button>
							<button
								class="document-table__action-btn document-table__action-btn--delete"
								@click.stop="handleAction(document, 'delete')"
								title="Удалить"
							>
								<Icon name="trash" size="18" />
							</button>
						</div>
					</td>
				</tr>
				<tr v-if="sortedDocuments.length === 0" class="document-table__empty-row">
					<td colspan="5" class="document-table__empty-message">
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
	selectedIds: Set<string>;
	isLoading?: boolean;
	sortBy?: 'name' | 'updatedAt';
	sortOrder?: 'asc' | 'desc';
	generatingStates?: Map<string, Set<'pdf' | 'ddoc'>>;
}

const props = withDefaults(defineProps<Props>(), {
	isLoading: false,
	sortBy: 'updatedAt',
	sortOrder: 'desc',
	generatingStates: () => new Map(),
});

const isGeneratingPdf = (documentId: string): boolean => {
	return props.generatingStates?.get(documentId)?.has('pdf') ?? false;
};

const isGeneratingDdoc = (documentId: string): boolean => {
	return props.generatingStates?.get(documentId)?.has('ddoc') ?? false;
};

const emit = defineEmits<{
	'update:selectedIds': [ids: Set<string>];
	'update:sortBy': [field: 'name' | 'updatedAt'];
	'update:sortOrder': [order: 'asc' | 'desc'];
	'row-click': [document: DocumentMeta];
	action: [document: DocumentMeta, action: string];
}>();

const allSelected = computed(() => {
	return (
		props.documents.length > 0 &&
		props.documents.every((doc) => props.selectedIds.has(doc.id))
	);
});

const sortedDocuments = computed(() => {
	const docs = [...props.documents];
	docs.sort((a, b) => {
		let aValue: any;
		let bValue: any;

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

const toggleSelectAll = () => {
	const newSelected = new Set<string>();
	if (!allSelected.value) {
		sortedDocuments.value.forEach((doc) => {
			newSelected.add(doc.id);
		});
	}
	emit('update:selectedIds', newSelected);
};

const toggleSelect = (id: string) => {
	const newSelected = new Set(props.selectedIds);
	if (newSelected.has(id)) {
		newSelected.delete(id);
	} else {
		newSelected.add(id);
	}
	emit('update:selectedIds', newSelected);
};

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

const formatDate = (dateString: string): string => {
	const date = new Date(dateString);
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffMins = Math.floor(diffMs / 60000);
	const diffHours = Math.floor(diffMs / 3600000);
	const diffDays = Math.floor(diffMs / 86400000);

	if (diffMins < 1) return 'только что';
	if (diffMins < 60) return `${diffMins} мин. назад`;
	if (diffHours < 24) return `${diffHours} ч. назад`;
	if (diffDays < 30) return `${diffDays} дн. назад`;
	return date.toLocaleDateString('ru-RU');
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

.document-table__col-checkbox {
	width: 40px;
}

.document-table__checkbox {
	width: 18px;
	height: 18px;
	cursor: pointer;
	accent-color: var(--accent);
}

.document-table__col-title {
	min-width: 200px;
}

.document-table__name {
	font-size: 14px;
	color: var(--text-primary);
	font-weight: 500;
}

.document-table__col-profile {
	width: 150px;
}

.document-table__badge {
	display: inline-block;
	padding: 4px 8px;
	border-radius: var(--radius-sm);
	font-size: 12px;
	font-weight: 500;
}

.document-table__badge--profile {
	background: var(--accent-light);
	color: var(--accent);
}

.document-table__empty {
	color: var(--text-tertiary);
	font-size: 13px;
}

.document-table__col-modified {
	width: 180px;
	font-size: 13px;
	color: var(--text-secondary);
}

.document-table__col-actions {
	width: 160px;
}

.document-table__sort-btn {
	background: transparent;
	border: none;
	color: var(--text-tertiary);
	cursor: pointer;
	font-size: 12px;
	margin-left: var(--spacing-xs);
	padding: 2px;
	transition: color 0.2s ease;
}

.document-table__sort-btn:hover {
	color: var(--accent);
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
	color: var(--text-secondary);
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

.document-table__action-btn--delete:hover {
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
