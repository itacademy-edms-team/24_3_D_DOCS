<template>
	<div class="main-layout">
		<!-- Sidebar -->
		<aside class="sidebar">
			<div class="sidebar-content">
				<!-- New Project Button -->
				<button class="new-project-btn" @click="handleNewProject">
					<span class="plus-icon">+</span>
					Новый проект
				</button>

				<!-- Navigation -->
				<nav class="nav-section">
					<button
						class="nav-item"
						:class="{ active: activeTab === 'docs' }"
						@click="activeTab = 'docs'"
					>
						<span class="nav-icon">📄</span>
						Все документы
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'profiles' }"
						@click="activeTab = 'profiles'"
					>
						<span class="nav-icon">🎨</span>
						Шаблоны
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'shared' }"
						@click="activeTab = 'shared'"
					>
						<span class="nav-icon">👥</span>
						Общие
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'archived' }"
						@click="activeTab = 'archived'"
					>
						<span class="nav-icon">📦</span>
						Архив
					</button>
				</nav>

				<!-- User Section -->
				<div class="user-section">
					<div class="user-info">
						<div class="user-avatar">
							{{ userInitials }}
						</div>
						<div class="user-details">
							<span class="user-email">{{ user?.email }}</span>
						</div>
					</div>
					<div class="user-actions">
						<button class="user-action-btn" @click="handleSettings">
							Настройки
						</button>
						<button class="user-action-btn logout" @click="handleLogout">
							Выход
						</button>
					</div>
				</div>
			</div>
		</aside>

		<!-- Main Content -->
		<main class="main-content">
			<!-- Header -->
			<header class="content-header">
				<h1 class="page-title">
					{{ activeTab === 'docs' ? 'Все документы' : activeTab === 'profiles' ? 'Шаблоны' : activeTab === 'shared' ? 'Общие' : 'Архив' }}
				</h1>
				<div class="header-actions">
					<button class="upgrade-btn" v-if="false">
						<span class="info-icon">ℹ️</span>
						Вы на бесплатном плане
						<span class="upgrade-text">Обновить</span>
					</button>
				</div>
			</header>

			<!-- Search Bar -->
			<div class="search-section">
				<div class="search-wrapper">
					<span class="search-icon">🔍</span>
					<input
						v-model="searchQueryInput"
						type="text"
						class="search-input"
						:placeholder="`Поиск в ${activeTab === 'docs' ? 'документах' : 'шаблонах'}...`"
					/>
				</div>
			</div>

			<!-- Table -->
			<div class="table-container">
				<table class="projects-table">
					<thead>
						<tr>
							<th class="checkbox-col">
								<input
									type="checkbox"
									:checked="allSelected"
									@change="toggleSelectAll"
									class="checkbox"
								/>
							</th>
							<th class="title-col">
								Название
								<button
									class="sort-btn"
									@click="toggleSort('name')"
									v-if="activeTab === 'docs' || activeTab === 'profiles'"
								>
									{{ sortBy === 'name' && sortOrder === 'asc' ? '↑' : '↓' }}
								</button>
							</th>
							<th class="owner-col">Владелец</th>
							<th class="modified-col">
								Последнее изменение
								<button
									class="sort-btn"
									@click="toggleSort('updatedAt')"
								>
									{{ sortBy === 'updatedAt' && sortOrder === 'asc' ? '↑' : '↓' }}
								</button>
							</th>
							<th class="actions-col">Действия</th>
						</tr>
					</thead>
					<tbody>
						<tr
							v-for="item in filteredItems"
							:key="item.id"
							class="table-row"
							@click="handleItemClick(item)"
						>
							<td class="checkbox-col" @click.stop>
								<input
									type="checkbox"
									:checked="selectedItems.has(item.id)"
									@change="toggleSelect(item.id)"
									class="checkbox"
								/>
							</td>
							<td class="title-col">
								<span class="item-name">{{ item.name || 'Без названия' }}</span>
							</td>
							<td class="owner-col">Вы</td>
							<td class="modified-col">
								{{ formatDate(item.updatedAt) }}
							</td>
							<td class="actions-col" @click.stop>
								<div class="action-buttons">
									<button
										class="action-btn"
										@click.stop="handleCopy(item)"
										title="Копировать"
									>
										📋
									</button>
									<button
										class="action-btn"
										@click.stop="handleDownload(item)"
										title="Скачать"
									>
										⬇️
									</button>
									<button
										class="action-btn"
										@click.stop="handleExport(item)"
										title="Экспорт PDF"
									>
										📄
									</button>
									<button
										class="action-btn delete"
										@click.stop="handleDelete(item)"
										title="Удалить"
									>
										🗑️
									</button>
								</div>
							</td>
						</tr>
						<tr v-if="filteredItems.length === 0" class="empty-row">
							<td colspan="5" class="empty-message">
								{{ isLoading ? 'Загрузка...' : 'Нет элементов для отображения' }}
							</td>
						</tr>
					</tbody>
				</table>
			</div>

			<!-- Footer -->
			<div class="table-footer">
				<span class="items-count">
					Показано {{ filteredItems.length }} из {{ totalItems }}
				</span>
			</div>
		</main>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useDebounce } from '@vueuse/core';
