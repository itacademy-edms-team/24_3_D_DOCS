<template>
	<Modal v-model="isOpen" :title="headerTitle" size="md" @update:modelValue="onClose">
		<div v-if="withEntityPicker && !picked" class="export-pdf-modal__picker">
			<div class="export-pdf-modal__tabs">
				<button
					type="button"
					:class="{ active: pickTab === 'docs' }"
					@click="pickTab = 'docs'"
				>
					Документы
				</button>
				<button
					type="button"
					:class="{ active: pickTab === 'profiles' }"
					@click="pickTab = 'profiles'"
				>
					Профили
				</button>
				<button
					type="button"
					:class="{ active: pickTab === 'titles' }"
					@click="pickTab = 'titles'"
				>
					Титульники
				</button>
			</div>
			<input
				v-model="pickerSearch"
				type="search"
				class="export-pdf-modal__search"
				placeholder="Поиск..."
			/>
			<ul class="export-pdf-modal__list">
				<li
					v-for="row in filteredPickerRows"
					:key="row.id"
					class="export-pdf-modal__list-item"
					@click="selectPickerRow(row)"
				>
					{{ row.name }}
				</li>
				<li v-if="filteredPickerRows.length === 0" class="export-pdf-modal__empty">Нет элементов</li>
			</ul>
		</div>

		<div v-else-if="withEntityPicker && picked?.kind === 'profile'" class="export-pdf-modal__ddoc-only">
			<p class="export-pdf-modal__hint">Профиль: <strong>{{ picked.name }}</strong></p>
			<p class="export-pdf-modal__sub">Будет скачан архив .ddoc с profile.json</p>
		</div>

		<div v-else-if="withEntityPicker && picked?.kind === 'titlePage'" class="export-pdf-modal__ddoc-only">
			<p class="export-pdf-modal__hint">Титульник: <strong>{{ picked.name }}</strong></p>
			<p class="export-pdf-modal__sub">Будет скачан архив .ddoc с titlepage.json</p>
		</div>

		<div v-else class="export-pdf-modal__doc">
			<div class="export-pdf-modal__field">
				<label class="export-pdf-modal__label">Титульная страница в PDF</label>
				<select v-model="exportTitlePageId" class="export-pdf-modal__select" :disabled="isLoadingTitlePages">
					<option value="">Без титульника (только визуал PDF)</option>
					<option v-for="tp in titlePages" :key="tp.id" :value="tp.id">
						{{ tp.name }}
					</option>
				</select>
			</div>
			<div class="export-pdf-modal__checks">
				<label class="export-pdf-modal__check">
					<input v-model="includeDocument" type="checkbox" />
					Документ (текст и вложения)
				</label>
				<label class="export-pdf-modal__check">
					<input v-model="includeStyleProfile" type="checkbox" />
					Профиль стиля
				</label>
				<label class="export-pdf-modal__check">
					<input v-model="includeTitlePage" type="checkbox" />
					Титульный лист (данные)
				</label>
			</div>
			<p v-if="!anyBundlePart" class="export-pdf-modal__warn">В PDF не будет вложен .ddoc</p>
		</div>

		<template #footer>
			<Button variant="secondary" @click="isOpen = false">Отмена</Button>
			<Button
				v-if="withEntityPicker && picked"
				variant="secondary"
				@click="clearPick"
			>
				Назад
			</Button>
			<Button
				v-if="isProfileDdoc"
				:isLoading="isWorking"
				:disabled="isWorking"
				@click="downloadProfileDdoc"
			>
				Скачать .ddoc
			</Button>
			<Button
				v-else-if="isTitleDdoc"
				:isLoading="isWorking"
				:disabled="isWorking"
				@click="downloadTitleDdoc"
			>
				Скачать .ddoc
			</Button>
			<Button
				v-else
				:isLoading="isWorking"
				:disabled="isWorking || !effectiveDocumentId"
				@click="downloadPdf"
			>
				Скачать PDF
			</Button>
		</template>
	</Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import Button from '@/shared/ui/Button/Button.vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import TitlePageAPI from '@/entities/title-page/api/TitlePageAPI';
import type { DocumentMeta } from '@/entities/document/types';
import type { Profile } from '@/entities/profile/types';

type TitlePageRow = { id: string; name: string };
type Picked = { kind: 'doc' | 'profile' | 'titlePage'; id: string; name: string };

