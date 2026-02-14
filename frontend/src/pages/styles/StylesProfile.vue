<template>
	<div class="styles-profile">
		<!-- Header -->
		<div class="styles-profile__header">
			<input
				v-if="selectedProfileId"
				v-model="profileName"
				@input="handleNameChange"
				class="styles-profile__name-input"
				type="text"
				placeholder="Название профиля"
			/>
			<Dropdown
				v-model="selectedProfileId"
				:options="profileOptions"
				placeholder="Выберите профиль"
			/>
			<div class="styles-profile__header-actions">
				<Button @click="handleSave" :isLoading="isSaving">
					Сохранить
				</Button>
				<Button variant="secondary" @click="handleDelete" v-if="selectedProfileId">
					Удалить
				</Button>
			</div>
			<div class="styles-profile__status" v-if="lastSaved">
				Сохранено {{ formattedTimeAgo }}
			</div>
		</div>

		<!-- Page Settings Card -->
		<PageSettingsCard
			v-model="profileData.pageSettings"
			class="styles-profile__page-settings"
		/>

		<!-- Table of Contents Settings -->
		<TableOfContentsSettingsCard
			v-model="profileData.tableOfContents"
			class="styles-profile__toc-settings"
		/>

		<!-- Style Tabs -->
		<NestedStyleTabs
			v-model="activeEntityType"
			:tabs="tabGroups"
			:entityStyles="profileData.entityStyles"
			:pageSettings="profileData.pageSettings"
			:headingNumbering="profileData.headingNumbering"
			@update:entityStyles="(entityType, style) => handleEntityStyleUpdate(entityType, style)"
			@update:headingNumbering="handleHeadingNumberingUpdate"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDebounceFn } from '@vueuse/core';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import Button from '@/shared/ui/Button/Button.vue';
import PageSettingsCard from '@/widgets/style-tabs/PageSettingsCard.vue';
import TableOfContentsSettingsCard from '@/widgets/style-tabs/TableOfContentsSettingsCard.vue';
import NestedStyleTabs from '@/widgets/style-tabs/NestedStyleTabs.vue';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import { getDefaultProfileData } from '@/utils/profileDefaults';
import type { ProfileData, EntityStyle, HeadingNumberingSettings } from '@/entities/profile/types';

const route = useRoute();
const router = useRouter();

const selectedProfileId = ref<string | undefined>(route.params.id as string);
const profileData = ref<ProfileData>(getDefaultProfileData());
const activeEntityType = ref('paragraph');
const isSaving = ref(false);
const lastSaved = ref<Date | null>(null);
const profiles = ref<any[]>([]);
const profileName = ref<string>('');
const currentProfile = ref<any>(null);

const tabGroups = [
	{ value: 'paragraph', label: 'Параграф' },
	{
		type: 'group',
		value: 'headings',
		label: 'Заголовки',
		children: [
			{ value: 'heading-1', label: 'Заголовок H1' },
			{ value: 'heading-2', label: 'Заголовок H2' },
			{ value: 'heading-3', label: 'Заголовок H3' },
			{ value: 'heading-4', label: 'Заголовок H4' },
			{ value: 'heading-5', label: 'Заголовок H5' },
			{ value: 'heading-6', label: 'Заголовок H6' },
		],
	},
	{ value: 'ordered-list', label: 'Списки' },
	{ value: 'table', label: 'Таблицы' },
	{ value: 'image', label: 'Изображения' },
	{ value: 'formula', label: 'Формулы' },
	{ value: 'code-block', label: 'Код' },
	{ value: 'highlight', label: 'Выделенный текст' },
	{
		type: 'group',
		value: 'captions',
		label: 'Подписи',
		children: [
			{ value: 'image-caption', label: 'Подпись к рисункам' },
			{ value: 'table-caption', label: 'Подпись к таблицам' },
			{ value: 'formula-caption', label: 'Подпись к формулам' },
		],
	},
];

const profileOptions = computed(() =>
	profiles.value.map((p) => ({ value: p.id, label: p.name })),
);

const handleSave = async () => {
	if (!selectedProfileId.value) return;
	isSaving.value = true;
	try {
		await ProfileAPI.update(selectedProfileId.value, {
			data: profileData.value,
		});
		lastSaved.value = new Date();
	} catch (error) {
		console.error('Failed to save profile:', error);
		alert('Ошибка при сохранении');
	} finally {
		isSaving.value = false;
	}
};

const handleDelete = async () => {
	if (!selectedProfileId.value) return;
	if (!confirm('Удалить профиль?')) return;
	try {
		await ProfileAPI.delete(selectedProfileId.value);
		router.push('/dashboard');
	} catch (error) {
		console.error('Failed to delete profile:', error);
		alert('Ошибка при удалении');
	}
};

const handleEntityStyleUpdate = (entityType: string, style: EntityStyle) => {
	profileData.value.entityStyles[entityType] = style;
};

const handleHeadingNumberingUpdate = (settings: HeadingNumberingSettings) => {
	profileData.value.headingNumbering = settings;
};