import { useAuthStore } from '@/entities/auth/store/authStore';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import type { Profile } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';

const router = useRouter();
const authStore = useAuthStore();

const activeTab = ref<'docs' | 'profiles' | 'shared' | 'archived'>('docs');
const searchQueryInput = ref('');
const debouncedSearchQuery = useDebounce(searchQueryInput, 300);
const sortBy = ref<'name' | 'updatedAt'>('updatedAt');
const sortOrder = ref<'asc' | 'desc'>('desc');
const selectedItems = ref<Set<string>>(new Set());
const isLoading = ref(false);

const profiles = ref<Profile[]>([]);
const documents = ref<DocumentMeta[]>([]);

const user = computed(() => authStore.user);

const userInitials = computed(() => {
	if (!user.value?.email) return 'U';
	const parts = user.value.email.split('@')[0].split('.');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[1][0]).toUpperCase();
	}
	return user.value.email[0].toUpperCase();
});

const currentItems = computed(() => {
	if (activeTab.value === 'profiles') {
		return profiles.value;
	}
	return documents.value;
});

const filteredItems = computed(() => {
	let items = [...currentItems.value];

	// Search filter with debounced query
	if (debouncedSearchQuery.value.trim()) {
		const query = debouncedSearchQuery.value.toLowerCase();
		items = items.filter((item) =>
			(item.name || '').toLowerCase().includes(query),
		);
	}

	// Sort
	items.sort((a, b) => {
		let aValue: any;
		let bValue: any;

		if (sortBy.value === 'name') {
			aValue = (a.name || '').toLowerCase();
			bValue = (b.name || '').toLowerCase();
		} else {
			aValue = new Date(a.updatedAt).getTime();
			bValue = new Date(b.updatedAt).getTime();
		}

		if (sortOrder.value === 'asc') {
			return aValue > bValue ? 1 : -1;
		}
		return aValue < bValue ? 1 : -1;
	});

	return items;
});

const totalItems = computed(() => currentItems.value.length);

const allSelected = computed(() => {
	return (
		filteredItems.value.length > 0 &&
		filteredItems.value.every((item) => selectedItems.value.has(item.id))
	);
});

async function loadData() {
	isLoading.value = true;
	try {
		const [profilesData, documentsData] = await Promise.all([
			ProfileAPI.getAll().catch(() => []),
			DocumentAPI.getAll().catch(() => []),
		]);
		profiles.value = profilesData;
		documents.value = documentsData;
	} catch (error) {
		console.error('Failed to load data:', error);
	} finally {
		isLoading.value = false;
	}
}

function toggleSort(field: 'name' | 'updatedAt') {
	if (sortBy.value === field) {
		sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc';
	} else {
		sortBy.value = field;
		sortOrder.value = 'desc';
	}
}

function toggleSelect(id: string) {
	if (selectedItems.value.has(id)) {
		selectedItems.value.delete(id);
	} else {
		selectedItems.value.add(id);
	}
}

