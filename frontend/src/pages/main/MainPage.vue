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
import Sidebar from '@/widgets/main/Sidebar/Sidebar.vue';
import Header from '@/widgets/main/Header/Header.vue';
import SearchBar from '@/widgets/main/SearchBar/SearchBar.vue';
import DataTable from '@/widgets/main/DataTable/DataTable.vue';
import { useMainPage } from '@/widgets/main/useMainPage';
import type { ProfileMeta } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';

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

function handleItemClick(item: ProfileMeta | DocumentMeta) {
	if (activeTab.value === 'profiles') {
		router.push(`/profile/${item.id}`);
	} else {
		router.push(`/document/${item.id}`);
	}
}

function handleNewProject() {
	if (activeTab.value === 'profiles') {
		ProfileAPI.create({ name: 'Новый шаблон' })
			.then((profile) => {
				router.push(`/profile/${profile.id}`);
			})
			.catch(console.error);
	} else {
		DocumentAPI.create({ name: 'Новый документ' })
			.then((doc) => {
				router.push(`/document/${doc.id}`);
			})
			.catch(console.error);
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
