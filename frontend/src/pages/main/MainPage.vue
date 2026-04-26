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
					{{ activeTab === 'docs' ? 'Все документы' : activeTab === 'profiles' ? 'Профили стилей' : 'Титульные листы' }}
				</h1>
				<div class="header-actions">
					<NotificationBell @accepted="loadData" />
					<button
						type="button"
						class="main-page__share-btn"
						title="Поделиться / экспорт"
						aria-label="Поделиться"
						@click="openShareExportModal"
					>
						<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor" aria-hidden="true">
							<path
								d="M18 16.08c-.76 0-1.44.3-1.96.77L8.91 12.7c.05-.23.09-.46.09-.7s-.04-.47-.09-.7l7.05-4.11c.54.5 1.25.81 2.04.81 1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3c0 .24.04.47.09.7L8.04 9.81C7.5 9.31 6.79 9 6 9c-1.66 0-3 1.34-3 3s1.34 3 3 3c.79 0 1.5-.31 2.04-.81l7.12 4.16c-.05.21-.08.43-.08.65 0 1.61 1.31 2.92 2.92 2.92s2.92-1.31 2.92-2.92-1.31-2.92-2.92-2.92z"
							/>
						</svg>
					</button>
					<Button
						v-if="activeTab === 'profiles' || activeTab === 'shared'"
						@click="handleCreateProfileOrTitlePage"
						:isLoading="isCreating"
					>
						{{ activeTab === 'profiles' ? '+ Создать профиль' : '+ Создать титульник' }}
					</Button>
					<button
						class="main-page__ai-btn"
						:class="{ 'main-page__ai-btn--active': showAIPanel }"
						@click="showAIPanel = !showAIPanel"
						title="DDOCS AI"
					>
						AI
					</button>
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
							:placeholder="`Поиск в ${activeTab === 'docs' ? 'документах' : activeTab === 'profiles' ? 'профилях стилей' : 'титульных листах'}...`"
						/>
					</div>
				</div>

				<div class="content-body__main">
					<!-- Table -->
					<DocumentTable
						v-if="activeTab === 'docs'"
						:documents="docsTableRows"
						:isLoading="isLoading"
						:sortBy="sortBy"
						:sortOrder="sortOrder"
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
								<th
									class="items-table__col-title items-table__col-title--sortable"
									@click="toggleSort('name')"
								>
									Название
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
								<td class="items-table__col-title">
									<span class="items-table__name">{{ item.name }}</span>
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
											class="items-table__action-btn"
											@click.stop="handleProfileExportDdoc(item)"
											title="Экспорт .ddoc"
										>
											<Icon name="archive" size="18" />
										</button>
										<button
											class="items-table__action-btn items-table__action-btn--delete"
											@click.stop="openDeleteProfileConfirm(item)"
											title="Удалить"
										>
											<Icon name="trash" size="18" />
										</button>
									</div>
								</td>
							</tr>
							<tr v-if="filteredItems.length === 0" class="items-table__empty-row">
								<td colspan="2" class="items-table__empty-message">
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
								<th
									class="items-table__col-title items-table__col-title--sortable"
									@click="toggleSort('name')"
								>
									Название
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
								<td class="items-table__col-title">
									<span class="items-table__name">{{ item.name }}</span>
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
											class="items-table__action-btn"
											@click.stop="handleTitlePageExportDdoc(item)"
											title="Экспорт .ddoc"
										>
											<Icon name="archive" size="18" />
										</button>
										<button
											class="items-table__action-btn items-table__action-btn--delete"
											@click.stop="openDeleteTitlePageConfirm(item)"
											title="Удалить"
										>
											<Icon name="trash" size="18" />
										</button>
									</div>
								</td>
							</tr>
							<tr v-if="filteredTitlePages.length === 0" class="items-table__empty-row">
								<td colspan="2" class="items-table__empty-message">
									{{ isLoading ? 'Загрузка...' : 'Нет титульников' }}
								</td>
							</tr>
						</tbody>
					</table>
					</div>
				</div>
			</div>
		</main>

		<!-- Create Document Modal -->
		<CreateDocumentModal
			v-model="showCreateModal"
			@created="handleDocumentCreated"
			@imported="handleFilesImported"
		/>

		<Modal v-model="showImportSuccessModal" title="Импорт завершен" size="sm">
			<p class="main-page__import-message">Файлы успешно импортированы</p>
			<template #footer>
				<Button :disabled="!importedDocumentId" @click="goToImportedDocument">К документу</Button>
				<Button variant="secondary" @click="closeImportSuccessModal">Ок</Button>
			</template>
		</Modal>

		<CreateTitlePageModal
			v-model="showCreateTitlePageModal"
			@created="handleTitlePageCreatedFromModal"
		/>

		<ExportPdfModal
			v-model="showExportPdfModal"
			:with-entity-picker="exportModalWithPicker"
			:document-id="exportModalDocumentId"
			:document-name="exportModalDocumentName"
		/>

		<!-- AI Panel (Chat Dock) -->
		<ChatDock
			v-model:open="showAIPanel"
			scope="global"
			@document-content-changed="handleAgentDocumentChanged"
			@width-changed="handleChatDockWidthChanged"
		/>

		<Transition name="main-toast">
			<div
				v-if="toast.visible"
				class="main-page__toast"
				:class="'main-page__toast--' + toast.variant"
				role="status"
			>
				{{ toast.text }}
			</div>
		</Transition>

		<Modal v-model="showDeleteConfirm" title="Удаление" size="sm">
			<p class="main-page__confirm-text">{{ deleteConfirmText }}</p>
			<template #footer>
				<div class="main-page__modal-footer">
					<button
						type="button"
						class="main-page__modal-btn main-page__modal-btn--secondary"
						@click="cancelDeleteConfirm"
					>
						Отмена
					</button>
					<button type="button" class="main-page__modal-btn main-page__modal-btn--danger" @click="confirmDelete">
						Удалить
					</button>
				</div>
			</template>
		</Modal>

		<Modal v-model="showLeaveCollabConfirm" title="Покинуть соавторство" size="sm">
			<p v-if="leaveCollabDoc" class="main-page__confirm-text">
				Покинуть соавторство документа «{{ leaveCollabDoc.name }}»? Доступ к документу будет отозван.
			</p>
			<template #footer>
				<div class="main-page__modal-footer">
					<button
						type="button"
						class="main-page__modal-btn main-page__modal-btn--secondary"
						@click="cancelLeaveCollabConfirm"
					>
						Отмена
					</button>
					<button type="button" class="main-page__modal-btn main-page__modal-btn--danger" @click="confirmLeaveCollab">
						Выйти
					</button>
				</div>
			</template>
		</Modal>
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
import ExportPdfModal from '@/widgets/export-pdf-modal/ExportPdfModal.vue';
import CreateDocumentModal from '@/widgets/create-document/CreateDocumentModal.vue';
import CreateTitlePageModal from '@/widgets/create-title-page/CreateTitlePageModal.vue';
import InfoBanner from '@/widgets/info-banner/InfoBanner.vue';
import ThemeToggle from '@/features/theme-toggle/ThemeToggle.vue';
import ChatDock from '@/features/agent/ChatDock.vue';
import NotificationBell from '@/features/notifications/NotificationBell.vue';
import CollabAPI from '@/shared/api/CollabAPI';
import Modal from '@/shared/ui/Modal/Modal.vue';
import Button from '@/shared/ui/Button/Button.vue';
import Icon from '@/components/Icon.vue';
import { getDefaultProfileData } from '@/utils/profileDefaults';
import type { Profile } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';

