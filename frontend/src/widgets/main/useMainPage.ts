import { ref, computed } from 'vue';
import { useDebounce } from '@vueuse/core';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import type { ProfileMeta } from '@/entities/profile/types';
import type { DocumentMeta } from '@/entities/document/types';

export type TabType = 'docs' | 'profiles';

export function useMainPage() {
	const activeTab = ref<TabType>('docs');
	const searchQueryInput = ref('');
	const debouncedSearchQuery = useDebounce(searchQueryInput, 300);
	const isLoading = ref(false);

	const profiles = ref<ProfileMeta[]>([]);
	const documents = ref<DocumentMeta[]>([]);

	const currentItems = computed(() => {
		if (activeTab.value === 'profiles') {
			return profiles.value;
		}
		return documents.value;
	});

	const filteredItems = computed(() => {
		let items = [...currentItems.value];

		if (debouncedSearchQuery.value.trim()) {
			const query = debouncedSearchQuery.value.toLowerCase();
			items = items.filter((item) =>
				(item.name || '').toLowerCase().includes(query),
			);
		}

		// Сортировка по дате изменения (новые сверху)
		items.sort((a, b) => {
			const aValue = new Date(a.updatedAt).getTime();
			const bValue = new Date(b.updatedAt).getTime();
			return bValue - aValue;
		});

		return items;
	});

	const totalItems = computed(() => currentItems.value.length);

	const tabTitles: Record<TabType, string> = {
		docs: 'Документы',
		profiles: 'Шаблоны',
	};

	const pageTitle = computed(() => tabTitles[activeTab.value]);

	const searchPlaceholder = computed(() => {
		return `Поиск в ${activeTab.value === 'docs' ? 'документах' : 'шаблонах'}...`;
	});

	async function loadData() {
		isLoading.value = true;
		try {
			const [profilesData, documentsData] = await Promise.all([
				ProfileAPI.getAll().catch(() => []),
				DocumentAPI.getAll().catch(() => []),
			]);
			profiles.value = profilesData;
			documents.value = documentsData;
		} catch (error) {
			console.error('Failed to load data:', error);
		} finally {
			isLoading.value = false;
		}
	}

	return {
		activeTab,
		searchQueryInput,
		isLoading,
		filteredItems,
		totalItems,
		pageTitle,
		searchPlaceholder,
		loadData,
	};
}
