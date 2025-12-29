<template>
	<div class="main-layout">
		<!-- Sidebar -->
		<aside class="sidebar">
			<div class="sidebar-content">
				<!-- New Project Button -->
				<button class="new-project-btn" @click="handleNewProject">
					<Icon name="plus" size="20" />
					Новый проект
				</button>

				<!-- Navigation -->
				<nav class="nav-section">
					<button
						class="nav-item nav-item--profiles"
						:class="{ active: activeTab === 'profiles' }"
						@click="activeTab = 'profiles'"
					>
						<Icon name="palette" size="18" class="nav-icon" />
						Профили стилей
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'docs' }"
						@click="activeTab = 'docs'"
					>
						<Icon name="description" size="18" class="nav-icon" />
						Все документы
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'shared' }"
						@click="activeTab = 'shared'"
					>
						<Icon name="assignment" size="18" class="nav-icon" />
						Титульные листы
					</button>
					<button
						class="nav-item"
						:class="{ active: activeTab === 'archived' }"
						@click="activeTab = 'archived'"
					>
						<Icon name="archive" size="18" class="nav-icon" />
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
							<Icon name="settings" size="16" />
							<span>Настройки</span>
						</button>
						<button class="user-action-btn logout" @click="handleLogout">
							<Icon name="logout" size="16" />
							<span>Выход</span>
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
					{{ activeTab === 'docs' ? 'Все документы' : activeTab === 'profiles' ? 'Профили стилей' : activeTab === 'shared' ? 'Титульные листы' : 'Архив' }}
				</h1>
				<div class="header-actions">
					<Button
						v-if="activeTab === 'profiles' || activeTab === 'shared'"
						@click="handleCreateProfileOrTitlePage"
						:isLoading="isCreating"
					>
						{{ activeTab === 'profiles' ? '+ Создать профиль' : '+ Создать титульник' }}
					</Button>
					<ThemeToggle />
					<button class="upgrade-btn" v-if="false">
						<Icon name="info" size="14" class="info-icon" />
						Вы на бесплатном плане
						<span class="upgrade-text">Обновить</span>
					</button>
				</div>
			</header>

			<!-- Info Banner -->
			<div class="content-body">
				<InfoBanner v-if="activeTab === 'docs'" />

				<!-- Search Bar -->
				<div class="search-section">
					<div class="search-wrapper">
						<Icon name="search" size="18" class="search-icon" />
						<input
							v-model="searchQueryInput"
							type="text"
							class="search-input"
							:placeholder="`Поиск в ${activeTab === 'docs' ? 'документах' : activeTab === 'profiles' ? 'профилях стилей' : activeTab === 'shared' ? 'титульных листах' : 'архиве'}...`"
						/>
					</div>
				</div>

				<!-- Table -->
				<DocumentTable
					v-if="activeTab === 'docs'"
					:documents="documents"
					:selectedIds="selectedItems"
					:isLoading="isLoading"
					:sortBy="sortBy"
					:sortOrder="sortOrder"
					@update:selectedIds="selectedItems = $event"
					@update:sortBy="sortBy = $event"
					@update:sortOrder="sortOrder = $event"
					@row-click="handleItemClick"
					@action="handleDocumentAction"
				/>

				<!-- Profiles Table -->
				<div v-if="activeTab === 'profiles'" class="items-table">
					<table class="items-table__table">
						<thead>
							<tr>
								<th class="items-table__col-checkbox">
									<input
										type="checkbox"
										:checked="allSelected"
										@change="toggleSelectAll"
										class="items-table__checkbox"
									/>
								</th>
								<th class="items-table__col-title">
									Название
									<button
										class="items-table__sort-btn"
										@click="toggleSort('name')"
									>
										<Icon :name="sortBy === 'name' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
									</button>
								</th>
								<th class="items-table__col-modified">
									Изменён
									<button
										class="items-table__sort-btn"
										@click="toggleSort('updatedAt')"
									>
										<Icon :name="sortBy === 'updatedAt' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
									</button>
								</th>
								<th class="items-table__col-actions">Действия</th>
							</tr>
						</thead>
						<tbody>
							<tr
								v-for="item in filteredItems"
								:key="item.id"
								class="items-table__row"
								@click="handleItemClick(item)"
							>
								<td class="items-table__col-checkbox" @click.stop>
									<input
										type="checkbox"
										:checked="selectedItems.has(item.id)"
										@change="toggleSelect(item.id)"
										class="items-table__checkbox"
									/>
								</td>
								<td class="items-table__col-title">
									<span class="items-table__name">{{ item.name }}</span>
								</td>
								<td class="items-table__col-modified">
									{{ formatDate(item.updatedAt) }}
								</td>
								<td class="items-table__col-actions" @click.stop>
									<div class="items-table__actions">
										<button
											class="items-table__action-btn"
											@click.stop="handleItemClick(item)"
											title="Открыть"
										>
											<Icon name="folder_open" size="18" />
										</button>
										<button
											class="items-table__action-btn items-table__action-btn--delete"
											@click.stop="handleDelete(item)"
											title="Удалить"
										>
											<Icon name="trash" size="18" />
										</button>
									</div>
								</td>
							</tr>
							<tr v-if="filteredItems.length === 0" class="items-table__empty-row">
								<td colspan="4" class="items-table__empty-message">
									{{ isLoading ? 'Загрузка...' : 'Нет профилей' }}
								</td>
							</tr>
						</tbody>
					</table>
				</div>

				<!-- Title Pages Table -->
				<div v-if="activeTab === 'shared'" class="items-table">
					<table class="items-table__table">
						<thead>
							<tr>
								<th class="items-table__col-checkbox">
									<input
										type="checkbox"
										:checked="allSelected"
										@change="toggleSelectAll"
										class="items-table__checkbox"
									/>
								</th>
								<th class="items-table__col-title">
									Название
									<button
										class="items-table__sort-btn"
										@click="toggleSort('name')"
									>
										<Icon :name="sortBy === 'name' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
									</button>
								</th>
								<th class="items-table__col-modified">
									Изменён
									<button
										class="items-table__sort-btn"
										@click="toggleSort('updatedAt')"
									>
										<Icon :name="sortBy === 'updatedAt' && sortOrder === 'asc' ? 'arrow_upward' : 'arrow_downward'" size="12" />
									</button>
								</th>
								<th class="items-table__col-actions">Действия</th>
							</tr>
						</thead>
						<tbody>
							<tr
								v-for="item in filteredTitlePages"
								:key="item.id"
								class="items-table__row"
								@click="handleTitlePageClick(item)"
							>
								<td class="items-table__col-checkbox" @click.stop>
									<input
										type="checkbox"
										:checked="selectedItems.has(item.id)"
										@change="toggleSelect(item.id)"
										class="items-table__checkbox"
									/>
								</td>
								<td class="items-table__col-title">
									<span class="items-table__name">{{ item.name }}</span>
								</td>
								<td class="items-table__col-modified">
									{{ formatDate(item.updatedAt) }}
								</td>
								<td class="items-table__col-actions" @click.stop>
									<div class="items-table__actions">
										<button
											class="items-table__action-btn"
											@click.stop="handleTitlePageClick(item)"
											title="Открыть"
										>
											<Icon name="folder_open" size="18" />
										</button>
										<button
											class="items-table__action-btn items-table__action-btn--delete"
											@click.stop="handleTitlePageDelete(item)"
											title="Удалить"
										>
											<Icon name="trash" size="18" />
										</button>
									</div>
								</td>
							</tr>
							<tr v-if="filteredTitlePages.length === 0" class="items-table__empty-row">
								<td colspan="4" class="items-table__empty-message">
									{{ isLoading ? 'Загрузка...' : 'Нет титульников' }}
								</td>
							</tr>
						</tbody>
					</table>
				</div>

				<!-- Footer -->
				<div class="table-footer">
					<span class="items-count">
						Показано {{ activeTab === 'shared' ? filteredTitlePages.length : filteredItems.length }} из {{ activeTab === 'shared' ? titlePages.length : totalItems }}
					</span>
				</div>
			</div>
		</main>

		<!-- Create Document Modal -->
		<CreateDocumentModal
			v-model="showCreateModal"
			@created="handleDocumentCreated"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useDebounce } from '@vueuse/core';