interface Props {
	modelValue: boolean;
	/** Режим «Поделиться»: сначала выбрать сущность */
	withEntityPicker?: boolean;
	documentId?: string;
	documentName?: string;
	initialTitlePageId?: string;
}

const props = withDefaults(defineProps<Props>(), {
	withEntityPicker: false,
});

const emit = defineEmits<{
	'update:modelValue': [v: boolean];
}>();

const isOpen = computed({
	get: () => props.modelValue,
	set: (v) => emit('update:modelValue', v),
});

const headerTitle = computed(() =>
	props.withEntityPicker ? 'Поделиться / экспорт' : 'Экспорт PDF',
);

const pickTab = ref<'docs' | 'profiles' | 'titles'>('docs');
const pickerSearch = ref('');
const documents = ref<DocumentMeta[]>([]);
const profiles = ref<Profile[]>([]);
const titlePageList = ref<TitlePageRow[]>([]);
const titlePages = ref<TitlePageRow[]>([]);
const isLoadingTitlePages = ref(false);
const picked = ref<Picked | null>(null);

const exportTitlePageId = ref('');
const includeDocument = ref(true);
const includeStyleProfile = ref(true);
const includeTitlePage = ref(true);
const isWorking = ref(false);

const anyBundlePart = computed(
	() => includeDocument.value || includeStyleProfile.value || includeTitlePage.value,
);

const effectiveDocumentId = computed(() => {
	if (props.withEntityPicker) {
		if (picked.value?.kind === 'doc') return picked.value.id;
		return '';
	}
	return props.documentId ?? '';
});

const effectiveDocumentName = computed(() => {
	if (props.withEntityPicker && picked.value?.kind === 'doc') return picked.value.name;
	return props.documentName ?? 'document';
});

const isProfileDdoc = computed(
	() => props.withEntityPicker && picked.value?.kind === 'profile',
);
const isTitleDdoc = computed(
	() => props.withEntityPicker && picked.value?.kind === 'titlePage',
);

const filteredPickerRows = computed((): { id: string; name: string }[] => {
	const q = pickerSearch.value.trim().toLowerCase();
	if (pickTab.value === 'docs') {
		let list = documents.value.map((d) => ({ id: d.id, name: d.name }));
		if (q) list = list.filter((r) => r.name.toLowerCase().includes(q));
		return list;
	}
	if (pickTab.value === 'profiles') {
		let list = profiles.value.map((p) => ({ id: p.id, name: p.name }));
		if (q) list = list.filter((r) => r.name.toLowerCase().includes(q));
		return list;
	}
	let list = titlePageList.value.map((t) => ({ id: t.id, name: t.name }));
	if (q) list = list.filter((r) => r.name.toLowerCase().includes(q));
	return list;
});

function selectPickerRow(row: { id: string; name: string }) {
	if (pickTab.value === 'docs') picked.value = { kind: 'doc', id: row.id, name: row.name };
	else if (pickTab.value === 'profiles')
		picked.value = { kind: 'profile', id: row.id, name: row.name };
	else picked.value = { kind: 'titlePage', id: row.id, name: row.name };
}

function clearPick() {
	picked.value = null;
}

function onClose(v: boolean) {
	if (!v) {
		picked.value = null;
		pickerSearch.value = '';
	}
}

async function loadPickerData() {
	const [d, p, t] = await Promise.all([
		DocumentAPI.getAll(),
		ProfileAPI.getAll().catch(() => [] as Profile[]),
		TitlePageAPI.getAll().catch(() => [] as TitlePageRow[]),
	]);
	documents.value = d;
	profiles.value = p;
	titlePageList.value = t;
}

async function loadTitlePagesForDocument() {
	isLoadingTitlePages.value = true;
	try {
		titlePages.value = await TitlePageAPI.getAll();
	} catch {
		titlePages.value = [];
	} finally {
		isLoadingTitlePages.value = false;
	}
}

watch(
	() => props.modelValue,
	async (open) => {
		if (!open) return;
		includeDocument.value = true;
		includeStyleProfile.value = true;
		includeTitlePage.value = true;
		picked.value = null;
		pickerSearch.value = '';
		if (props.withEntityPicker) {
			await loadPickerData();
		}
		await loadTitlePagesForDocument();
		exportTitlePageId.value = props.initialTitlePageId || '';
	},
);

