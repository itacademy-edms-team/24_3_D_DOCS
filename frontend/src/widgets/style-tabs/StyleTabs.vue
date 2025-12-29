<template>
	<div class="style-tabs">
		<Tabs v-model="activeTab" :tabs="tabs" />
		<div class="style-tabs__content">
			<EntityStyleForm
				v-for="tab in tabs"
				:key="tab.value"
				v-show="activeTab === tab.value"
				:entityType="tab.value"
				:style="entityStyles[tab.value] || {}"
				:pageSettings="pageSettings"
				@update:style="handleStyleUpdate(tab.value, $event)"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import Tabs from '@/shared/ui/Tabs/Tabs.vue';
import EntityStyleForm from './EntityStyleForm.vue';
import type { EntityStyle, PageSettings } from '@/entities/profile/types';

interface Props {
	entityTypes: Array<{ value: string; label: string }>;
	entityStyles: Record<string, EntityStyle>;
	pageSettings: PageSettings;
	modelValue?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
	'update:entityStyles': [entityType: string, style: EntityStyle];
}>();

const activeTab = ref(props.modelValue || props.entityTypes[0]?.value || 'paragraph');

const tabs = computed(() =>
	props.entityTypes.map((et) => ({ value: et.value, label: et.label })),
);

const handleStyleUpdate = (entityType: string, style: EntityStyle) => {
	emit('update:entityStyles', entityType, style);
};
</script>

<style scoped>
.style-tabs {
	display: flex;
	flex-direction: column;
}

.style-tabs__content {
	margin-top: var(--spacing-lg);
}
</style>