const router = useRouter();
const authStore = useAuthStore();

const activeTab = ref<'docs' | 'profiles' | 'shared'>('docs');
const searchQueryInput = ref('');
const debouncedSearchQuery = useDebounce(searchQueryInput, 300);
const sortBy = ref<'name' | 'updatedAt'>('updatedAt');
const sortOrder = ref<'asc' | 'desc'>('desc');
const isLoading = ref(false);

const profiles = ref<Profile[]>([]);
const documents = ref<DocumentMeta[]>([]);
const titlePages = ref<any[]>([]);
const showCreateModal = ref(false);
const showCreateTitlePageModal = ref(false);
const isCreating = ref(false);
const showAIPanel = ref(false);
const showExportPdfModal = ref(false);
const exportModalWithPicker = ref(false);
const exportModalDocumentId = ref<string | undefined>(undefined);
const exportModalDocumentName = ref<string | undefined>(undefined);
const showImportSuccessModal = ref(false);
const importedDocumentId = ref<string | null>(null);

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

/** Строки таблицы документов (поиск + сортировка на главной). */
const docsTableRows = computed((): DocumentMeta[] =>
	activeTab.value === 'docs' ? (filteredItems.value as DocumentMeta[]) : [],
);

const toast = ref<{ visible: boolean; text: string; variant: 'success' | 'error' }>({
	visible: false,
	text: '',
	variant: 'error',
});
let mainToastTimer: ReturnType<typeof setTimeout> | null = null;

