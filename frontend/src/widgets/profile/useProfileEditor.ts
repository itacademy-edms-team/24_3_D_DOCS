import { ref, onMounted } from 'vue';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import type { Profile, UpdateProfileDTO } from '@/entities/profile/types';
import type { EntityType } from '@/entities/profile/constants';

export function useProfileEditor(profileId: string | undefined) {
	const profile = ref<Profile | null>(null);
	const loading = ref(true);
	const saving = ref(false);
	const selectedEntity = ref<EntityType>('paragraph');

	function init() {
		if (profileId) {
			loadProfile(profileId);
		} else {
			loading.value = false;
		}
	}

	onMounted(() => {
		init();
	});

	async function loadProfile(id: string) {
		try {
			loading.value = true;
			const data = await ProfileAPI.getById(id);
			profile.value = data;
		} catch (error) {
			console.error('Failed to load profile:', error);
		} finally {
			loading.value = false;
		}
	}

	async function handleSave() {
		if (!profile.value || !profileId) return;

		saving.value = true;
		try {
			const updateData: UpdateProfileDTO = {
				name: profile.value.name,
				page: profile.value.page,
				entities: profile.value.entities,
			};
			const updated = await ProfileAPI.update(profileId, updateData);
			if (updated) {
				profile.value = updated;
			}
		} catch (error) {
			console.error('Failed to save profile:', error);
		} finally {
			saving.value = false;
		}
	}

	function handleNameChange(name: string) {
		if (!profile.value) return;
		profile.value = { ...profile.value, name };
	}

	function handlePageSettingChange<K extends keyof Profile['page']>(
		key: K,
		value: Profile['page'][K],
	) {
		if (!profile.value) return;
		profile.value = {
			...profile.value,
			page: { ...profile.value.page, [key]: value },
		};
	}

	function handleMarginChange(side: 'top' | 'right' | 'bottom' | 'left', value: number) {
		if (!profile.value) return;
		profile.value = {
			...profile.value,
			page: {
				...profile.value.page,
				margins: { ...profile.value.page.margins, [side]: value },
			},
		};
	}

	function handleEntityStyleChange(entityType: EntityType, style: any) {
		if (!profile.value) return;
		profile.value = {
			...profile.value,
			entities: { ...profile.value.entities, [entityType]: style },
		};
	}

	function handleResetEntityStyle(entityType: EntityType) {
		if (!profile.value) return;
		const newEntities = { ...profile.value.entities };
		delete newEntities[entityType];
		profile.value = { ...profile.value, entities: newEntities };
	}

	return {
		profile,
		loading,
		saving,
		selectedEntity,
		setSelectedEntity: (entity: EntityType) => {
			selectedEntity.value = entity;
		},
		handleSave,
		handleNameChange,
		handlePageSettingChange,
		handleMarginChange,
		handleEntityStyleChange,
		handleResetEntityStyle,
	};
}
