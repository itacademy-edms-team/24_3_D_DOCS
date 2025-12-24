<template>
	<div class="main-layout">
		<Sidebar
			:active-tab="activeTab"
			@tab-change="activeTab = $event"
			@logout="handleLogout"
			@settings="handleSettings"
		/>

		<main class="main-content">
			<Header
				:title="pageTitle"
				:active-tab="activeTab"
				@new-project="handleNewProject"
			/>

			<SearchBar
				v-model="searchQueryInput"
				:placeholder="searchPlaceholder"
			/>

			<DataTable
				:items="filteredItems"
				:is-loading="isLoading"
				@item-click="handleItemClick"
				@delete="handleDelete"
				@update="handleUpdate"
			/>

			<div class="table-footer">
				<span class="items-count">
					Показано {{ filteredItems.length }} из {{ totalItems }}
				</span>
			</div>
		</main>
	</div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/entities/auth/store/authStore';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import Sidebar from '@/widgets/main/Sidebar/Sidebar.vue';
import Header from '@/widgets/main/Header/Header.vue';
import SearchBar from '@/widgets/main/SearchBar/SearchBar.vue';
import DataTable from '@/widgets/main/DataTable/DataTable.vue';
import { useMainPage } from '@/widgets/main/useMainPage';
import type { ProfileMeta } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';
import type { TitlePageMeta } from '@/entities/title-page/types';

const router = useRouter();
const authStore = useAuthStore();

const {
	activeTab,
	searchQueryInput,
	isLoading,
	filteredItems,
	totalItems,
	pageTitle,
	searchPlaceholder,
	loadData,
} = useMainPage();

function handleItemClick(item: ProfileMeta | DocumentMeta | TitlePageMeta) {
	if (activeTab.value === 'profiles') {
		router.push(`/profile/${item.id}`);
	} else if (activeTab.value === 'title-pages') {
		router.push(`/title-page/${item.id}`);
	} else {
		router.push(`/document/${item.id}`);
	}
}

async function handleNewProject() {
	try {
		if (activeTab.value === 'profiles') {
			await ProfileAPI.create({ name: 'Новый шаблон' });
		} else if (activeTab.value === 'title-pages') {
			await TitlePageAPI.create({ name: 'Новый титульный лист' });
		} else {
			await DocumentAPI.create({ name: 'Новый документ' });
		}
		// Reload data to show new item
		await loadData();
	} catch (error) {
		console.error('Failed to create item:', error);
		alert('Ошибка при создании элемента: ' + (error instanceof Error ? error.message : String(error)));
	}
}

async function handleDelete(item: ProfileMeta | DocumentMeta | TitlePageMeta) {
	if (!confirm(`Вы уверены, что хотите удалить "${item.name || 'элемент'}"?`)) {
		return;
	}

	try {
		if (activeTab.value === 'profiles') {
			await ProfileAPI.delete(item.id);
		} else if (activeTab.value === 'title-pages') {
			await TitlePageAPI.delete(item.id);
		} else {
			await DocumentAPI.delete(item.id);
		}
		await loadData();
	} catch (error) {
		console.error('Failed to delete item:', error);
		alert('Ошибка при удалении элемента');
	}
}

async function handleUpdate(item: ProfileMeta | DocumentMeta | TitlePageMeta, name: string) {
	try {
		if (activeTab.value === 'profiles') {
			await ProfileAPI.update(item.id, { name });
		} else if (activeTab.value === 'title-pages') {
			await TitlePageAPI.update(item.id, { name });
		} else {
			await DocumentAPI.update(item.id, { name });
		}
		await loadData();
	} catch (error) {
		console.error('Failed to update item:', error);
		alert('Ошибка при обновлении названия');
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

.main-content {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	background: #0a0a0a;
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
