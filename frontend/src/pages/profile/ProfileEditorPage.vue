<template>
	<div class="profile-editor-page">
		<div v-if="loading" class="loading-state">
			<div class="loading-text">Загрузка...</div>
		</div>

		<div v-else-if="!profile" class="error-state">
			<div class="error-text">Профиль не найден</div>
		</div>

		<div v-else class="editor-container">
			<!-- Header -->
			<div class="editor-header">
				<div class="header-left">
					<button class="back-btn" @click="handleBack">
						← Назад
					</button>
					<input
						type="text"
						class="profile-name-input"
						:value="profile.name"
						@input="handleNameChange($event)"
					/>
				</div>
				<button class="save-btn" @click="handleSave" :disabled="saving">
					{{ saving ? 'Сохранение...' : 'Сохранить' }}
				</button>
			</div>

			<!-- Main Content -->
			<div class="editor-content">
				<ProfileEditorSidebar
					:profile="profile"
					:selected-entity="selectedEntity"
					@entity-select="setSelectedEntity"
					@page-setting-change="handlePageSettingChange"
					@margin-change="handleMarginChange"
				/>

				<div class="editor-main">
					<EntityStyleEditor
						:entity-type="selectedEntity"
						:style="profile.entities[selectedEntity] || {}"
						:show-reset="!!profile.entities[selectedEntity]"
						@change="handleEntityStyleChange"
						@reset="handleResetEntityStyle"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { useProfileEditor } from '@/widgets/profile/useProfileEditor';
import ProfileEditorSidebar from '@/widgets/profile/ProfileEditorSidebar.vue';
import EntityStyleEditor from '@/widgets/profile/EntityStyleEditor.vue';
import type { Profile } from '@/entities/profile/types';

const route = useRoute();
const router = useRouter();
const profileId = route.params.id as string;

const {
	profile,
	loading,
	saving,
	selectedEntity,
	setSelectedEntity,
	handleSave,
	handleNameChange: handleNameChangeInternal,
	handlePageSettingChange,
	handleMarginChange,
	handleEntityStyleChange: handleEntityStyleChangeInternal,
	handleResetEntityStyle,
} = useProfileEditor(profileId);

function handleBack() {
	router.push('/dashboard');
}

function handleNameChange(event: Event) {
	const target = event.target as HTMLInputElement;
	handleNameChangeInternal(target.value);
}

function handleEntityStyleChange(style: Record<string, any>) {
	handleEntityStyleChangeInternal(selectedEntity.value, style);
}
</script>

<style scoped>
.profile-editor-page {
	min-height: 100vh;
	background: #0a0a0a;
	color: #e4e4e7;
}

.loading-state,
.error-state {
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 100vh;
}

.loading-text,
.error-text {
	font-size: 16px;
	color: #71717a;
}

.editor-container {
	display: flex;
	flex-direction: column;
	min-height: 100vh;
}

.editor-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 2rem;
	border-bottom: 1px solid #27272a;
	background: #0a0a0a;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.back-btn {
	padding: 0.5rem 1rem;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 14px;
	cursor: pointer;
	transition: all 0.2s;
	font-family: inherit;
}

.back-btn:hover {
	background: #27272a;
	color: #e4e4e7;
	border-color: #3f3f46;
}

.profile-name-input {
	padding: 0.5rem 0.75rem;
	background: transparent;
	border: 1px solid transparent;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 20px;
	font-weight: 600;
	outline: none;
	transition: all 0.2s;
	font-family: inherit;
	width: 300px;
}

.profile-name-input:focus {
	background: #18181b;
	border-color: #6366f1;
}

.save-btn {
	padding: 0.75rem 1.5rem;
	background: #6366f1;
	color: white;
	border: none;
	border-radius: 8px;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: background 0.2s;
	font-family: inherit;
}

.save-btn:hover:not(:disabled) {
	background: #818cf8;
}

.save-btn:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.editor-content {
	display: grid;
	grid-template-columns: 300px 1fr;
	gap: 1.5rem;
	padding: 1.5rem;
	flex: 1;
	overflow: hidden;
}

.editor-main {
	overflow-y: auto;
	padding-right: 0.5rem;
}
</style>