function toggleSelectAll() {
	if (allSelected.value) {
		selectedItems.value.clear();
	} else {
		filteredItems.value.forEach((item) => {
			selectedItems.value.add(item.id);
		});
	}
}

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffMins = Math.floor(diffMs / 60000);
	const diffHours = Math.floor(diffMs / 3600000);
	const diffDays = Math.floor(diffMs / 86400000);
	const diffMonths = Math.floor(diffDays / 30);

	if (diffMins < 1) return 'только что';
	if (diffMins < 60) return `${diffMins} мин. назад`;
	if (diffHours < 24) return `${diffHours} ч. назад`;
	if (diffDays < 30) return `${diffDays} дн. назад`;
	if (diffMonths < 12) return `${diffMonths} мес. назад`;
	return date.toLocaleDateString('ru-RU');
}

function handleItemClick(item: Profile | DocumentMeta) {
	if (activeTab.value === 'profiles') {
		router.push(`/profile/${item.id}`);
	} else {
		router.push(`/document/${item.id}`);
	}
}

function handleNewProject() {
	if (activeTab.value === 'profiles') {
		// Create new profile
		ProfileAPI.create({ name: 'Новый шаблон' })
			.then((profile) => {
				router.push(`/profile/${profile.id}`);
			})
			.catch(console.error);
	} else {
		// Create new document
		DocumentAPI.create({ name: 'Новый документ' })
			.then((doc) => {
				router.push(`/document/${doc.id}`);
			})
			.catch(console.error);
	}
}

function handleCopy(item: Profile | DocumentMeta) {
	console.log('Copy:', item);
	// TODO: Implement copy functionality
}

function handleDownload(item: Profile | DocumentMeta) {
	console.log('Download:', item);
	// TODO: Implement download functionality
}

function handleExport(item: Profile | DocumentMeta) {
	console.log('Export:', item);
	// TODO: Implement export functionality
}

async function handleDelete(item: Profile | DocumentMeta) {
	if (!confirm('Вы уверены, что хотите удалить этот элемент?')) {
		return;
	}

	try {
		if (activeTab.value === 'profiles') {
			await ProfileAPI.delete(item.id);
		} else {
			await DocumentAPI.delete(item.id);
		}
		await loadData();
		selectedItems.value.delete(item.id);
	} catch (error) {
		console.error('Failed to delete:', error);
		alert('Ошибка при удалении');
	}
}

function handleSettings() {
	console.log('Settings');
	// TODO: Navigate to settings page
}

async function handleLogout() {
	await authStore.logout();
	router.push('/auth');
}

onMounted(() => {
	loadData();
});
</script>

<style scoped>
.main-layout {
	display: flex;
	height: 100vh;
	background: #0a0a0a;
	color: #e4e4e7;
	overflow: hidden;
}

/* Sidebar */
.sidebar {
	width: 280px;
	background: #18181b;
	border-right: 1px solid #27272a;
	display: flex;
	flex-direction: column;
	overflow-y: auto;
}

.sidebar-content {
	display: flex;
	flex-direction: column;
	height: 100%;
	padding: 1.5rem;
	gap: 2rem;
}

.new-project-btn {
	width: 100%;
	padding: 0.75rem 1rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 0.5rem;
	transition: background 0.2s;
}

.new-project-btn:hover {
	background: #818cf8;
}

.plus-icon {
	font-size: 20px;
	font-weight: 300;
}

.nav-section {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
	flex: 1;
}

.nav-item {
	width: 100%;
	padding: 0.75rem 1rem;
	background: transparent;
	border: none;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 14px;
	text-align: left;
	cursor: pointer;
	display: flex;
	align-items: center;
	gap: 0.75rem;
	transition: all 0.2s;
}

.nav-item:hover {
	background: #27272a;
	color: #e4e4e7;
}

.nav-item.active {
	background: rgba(99, 102, 241, 0.2);
	color: #6366f1;
}

.nav-icon {
	font-size: 18px;
}

.user-section {
	margin-top: auto;
	padding-top: 1.5rem;
	border-top: 1px solid #27272a;
}

.user-info {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	margin-bottom: 1rem;
}