function showMainToast(text: string, variant: 'success' | 'error' = 'error') {
	if (mainToastTimer) clearTimeout(mainToastTimer);
	toast.value = { visible: true, text, variant };
	mainToastTimer = setTimeout(() => {
		toast.value = { ...toast.value, visible: false };
		mainToastTimer = null;
	}, 3800);
}

const showDeleteConfirm = ref(false);
const deletePending = ref<{ kind: 'document' | 'profile' | 'title'; id: string; name: string } | null>(null);

const deleteConfirmText = computed(() => {
	const p = deletePending.value;
	if (!p) return '';
	const label =
		p.kind === 'document' ? 'документ' : p.kind === 'profile' ? 'профиль стилей' : 'титульный лист';
	return `Удалить ${label} «${p.name}»? Это действие нельзя отменить.`;
});

function openDeleteDocumentConfirm(doc: DocumentMeta) {
	deletePending.value = { kind: 'document', id: doc.id, name: doc.name };
	showDeleteConfirm.value = true;
}

function openDeleteProfileConfirm(item: Profile) {
	deletePending.value = { kind: 'profile', id: item.id, name: item.name };
	showDeleteConfirm.value = true;
}

function openDeleteTitlePageConfirm(item: { id: string; name: string }) {
	deletePending.value = { kind: 'title', id: item.id, name: item.name };
	showDeleteConfirm.value = true;
}

function cancelDeleteConfirm() {
	showDeleteConfirm.value = false;
	deletePending.value = null;
}

async function confirmDelete() {
	const p = deletePending.value;
	if (!p) return;
	try {
		if (p.kind === 'document') await DocumentAPI.delete(p.id);
		else if (p.kind === 'profile') await ProfileAPI.delete(p.id);
		else await TitlePageAPI.delete(p.id);
		await loadData();
		cancelDeleteConfirm();
	} catch (e: any) {
		const msg = e?.response?.data?.message || e?.message || 'Не удалось удалить';
		showMainToast(msg, 'error');
	}
}

const showLeaveCollabConfirm = ref(false);
const leaveCollabDoc = ref<DocumentMeta | null>(null);

function requestLeaveCollab(doc: DocumentMeta) {
	leaveCollabDoc.value = doc;
	showLeaveCollabConfirm.value = true;
}

function cancelLeaveCollabConfirm() {
	showLeaveCollabConfirm.value = false;
	leaveCollabDoc.value = null;
}

async function confirmLeaveCollab() {
	const doc = leaveCollabDoc.value;
	if (!doc) return;
	try {
		await CollabAPI.leaveCollab(doc.id);
		await loadData();
		cancelLeaveCollabConfirm();
	} catch (e: any) {
		const msg = e?.response?.data?.message || e?.message || 'Не удалось покинуть соавторство';
		showMainToast(msg, 'error');
	}
}

