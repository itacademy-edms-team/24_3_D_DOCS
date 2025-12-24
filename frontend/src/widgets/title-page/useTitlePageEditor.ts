import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { TitlePage, TitlePageElement, TitlePageElementType } from '@/shared/types/titlePage';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';

export function useTitlePageEditor() {
	const route = useRoute();
	const router = useRouter();

	const titlePage = ref<TitlePage | null>(null);
	const selectedElementIds = ref<string[]>([]);
	const currentTool = ref<TitlePageElementType | null>(null);
	const saving = ref(false);
	const loading = ref(true);

	const selectedElement = computed(() => {
		if (selectedElementIds.value.length !== 1) return null;
		return (
			titlePage.value?.elements.find(
				(el) => el.id === selectedElementIds.value[0]
			) || null
		);
	});

	async function loadTitlePage() {
		const id = route.params.id as string;
		if (!id) {
			loading.value = false;
			return;
		}

		try {
			loading.value = true;
			titlePage.value = await TitlePageAPI.getById(id);
		} catch (error) {
			console.error('Failed to load title page:', error);
			titlePage.value = null;
		} finally {
			loading.value = false;
		}
	}

	async function saveTitlePage() {
		if (!titlePage.value) return;

		try {
			saving.value = true;
			await TitlePageAPI.update(titlePage.value.id, {
				name: titlePage.value.name,
				elements: titlePage.value.elements,
				variables: titlePage.value.variables,
			});
		} catch (error) {
			console.error('Failed to save title page:', error);
			throw error;
		} finally {
			saving.value = false;
		}
	}

	function handleElementSelect(ids: string[]) {
		selectedElementIds.value = ids;
	}

	function handleElementToggle(id: string) {
		if (selectedElementIds.value.includes(id)) {
			selectedElementIds.value = selectedElementIds.value.filter(
				(elId) => elId !== id
			);
		} else {
			selectedElementIds.value = [...selectedElementIds.value, id];
		}
	}

	function handleElementMove(id: string, x: number, y: number) {
		if (!titlePage.value) return;
		const element = titlePage.value.elements.find((el) => el.id === id);
		if (element) {
			element.x = x;
			element.y = y;
		}
	}

	function handleElementsMove(ids: string[], deltaX: number, deltaY: number) {
		if (!titlePage.value) return;
		ids.forEach((id) => {
			const element = titlePage.value!.elements.find((el) => el.id === id);
			if (element) {
				element.x += deltaX;
				element.y += deltaY;
			}
		});
	}

	function handleElementAdd(type: TitlePageElementType, x: number, y: number) {
		if (!titlePage.value) return;

		const newElement: TitlePageElement = {
			id: crypto.randomUUID(),
			type,
			x,
			y,
			...(type === 'text' && { content: 'Новый текст' }),
			...(type === 'variable' && { variableKey: 'key' }),
			...(type === 'line' && { length: 100, thickness: 1 }),
		};

		titlePage.value.elements.push(newElement);
		selectedElementIds.value = [newElement.id];
	}

	function handleElementUpdate(element: TitlePageElement) {
		if (!titlePage.value) return;
		const index = titlePage.value.elements.findIndex(
			(el) => el.id === element.id
		);
		if (index !== -1) {
			// Создаем новый массив для правильной реактивности Vue
			const newElements = [...titlePage.value.elements];
			// Создаем новый объект элемента для правильной реактивности
			newElements[index] = { ...newElements[index], ...element };
			titlePage.value.elements = newElements;
		}
	}

	function handleElementDelete() {
		if (!titlePage.value || selectedElementIds.value.length === 0) return;
		titlePage.value.elements = titlePage.value.elements.filter(
			(el) => !selectedElementIds.value.includes(el.id)
		);
		selectedElementIds.value = [];
	}

	function handleNameChange(name: string) {
		if (titlePage.value) {
			titlePage.value.name = name;
		}
	}

	function handleToolChange(tool: TitlePageElementType | null) {
		currentTool.value = tool;
	}

	return {
		titlePage,
		selectedElementIds,
		selectedElement,
		currentTool,
		saving,
		loading,
		loadTitlePage,
		saveTitlePage,
		handleElementSelect,
		handleElementToggle,
		handleElementMove,
		handleElementsMove,
		handleElementAdd,
		handleElementUpdate,
		handleElementDelete,
		handleNameChange,
		handleToolChange,
	};
}