watch(
	() => props.initialTitlePageId,
	(v) => {
		if (props.modelValue && v) exportTitlePageId.value = v;
	},
);

function downloadFile(blob: Blob, filename: string) {
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}

async function downloadPdf() {
	const id = effectiveDocumentId.value;
	if (!id) return;
	isWorking.value = true;
	try {
		const blob = await DocumentAPI.generatePdf(id, {
			titlePageId: exportTitlePageId.value || undefined,
			includeDocument: includeDocument.value,
			includeStyleProfile: includeStyleProfile.value,
			includeTitlePage: includeTitlePage.value,
		});
		downloadFile(blob, `${effectiveDocumentName.value}.pdf`);
		isOpen.value = false;
	} catch (e) {
		console.error(e);
		alert('Не удалось сформировать PDF');
	} finally {
		isWorking.value = false;
	}
}

async function downloadProfileDdoc() {
	const id = picked.value?.id;
	if (!id) return;
	isWorking.value = true;
	try {
		const blob = await ProfileAPI.exportDdoc(id);
		const name = picked.value?.name ?? 'profile';
		downloadFile(blob, `${name}.ddoc`);
		isOpen.value = false;
	} catch (e) {
		console.error(e);
		alert('Не удалось скачать .ddoc');
	} finally {
		isWorking.value = false;
	}
}

async function downloadTitleDdoc() {
	const id = picked.value?.id;
	if (!id) return;
	isWorking.value = true;
	try {
		const blob = await TitlePageAPI.exportDdoc(id);
		const name = picked.value?.name ?? 'title';
		downloadFile(blob, `${name}.ddoc`);
		isOpen.value = false;
	} catch (e) {
		console.error(e);
		alert('Не удалось скачать .ddoc');
	} finally {
		isWorking.value = false;
	}
}
</script>

<style scoped>
.export-pdf-modal__picker {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md, 12px);
	max-height: 360px;
}
.export-pdf-modal__tabs {
	display: flex;
	gap: 8px;
}
.export-pdf-modal__tabs button {
	flex: 1;
	padding: 8px 10px;
	border: 1px solid var(--border-color, #333);
	background: var(--bg-secondary, #1a1a1a);
	color: inherit;
	border-radius: 6px;
	cursor: pointer;
}
.export-pdf-modal__tabs button.active {
	border-color: var(--accent, #3b82f6);
}
.export-pdf-modal__search {
	width: 100%;
	padding: 8px 10px;
	border-radius: 6px;
	border: 1px solid var(--border-color);
	background: var(--bg-primary);
	color: inherit;
}
.export-pdf-modal__list {
	list-style: none;
	margin: 0;
	padding: 0;
	overflow: auto;
	border: 1px solid var(--border-color);
	border-radius: 6px;
	max-height: 220px;
}
.export-pdf-modal__list-item {
	padding: 10px 12px;
	cursor: pointer;
	border-bottom: 1px solid var(--border-color);
}
.export-pdf-modal__list-item:hover {
	background: var(--bg-hover, rgba(255, 255, 255, 0.04));
}
.export-pdf-modal__list-item:last-child {
	border-bottom: none;
}
.export-pdf-modal__empty {
	padding: 16px;
	color: var(--text-muted, #888);
}
.export-pdf-modal__field {
	margin-bottom: var(--spacing-md, 12px);
}
.export-pdf-modal__label {
	display: block;
	margin-bottom: 6px;
	font-size: 13px;
	color: var(--text-muted, #999);
}
.export-pdf-modal__select {
	width: 100%;
	padding: 8px 10px;
	border-radius: 6px;
	border: 1px solid var(--border-color);
	background: var(--bg-primary);
	color: inherit;
}
.export-pdf-modal__checks {
	display: flex;
	flex-direction: column;
	gap: 8px;
}
.export-pdf-modal__check {
	display: flex;
	align-items: center;
	gap: 8px;
	cursor: pointer;
	font-size: 14px;
}
.export-pdf-modal__warn {
	color: #f59e0b;
	font-size: 13px;
	margin: 8px 0 0;
}
.export-pdf-modal__hint,
.export-pdf-modal__ddoc-only {
	margin: 0 0 8px;
}
.export-pdf-modal__sub {
	margin: 0;
	font-size: 13px;
	color: var(--text-muted, #999);
}
</style>
