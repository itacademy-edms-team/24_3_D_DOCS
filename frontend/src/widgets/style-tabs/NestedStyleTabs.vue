<template>
	<div class="nested-style-tabs">
		<!-- Main tabs row -->
		<div class="nested-style-tabs__main-header" role="tablist">
			<button
				v-for="tab in mainTabs"
				:key="tab.value"
				:class="['nested-style-tabs__main-tab', { 'nested-style-tabs__main-tab--active': activeMainTab === tab.value }]"
				@click="handleMainTabClick(tab.value)"
				:aria-selected="activeMainTab === tab.value"
				role="tab"
			>
				{{ tab.label }}
			</button>
		</div>

		<!-- Sub tabs row (shown when a group is active) -->
		<div
			v-if="activeSubTabs.length > 0"
			class="nested-style-tabs__sub-header"
			role="tablist"
		>
			<button
				v-for="tab in activeSubTabs"
				:key="tab.value"
				:class="['nested-style-tabs__sub-tab', { 'nested-style-tabs__sub-tab--active': activeSubTab === tab.value }]"
				@click="handleSubTabClick(tab.value)"
				:aria-selected="activeSubTab === tab.value"
				role="tab"
			>
				{{ tab.label }}
			</button>
		</div>

		<!-- Content -->
		<div class="nested-style-tabs__content">
			<EntityStyleForm
				v-for="entityType in allEntityTypes"
				:key="entityType"
				v-show="activeEntityType === entityType"
				:entityType="entityType"
				:style="entityStyles[entityType] || {}"
				:pageSettings="pageSettings"
				:headingNumbering="headingNumbering"
				@update:style="handleStyleUpdate(entityType, $event)"
				@update:headingNumbering="handleHeadingNumberingUpdate"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import EntityStyleForm from './EntityStyleForm.vue';
import type { EntityStyle, PageSettings, HeadingNumberingSettings } from '@/entities/profile/types';

interface Tab {
	value: string;
	label: string;
}

interface TabGroup {
	type: 'group';
	value: string;
	label: string;
	children: Tab[];
}

type TabOrGroup = Tab | TabGroup;

interface Props {
	tabs: TabOrGroup[];
	entityStyles: Record<string, EntityStyle>;
	pageSettings: PageSettings;
	headingNumbering?: HeadingNumberingSettings;
	modelValue?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
	'update:entityStyles': [entityType: string, style: EntityStyle];
	'update:headingNumbering': [settings: HeadingNumberingSettings];
}>();

const mainTabs = computed<Tab[]>(() =>
	props.tabs.map((tab) => {
		if ('type' in tab && tab.type === 'group') {
			return { value: tab.value, label: tab.label };
		}
		return tab;
	}),
);

const getGroupByValue = (value: string): TabGroup | null => {
	const tab = props.tabs.find((t) => t.value === value);
	if (tab && 'type' in tab && tab.type === 'group') {
		return tab as TabGroup;
	}
	return null;
};

const activeMainTab = ref<string>(
	props.modelValue
		? props.tabs.find((t) => {
				if ('type' in t && t.type === 'group') {
					return t.children.some((c) => c.value === props.modelValue);
				}
				return t.value === props.modelValue;
			})?.value || props.tabs[0]?.value || ''
		: props.tabs[0]?.value || '',
);

const activeSubTab = ref<string>('');

// Get all entity types (flat list)
const allEntityTypes = computed<string[]>(() => {
	const types: string[] = [];
	props.tabs.forEach((tab) => {
		if ('type' in tab && tab.type === 'group') {
			types.push(...tab.children.map((c) => c.value));
		} else {
			types.push(tab.value);
		}
	});
	return types;
});

// Get active sub tabs
const activeSubTabs = computed<Tab[]>(() => {
	const group = getGroupByValue(activeMainTab.value);
	if (group) {
		return group.children;
	}
	return [];
});

// Current active entity type
const activeEntityType = computed<string>(() => {
	const group = getGroupByValue(activeMainTab.value);
	if (group) {
		if (activeSubTab.value && group.children.some((c) => c.value === activeSubTab.value)) {
			return activeSubTab.value;
		}
		// Default to first child if no sub tab selected
		return group.children[0]?.value || '';
	}
	return activeMainTab.value;
});

// Initialize activeSubTab when main tab changes
watch(activeMainTab, (newValue) => {
	const group = getGroupByValue(newValue);
	if (group) {
		// Check if modelValue matches one of the children
		const matchingChild = group.children.find((c) => c.value === props.modelValue);
		activeSubTab.value = matchingChild?.value || group.children[0]?.value || '';
	} else {
		activeSubTab.value = '';
	}
	emit('update:modelValue', activeEntityType.value);
});

// Initialize from modelValue
watch(
	() => props.modelValue,
	(newValue) => {
		if (!newValue) return;

		// Find which main tab contains this entity type
		const matchingTab = props.tabs.find((tab) => {
			if ('type' in tab && tab.type === 'group') {
				return tab.children.some((c) => c.value === newValue);
			}
			return tab.value === newValue;
		});

		if (matchingTab) {
			activeMainTab.value = matchingTab.value;

			if ('type' in matchingTab && matchingTab.type === 'group') {
				const child = matchingTab.children.find((c) => c.value === newValue);
				if (child) {
					activeSubTab.value = child.value;
				}
			}
		}
	},
	{ immediate: true },
);

const handleMainTabClick = (value: string) => {
	activeMainTab.value = value;
	const group = getGroupByValue(value);
	if (group) {
		activeSubTab.value = group.children[0]?.value || '';
	}
	emit('update:modelValue', activeEntityType.value);
};

const handleSubTabClick = (value: string) => {
	activeSubTab.value = value;
	emit('update:modelValue', activeEntityType.value);
};

const handleStyleUpdate = (entityType: string, style: EntityStyle) => {
	emit('update:entityStyles', entityType, style);
};

const handleHeadingNumberingUpdate = (settings: HeadingNumberingSettings) => {
	emit('update:headingNumbering', settings);
};
</script>

<style scoped>
.nested-style-tabs {
	display: flex;
	flex-direction: column;
}

.nested-style-tabs__main-header {
	display: flex;
	gap: var(--spacing-xs);
	border-bottom: 2px solid var(--border-color);
	margin-bottom: 0;
}

.nested-style-tabs__main-tab {
	padding: var(--spacing-sm) var(--spacing-md);
	background: transparent;
	border: none;
	border-bottom: 2px solid transparent;
	color: var(--text-secondary);
	font-size: 14px;
	font-weight: 500;
	cursor: pointer;
	transition: all 0.2s ease;
	margin-bottom: -2px;
}

.nested-style-tabs__main-tab:hover {
	color: var(--text-primary);
}

.nested-style-tabs__main-tab--active {
	color: var(--accent);
	border-bottom-color: var(--accent);
}

.nested-style-tabs__sub-header {
	display: flex;
	gap: var(--spacing-xs);
	border-bottom: 2px solid var(--border-color);
	margin-bottom: var(--spacing-lg);
	padding-left: var(--spacing-md);
}

.nested-style-tabs__sub-tab {
	padding: var(--spacing-sm) var(--spacing-md);
	background: transparent;
	border: none;
	border-bottom: 2px solid transparent;
	color: var(--text-secondary);
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	transition: all 0.2s ease;
	margin-bottom: -2px;
}

.nested-style-tabs__sub-tab:hover {
	color: var(--text-primary);
}

.nested-style-tabs__sub-tab--active {
	color: var(--accent);
	border-bottom-color: var(--accent);
}

.nested-style-tabs__content {
	margin-top: 0;
}
</style>