async function loadData() {
	isLoading.value = true;
	try {
		const [profilesData, documentsData, titlePagesData] = await Promise.all([
			ProfileAPI.getAll().catch((err) => {
				console.error('Failed to load profiles:', err);
				return [];
			}),
			DocumentAPI.getAll(undefined, debouncedSearchQuery.value || undefined).catch((err) => {
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
	if (activeTab.value === 'shared') {
		showCreateTitlePageModal.value = true;
		return;
	}

	isCreating.value = true;
	try {
		const profile = await ProfileAPI.create({
			name: 'Новый профиль стилей',
			data: getDefaultProfileData(),
		});
		router.push(`/profile/${profile.id}`);
		await loadData();
	} catch (error: any) {
		console.error('Failed to create:', error);
		const errorMessage = error?.message || error?.response?.data?.message || 'Ошибка при создании';
		alert(`Ошибка при создании: ${errorMessage}`);
	} finally {
		isCreating.value = false;
	}
}

function handleTitlePageCreatedFromModal(titlePageId: string) {
	router.push(`/title-page/${titlePageId}`);
	loadData();
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

function handleFilesImported(documentId: string) {
	importedDocumentId.value = documentId;
	showImportSuccessModal.value = true;
	loadData();
}

function closeImportSuccessModal() {
	showImportSuccessModal.value = false;
	importedDocumentId.value = null;
}

function goToImportedDocument() {
	if (!importedDocumentId.value) return;
	const id = importedDocumentId.value;
	closeImportSuccessModal();
	router.push(`/document/${id}`);
}

function openShareExportModal() {
	exportModalWithPicker.value = true;
	exportModalDocumentId.value = undefined;
	exportModalDocumentName.value = undefined;
	showExportPdfModal.value = true;
}

function handleDocumentAction(document: DocumentMeta, action: string) {
	if (action === 'open') {
		router.push(`/document/${document.id}`);
	} else if (action === 'export-pdf') {
		openDocumentExportModal(document);
	} else if (action === 'delete') {
		openDeleteDocumentConfirm(document);
	} else if (action === 'leave') {
		requestLeaveCollab(document);
	}
}

function downloadFile(blob: Blob, filename: string) {
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}

function openDocumentExportModal(document: DocumentMeta) {
	exportModalWithPicker.value = false;
	exportModalDocumentId.value = document.id;
	exportModalDocumentName.value = document.name;
	showExportPdfModal.value = true;
}

async function handleProfileExportDdoc(item: Profile) {
	try {
		const blob = await ProfileAPI.exportDdoc(item.id);
		downloadFile(blob, `${item.name}.ddoc`);
	} catch (e) {
		console.error(e);
		alert('Не удалось скачать .ddoc');
	}
}

async function handleTitlePageExportDdoc(item: { id: string; name: string }) {
	try {
		const blob = await TitlePageAPI.exportDdoc(item.id);
		downloadFile(blob, `${item.name}.ddoc`);
	} catch (e) {
		console.error(e);
		alert('Не удалось скачать .ddoc');
	}
}

function handleSettings() {
	router.push('/settings');
}

const handleChatDockWidthChanged = (_width: number) => {
	// Layout can react to panel width if needed
};

const handleAgentDocumentChanged = async () => {
	await loadData();
};

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
				router.push('/auth');
			}
		}
	} else {
		// Если пользователь не авторизован, редиректим на страницу входа
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
	min-height: 0;
	overflow: hidden;
	padding: 0 var(--spacing-xl);
}

.content-body__main {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	margin-top: 1rem;
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

.main-page__share-btn {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 40px;
	height: 40px;
	padding: 0;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	color: var(--text-primary);
	cursor: pointer;
	transition: all 0.2s ease;
}
.main-page__share-btn:hover {
	border-color: var(--accent);
	color: var(--accent);
}

.main-page__ai-btn {
	padding: 0.5rem 1rem;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	font-size: 13px;
	font-weight: 600;
	color: var(--text-primary);
	cursor: pointer;
	transition: all 0.2s ease;
}

.main-page__ai-btn:hover {
	background: var(--bg-tertiary);
	border-color: var(--accent);
	color: var(--accent);
}

.main-page__ai-btn--active {
	background: var(--accent);
	color: white;
	border-color: var(--accent);
}

.main-page__import-message {
	margin: 0;
	font-size: 14px;
	color: var(--text-primary);
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
	width: 100%;
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

.items-table__name {
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
}

.items-table__col-actions {
	width: 120px;
}

.items-table__col-title--sortable {
	cursor: pointer;
	user-select: none;
}

.items-table__col-title--sortable:hover {
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

.main-page__toast {
	position: fixed;
	top: 24px;
	left: 50%;
	transform: translateX(-50%);
	padding: 12px 22px;
	font-size: 14px;
	font-weight: 500;
	border-radius: var(--radius-md);
	box-shadow: var(--shadow-lg);
	z-index: 10001;
	max-width: min(520px, calc(100vw - 32px));
	text-align: center;
	line-height: 1.4;
}

.main-page__toast--success {
	background: #16a34a;
	color: #fff;
}

.main-page__toast--error {
	background: #dc2626;
	color: #fff;
}

.main-toast-enter-active,
.main-toast-leave-active {
	transition: opacity 0.25s ease, transform 0.25s ease;
}

.main-toast-enter-from,
.main-toast-leave-to {
	opacity: 0;
	transform: translate(-50%, -10px);
}

.main-page__confirm-text {
	margin: 0;
	font-size: 14px;
	line-height: 1.55;
	color: var(--text-primary);
}

.main-page__modal-footer {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	flex-wrap: wrap;
}

.main-page__modal-btn {
	padding: 8px 16px;
	font-size: 14px;
	font-weight: 500;
	border-radius: var(--radius-md);
	cursor: pointer;
	font-family: inherit;
	border: 1px solid transparent;
}

.main-page__modal-btn--secondary {
	background: var(--bg-secondary);
	border-color: var(--border-color);
	color: var(--text-primary);
}

.main-page__modal-btn--danger {
	background: var(--danger, #ef4444);
	color: #fff;
	border-color: var(--danger, #ef4444);
}

.main-page__modal-btn--danger:hover {
	filter: brightness(1.05);
}
</style>