const handleNameChange = useDebounceFn(async () => {
	if (!selectedProfileId.value) return;
	
	const newName = profileName.value.trim() || 'Новый профиль стилей';
	const previousName = currentProfile.value?.name || '';
	
	// Пропускаем если название не изменилось
	if (newName === previousName) return;
	
	try {
		await ProfileAPI.update(selectedProfileId.value, {
			name: newName,
		});
		// Обновляем локальное состояние после успешного сохранения
		if (currentProfile.value) {
			currentProfile.value.name = newName;
		}
		// Обновляем список профилей
		const index = profiles.value.findIndex(p => p.id === selectedProfileId.value);
		if (index !== -1) {
			profiles.value[index].name = newName;
		}
	} catch (error) {
		console.error('Failed to update profile name:', error);
		// Откатываем к предыдущему значению при ошибке
		profileName.value = previousName;
		alert('Ошибка при сохранении названия профиля');
	}
}, 1000);

const formatTimeAgo = (date: Date): string => {
	const diffMs = Date.now() - date.getTime();
	const diffMins = Math.floor(diffMs / 60000);
	if (diffMins < 1) return 'только что';
	if (diffMins < 60) return `${diffMins} мин. назад`;
	return `${Math.floor(diffMins / 60)} ч. назад`;
};

const formattedTimeAgo = computed(() => {
	if (!lastSaved.value) return '';
	return formatTimeAgo(lastSaved.value);
});

// Синхронизируем selectedProfileId с route.params
watch(
	() => route.params.id,
	(newId) => {
		if (newId && newId !== selectedProfileId.value) {
			selectedProfileId.value = newId as string;
		}
	},
	{ immediate: true },
);

// Merge default values with profile data
function mergeWithDefaults(data: ProfileData): ProfileData {
	const defaults = getDefaultProfileData();
	
	// Merge pageSettings
	const mergedPageSettings = {
		...defaults.pageSettings,
		...data.pageSettings,
		pageNumbers: {
			...defaults.pageSettings.pageNumbers,
			...data.pageSettings?.pageNumbers,
		},
		margins: {
			...defaults.pageSettings.margins,
			...data.pageSettings?.margins,
		},
	};
	
	// Merge entityStyles - for each type, merge defaults with profile styles
	const mergedEntityStyles: Record<string, EntityStyle> = {};
	
	// Get all entity types from defaults and data
	const allEntityTypes = new Set([
		...Object.keys(defaults.entityStyles),
		...Object.keys(data.entityStyles || {}),
	]);
	
	allEntityTypes.forEach((entityType) => {
		const defaultStyle = defaults.entityStyles[entityType] || {};
		const profileStyle = data.entityStyles?.[entityType] || {};
		
		// Deep merge: use profile style values if set, otherwise use defaults
		mergedEntityStyles[entityType] = {
			...defaultStyle,
			...profileStyle,
		};
	});
	
	return {
		...defaults,
		...data,
		pageSettings: mergedPageSettings,
		entityStyles: mergedEntityStyles,
		headingNumbering: data.headingNumbering || defaults.headingNumbering,
		tableOfContents: data.tableOfContents || defaults.tableOfContents,
	};
}

watch(selectedProfileId, async (id, oldId) => {
	// Избегаем повторной загрузки того же профиля
	if (id && id !== oldId) {
		try {
			const profile = await ProfileAPI.getById(id);
			currentProfile.value = profile;
			profileName.value = profile.name || '';
			if (profile.data) {
				profileData.value = mergeWithDefaults(profile.data);
			} else {
				profileData.value = getDefaultProfileData();
			}
		} catch (error) {
			console.error('Failed to load profile:', error);
		}
	}
});

onMounted(async () => {
	try {
		profiles.value = await ProfileAPI.getAll();
		if (selectedProfileId.value) {
			const profile = await ProfileAPI.getById(selectedProfileId.value);
			currentProfile.value = profile;
			profileName.value = profile.name || '';
			if (profile.data) {
				profileData.value = mergeWithDefaults(profile.data);
			} else {
				profileData.value = getDefaultProfileData();
			}
		}
	} catch (error) {
		console.error('Failed to load profiles:', error);
	}
});
</script>

<style scoped>
.styles-profile {
	padding: var(--spacing-xl);
	max-width: 1400px;
	margin: 0 auto;
}

.styles-profile__header {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	margin-bottom: var(--spacing-xl);
	padding-bottom: var(--spacing-lg);
	border-bottom: 2px solid var(--border-color);
}

.styles-profile__name-input {
	padding: var(--spacing-xs) var(--spacing-md);
	font-size: 16px;
	font-weight: 500;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	color: var(--text-primary);
	transition: border-color 0.2s ease, background-color 0.2s ease;
	min-width: 200px;
	font-family: inherit;
}

.styles-profile__name-input:hover {
	border-color: var(--border-hover);
	background: var(--bg-tertiary);
}

.styles-profile__name-input:focus {
	outline: none;
	border-color: var(--accent);
	background: var(--bg-secondary);
}

.styles-profile__header-actions {
	display: flex;
	gap: var(--spacing-sm);
	margin-left: auto;
}

.styles-profile__status {
	font-size: 13px;
	color: var(--text-secondary);
}

.styles-profile__page-settings,
.styles-profile__toc-settings {
	margin-bottom: var(--spacing-xl);
}
</style>