import { useAuthStore } from '@/entities/auth/store/authStore';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import DocumentTable from '@/widgets/document-table/DocumentTable.vue';
import CreateDocumentModal from '@/widgets/create-document/CreateDocumentModal.vue';
import InfoBanner from '@/widgets/info-banner/InfoBanner.vue';
import ThemeToggle from '@/features/theme-toggle/ThemeToggle.vue';
import Button from '@/shared/ui/Button/Button.vue';
import Icon from '@/components/Icon.vue';
import { getDefaultProfileData } from '@/utils/profileDefaults';
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
const titlePages = ref<any[]>([]);
const showCreateModal = ref(false);
const isCreating = ref(false);

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

const filteredTitlePages = computed(() => {
	let items = [...titlePages.value];

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
	const items = activeTab.value === 'shared' ? filteredTitlePages.value : filteredItems.value;
	return (
		items.length > 0 &&
		items.every((item) => selectedItems.value.has(item.id))
	);
});

async function loadData() {
	isLoading.value = true;
	try {
		const status = activeTab.value === 'archived' ? 'archived' : undefined;
		const [profilesData, documentsData, titlePagesData] = await Promise.all([
			ProfileAPI.getAll().catch((err) => {
				console.error('Failed to load profiles:', err);
				return [];
			}),
			DocumentAPI.getAll(status, debouncedSearchQuery.value || undefined).catch((err) => {
				console.error('Failed to load documents:', err);
				return [];
			}),
			TitlePageAPI.getAll().catch((err) => {
				console.error('Failed to load title pages:', err);
				return [];
			}),
		]);
		profiles.value = profilesData || [];
		documents.value = documentsData || [];
		titlePages.value = titlePagesData || [];
	} catch (error) {
		console.error('Failed to load data:', error);
		// Убеждаемся, что массивы инициализированы даже при ошибке
		profiles.value = profiles.value || [];
		documents.value = documents.value || [];
		titlePages.value = titlePages.value || [];
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
	const items = activeTab.value === 'shared' ? filteredTitlePages.value : filteredItems.value;
	if (allSelected.value) {
		selectedItems.value.clear();
	} else {
		items.forEach((item) => {
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

function handleTitlePageClick(item: any) {
	router.push(`/title-page/${item.id}`);
}

async function handleCreateProfileOrTitlePage() {
	isCreating.value = true;
	try {
		if (activeTab.value === 'profiles') {
			const profile = await ProfileAPI.create({
				name: 'Новый профиль стилей',
				data: getDefaultProfileData(),
			});
			router.push(`/profile/${profile.id}`);
		} else if (activeTab.value === 'shared') {
			const titlePage = await TitlePageAPI.create({ name: 'Новый титульник' });
			router.push(`/title-page/${titlePage.id}`);
		}
		await loadData();
	} catch (error: any) {
		console.error('Failed to create:', error);
		const errorMessage = error?.message || error?.response?.data?.message || 'Ошибка при создании';
		alert(`Ошибка при создании: ${errorMessage}`);
	} finally {
		isCreating.value = false;
	}
}

async function handleTitlePageDelete(item: any) {
	if (!confirm('Вы уверены, что хотите удалить этот титульник?')) {
		return;
	}

	try {
		await TitlePageAPI.delete(item.id);
		await loadData();
		selectedItems.value.delete(item.id);
	} catch (error) {
		console.error('Failed to delete:', error);
		alert('Ошибка при удалении');
	}
}

function handleNewProject() {
	if (activeTab.value === 'profiles') {
		handleCreateProfileOrTitlePage();
	} else if (activeTab.value === 'shared') {
		handleCreateProfileOrTitlePage();
	} else {
		showCreateModal.value = true;
	}
}

function handleDocumentCreated(documentId: string) {
	router.push(`/document/${documentId}`);
	loadData();
}

function handleDocumentAction(document: DocumentMeta, action: string) {
	if (action === 'open') {
		router.push(`/document/${document.id}`);
	} else if (action === 'export-pdf') {
		handleExportPdf(document);
	} else if (action === 'export-ddoc') {
		handleExportDdoc(document);
	} else if (action === 'delete') {
		handleDelete(document);
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

async function handleExportPdf(document: DocumentMeta) {
	try {
		const blob = await DocumentAPI.generatePdf(document.id);
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `${document.name}.pdf`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		URL.revokeObjectURL(url);
	} catch (error) {
		console.error('Failed to export PDF:', error);
		alert('Ошибка при экспорте PDF');
	}
}

async function handleExportDdoc(document: DocumentMeta) {
	try {
		const blob = await DocumentAPI.exportDocument(document.id);
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `${document.name}.ddoc`;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		URL.revokeObjectURL(url);
	} catch (error) {
		console.error('Failed to export .ddoc:', error);
		alert('Ошибка при экспорте документа');
	}
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

watch([activeTab, debouncedSearchQuery], () => {
	if (authStore.isAuth) {
		loadData();
	}
});

onMounted(async () => {
	// Проверяем авторизацию перед загрузкой данных
	if (authStore.isAuth) {
		try {
			await authStore.checkAuth();
			// Если авторизация успешна, загружаем данные
			if (authStore.isAuth) {
				await loadData();
			}
		} catch (error) {
			console.error('Auth check failed:', error);
			// Если авторизация не удалась, роутер должен редиректить на /auth
			// Но на всякий случай проверяем еще раз
			if (!authStore.isAuth) {
				console.log('User not authenticated, redirecting to /auth');
				router.push('/auth');
			}
		}
	} else {
		// Если пользователь не авторизован, редиректим на страницу входа
		console.log('User not authenticated, redirecting to /auth');
		router.push('/auth');
	}
});
</script>

<style scoped>
.main-layout {
	display: flex;
	height: 100vh;
	background: var(--bg-primary);
	color: var(--text-primary);
	overflow: hidden;
	animation: layoutFadeIn 0.6s ease-out;
}

@keyframes layoutFadeIn {
	from { opacity: 0; }
	to { opacity: 1; }
}

/* Sidebar */
.sidebar {
	width: 280px;
	background: var(--bg-secondary);
	border-right: 1px solid var(--border-color);
	display: flex;
	flex-direction: column;
	overflow-y: auto;
	animation: sidebarSlide 0.5s ease-out;
}

@keyframes sidebarSlide {
	from { transform: translateX(-20px); opacity: 0; }
	to { transform: translateX(0); opacity: 1; }
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
	background: var(--accent);
	color: white;
	border: none;
	border-radius: var(--radius-md);
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 0.75rem;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: 0 4px 12px rgba(var(--accent-rgb, 16, 185, 129), 0.2);
}

.new-project-btn:hover {
	background: var(--accent-hover);
	transform: translateY(-2px);
	box-shadow: 0 6px 16px rgba(var(--accent-rgb, 16, 185, 129), 0.3);
}

.new-project-btn:active {
	transform: translateY(0);
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
	border-radius: var(--radius-sm);
	color: var(--text-secondary);
	font-size: 14px;
	text-align: left;
	cursor: pointer;
	display: flex;
	align-items: center;
	gap: 0.75rem;
	transition: all 0.2s ease;
}

.nav-item:hover {
	background: var(--bg-tertiary);
	color: var(--text-primary);
	transform: translateX(4px);
}

.nav-item.active {
	background: var(--accent-light);
	color: var(--accent);
	font-weight: 600;
}

.nav-icon {
	color: inherit;
	transition: transform 0.2s ease;
}

.nav-item:hover .nav-icon {
	transform: scale(1.1);
}

/* User Section */
.user-section {
	margin-top: auto;
	padding-top: 1.5rem;
	border-top: 1px solid var(--border-color);
}

.user-info {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	margin-bottom: 1.25rem;
	padding: 0.5rem;
	background: var(--bg-primary);
	border-radius: var(--radius-lg);
	border: 1px solid var(--border-color);
}

.user-avatar {
	width: 40px;
	height: 40px;
	border-radius: 12px;
	background: linear-gradient(135deg, var(--accent) 0%, var(--accent-hover) 100%);
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
	font-weight: 700;
	color: white;
	flex-shrink: 0;
	box-shadow: 0 4px 10px rgba(var(--accent-rgb, 16, 185, 129), 0.2);
}

.user-details {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0;
}

.user-email {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.user-actions {
	display: flex;
	gap: 0.5rem;
}

.user-action-btn {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 0.5rem;
	padding: 0.6rem;
	background: var(--bg-primary);
	border: 1px solid var(--border-color);
	border-radius: 10px;
	color: var(--text-secondary);
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.user-action-btn:hover {
	background: var(--bg-secondary);
	color: var(--text-primary);
	border-color: var(--border-hover);
	transform: translateY(-1px);
}

.user-action-btn.logout:hover {
	background: rgba(239, 68, 68, 0.08);
	color: var(--danger);
	border-color: rgba(239, 68, 68, 0.2);
}

.user-action-btn:active {
	transform: translateY(0);
}

/* Main Content */
.main-content {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	background: var(--bg-primary);
	animation: mainContentFade 0.7s ease-out;
}

@keyframes mainContentFade {
	from { opacity: 0; transform: translateY(10px); }
	to { opacity: 1; transform: translateY(0); }
}

.content-body {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	padding: 0 var(--spacing-xl);
}

.content-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: var(--spacing-xl) var(--spacing-xl) var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
}

.page-title {
	font-size: 28px;
	font-weight: 700;
	color: var(--text-primary);
	margin: 0;
}

.header-actions {
	display: flex;
	align-items: center;
	gap: 1rem;
	flex-wrap: nowrap;
	white-space: nowrap;
}

.search-section {
	padding: var(--spacing-md) 0;
	border-bottom: 1px solid var(--border-color);
}

.search-wrapper {
	position: relative;
	max-width: 500px;
}

.search-icon {
	position: absolute;
	left: 12px;
	top: 50%;
	transform: translateY(-50%);
	color: var(--text-tertiary);
	pointer-events: none;
}

.search-input {
	width: 100%;
	padding: 0.75rem 1rem 0.75rem 2.5rem;
	background: var(--bg-secondary);
	border: 1.5px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	font-size: 14px;
	outline: none;
	transition: all 0.2s ease;
}

.search-input:focus {
	border-color: var(--accent);
	background: var(--bg-primary);
	box-shadow: 0 0 0 4px rgba(var(--accent-rgb, 16, 185, 129), 0.1);
}

/* Items Table */
.items-table__row {
	cursor: pointer;
	transition: all 0.2s ease;
}

.items-table__row:hover {
	background: var(--bg-secondary);
	transform: scale(1.002);
}

.items-table__action-btn {
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
}

.items-table__action-btn:hover {
	background: var(--bg-tertiary);
	color: var(--text-primary);
	border-color: var(--border-hover);
	transform: translateY(-1px);
}

.items-table__action-btn--delete:hover {
	background: rgba(239, 68, 68, 0.1);
	color: var(--danger);
	border-color: var(--danger);
}

.items-table {
	flex: 1;
	overflow-y: auto;
	margin-top: 1rem;
}

.items-table__table {
	width: 100%;
	border-collapse: collapse;
}

.items-table__table thead {
	position: sticky;
	top: 0;
	background: var(--bg-primary);
	z-index: 10;
}

.items-table__table th {
	padding: 1rem;
	text-align: left;
	font-size: 13px;
	font-weight: 600;
	color: var(--text-secondary);
	text-transform: uppercase;
	letter-spacing: 0.5px;
	border-bottom: 2px solid var(--border-color);
}

.items-table__table td {
	padding: 1rem;
	border-bottom: 1px solid var(--border-color);
	vertical-align: middle;
}

.items-table__col-checkbox {
	width: 48px;
}

.items-table__checkbox {
	width: 18px;
	height: 18px;
	cursor: pointer;
	accent-color: var(--accent);
}

.items-table__name {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.items-table__col-modified {
	width: 200px;
	font-size: 13px;
	color: var(--text-secondary);
}

.items-table__col-actions {
	width: 120px;
}

.items-table__sort-btn {
	background: transparent;
	border: none;
	color: var(--text-tertiary);
	cursor: pointer;
	font-size: 12px;
	margin-left: var(--spacing-xs);
	padding: 2px;
	transition: color 0.2s ease;
	display: inline-flex;
	align-items: center;
	justify-content: center;
}

.items-table__sort-btn:hover {
	color: var(--accent);
}

.items-table__actions {
	display: flex;
	gap: 0.5rem;
	justify-content: flex-end;
}

.items-table__empty-message {
	padding: 4rem;
	text-align: center;
	color: var(--text-tertiary);
	font-size: 15px;
}

.table-footer {
	padding: 1.5rem 0;
	border-top: 1px solid var(--border-color);
	color: var(--text-tertiary);
	font-size: 13px;
}
</style>