.user-avatar {
	width: 36px;
	height: 36px;
	border-radius: 50%;
	background: #6366f1;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	font-weight: 600;
	color: white;
	flex-shrink: 0;
}

.user-details {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0;
}

.user-email {
	font-size: 13px;
	color: #a1a1aa;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.user-actions {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.user-action-btn {
	padding: 0.5rem 1rem;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 13px;
	cursor: pointer;
	text-align: left;
	transition: all 0.2s;
}

.user-action-btn:hover {
	background: #27272a;
	color: #e4e4e7;
}

.user-action-btn.logout {
	color: #f87171;
	border-color: #27272a;
}

.user-action-btn.logout:hover {
	background: rgba(248, 113, 113, 0.1);
	border-color: #ef4444;
}

/* Main Content */
.main-content {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	background: #0a0a0a;
}

.content-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 2rem 2rem 1rem;
	border-bottom: 1px solid #27272a;
}

.page-title {
	font-size: 28px;
	font-weight: 700;
	color: #e4e4e7;
	margin: 0;
}

.header-actions {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.upgrade-btn {
	padding: 0.5rem 1rem;
	background: rgba(99, 102, 241, 0.1);
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 13px;
	cursor: pointer;
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.info-icon {
	font-size: 14px;
}

.upgrade-text {
	color: #6366f1;
	font-weight: 600;
}

/* Search */
.search-section {
	padding: 1rem 2rem;
	border-bottom: 1px solid #27272a;
}

.search-wrapper {
	position: relative;
	max-width: 500px;
}

.search-icon {
	position: absolute;
	left: 1rem;
	top: 50%;
	transform: translateY(-50%);
	font-size: 16px;
	color: #71717a;
}

.search-input {
	width: 100%;
	padding: 0.75rem 1rem 0.75rem 3rem;
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	color: #e4e4e7;
	font-size: 14px;
	outline: none;
	transition: border-color 0.2s;
}

.search-input:focus {
	border-color: #6366f1;
}

.search-input::placeholder {
	color: #71717a;
}

/* Table */
.table-container {
	flex: 1;
	overflow-y: auto;
	padding: 0 2rem;
}

.projects-table {
	width: 100%;
	border-collapse: collapse;
	margin-top: 1rem;
}

.projects-table thead {
	position: sticky;
	top: 0;
	background: #0a0a0a;
	z-index: 10;
}

.projects-table th {
	padding: 1rem;
	text-align: left;
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
}

.table-row {
	cursor: pointer;
	transition: background 0.15s;
}

.table-row:hover {
	background: rgba(99, 102, 241, 0.05);
}

.checkbox-col {
	width: 40px;
}

.checkbox {
	width: 18px;
	height: 18px;
	cursor: pointer;
	accent-color: #6366f1;
}

.title-col {
	min-width: 200px;
}

.item-name {
	font-size: 14px;
	color: #e4e4e7;
	font-weight: 500;
}

.owner-col {
	width: 120px;
	font-size: 14px;
	color: #a1a1aa;
}

.modified-col {
	width: 180px;
	font-size: 13px;
	color: #a1a1aa;
}

.actions-col {
	width: 160px;
}

.sort-btn {
	background: transparent;
	border: none;
	color: #71717a;
	cursor: pointer;
	font-size: 12px;
	margin-left: 0.5rem;
	padding: 0.25rem;
	transition: color 0.2s;
}

.sort-btn:hover {
	color: #6366f1;
}

.action-buttons {
	display: flex;
	gap: 0.5rem;
}

.action-btn {
	width: 32px;
	height: 32px;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
	transition: all 0.2s;
}

.action-btn:hover {
	background: #27272a;
	border-color: #3f3f46;
}

.action-btn.delete:hover {
	background: rgba(248, 113, 113, 0.1);
	border-color: #ef4444;
}

.empty-row {
	pointer-events: none;
}

.empty-message {
	text-align: center;
	padding: 3rem;
	color: #71717a;
	font-size: 14px;
}

.table-footer {
	padding: 1rem 2rem;
	border-top: 1px solid #27272a;
	background: #0a0a0a;
}

.items-count {
	font-size: 13px;
	color: #71717a;
}
</style>
