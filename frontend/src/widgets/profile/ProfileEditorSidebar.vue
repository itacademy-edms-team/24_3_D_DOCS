<template>
	<div class="sidebar">
		<PageSettingsEditor
			:page-settings="profile.page"
			@page-setting-change="handlePageSettingChange"
			@margin-change="handleMarginChange"
		/>

		<div class="entities-card">
			<h3 class="card-title">Стили сущностей</h3>
			<ul class="entity-list">
				<li
					v-for="entityType in ALL_ENTITY_TYPES"
					:key="entityType"
					class="entity-item"
					:class="{ selected: selectedEntity === entityType }"
					@click="$emit('entity-select', entityType)"
				>
					<span>{{ ENTITY_LABELS[entityType] }}</span>
				</li>
			</ul>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { Profile } from '@/entities/profile/types';
import type { EntityType } from '@/entities/profile/constants';
import { ALL_ENTITY_TYPES, ENTITY_LABELS } from '@/entities/profile/constants';
import PageSettingsEditor from './PageSettingsEditor.vue';

interface Props {
	profile: Profile;
	selectedEntity: EntityType;
}

defineProps<Props>();

const emit = defineEmits<{
	'entity-select': [entityType: EntityType];
	'page-setting-change': [key: keyof Profile['page'], value: any];
	'margin-change': [side: 'top' | 'right' | 'bottom' | 'left', value: number];
}>();

function handlePageSettingChange(key: keyof Profile['page'], value: any) {
	emit('page-setting-change', key, value);
}

function handleMarginChange(side: 'top' | 'right' | 'bottom' | 'left', value: number) {
	emit('margin-change', side, value);
}
</script>

<style scoped>
.sidebar {
	display: flex;
	flex-direction: column;
	gap: 1.5rem;
}

.entities-card {
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	padding: 1.5rem;
}

.card-title {
	font-size: 16px;
	font-weight: 600;
	color: #e4e4e7;
	margin: 0 0 1rem 0;
}

.entity-list {
	list-style: none;
	padding: 0;
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.entity-item {
	padding: 0.75rem 1rem;
	border-radius: 6px;
	cursor: pointer;
	transition: all 0.2s;
	display: flex;
	justify-content: space-between;
	align-items: center;
	border-left: 3px solid transparent;
	color: #a1a1aa;
	font-size: 14px;
}

.entity-item:hover {
	background: rgba(99, 102, 241, 0.05);
	color: #e4e4e7;
}

.entity-item.selected {
	background: rgba(99, 102, 241, 0.1);
	border-left-color: #6366f1;
	color: #e4e4e7;
	font-weight: 500;
}
</style>
