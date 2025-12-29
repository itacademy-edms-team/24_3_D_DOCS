<template>
	<div class="attachments-page">
		<header class="attachments-header">
			<h2>Архив вложений</h2>
			<div class="controls">
				<select v-model="view">
					<option value="flat">Плоское</option>
					<option value="folders">Папки (по документам)</option>
				</select>
				<select v-model="filter">
					<option value="all">Все</option>
					<option value="image">Картинки</option>
					<option value="pdf">PDF</option>
				</select>
			</div>
		</header>

		<main class="attachments-body">
			<div v-if="isLoading">Загрузка...</div>
			<div v-else>
				<ul class="attachments-list">
					<li v-for="item in items" :key="item.id" class="attachment-item">
						<div class="meta">
							<span class="icon">{{ item.contentType.startsWith('image/') ? '🖼️' : '📄' }}</span>
							<span class="name">{{ item.fileName }}</span>
							<span class="date">{{ formatDate(item.updatedAt) }}</span>
						</div>
						<div class="actions">
							<button @click="download(item)">Скачать</button>
							<button @click="rename(item)">Переименовать</button>
							<button @click="remove(item)">Удалить</button>
						</div>
					</li>
				</ul>
				<div v-if="items.length === 0" class="empty">Нет вложений</div>
			</div>
		</main>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import AttachmentsAPI from '@/app/services/attachments';

const view = ref<'flat' | 'folders'>('flat');
const filter = ref<'all' | 'image' | 'pdf'>('all');
const items = ref<Array<any>>([]);
const isLoading = ref(false);

function formatDate(dateString: string) {
	const d = new Date(dateString);
	return d.toLocaleString();
}

async function load() {
	isLoading.value = true;
	try {
		const list = await AttachmentsAPI.list(filter.value === 'all' ? 'all' : filter.value);
		items.value = list || [];
	} catch (err) {
		console.error('Failed to load attachments', err);
		items.value = [];
	} finally {
		isLoading.value = false;
	}
}

async function download(item: any) {
	try {
		const blob = await AttachmentsAPI.downloadBlob(item.id);
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = item.fileName;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		URL.revokeObjectURL(url);
	} catch (err) {
		console.error('Download failed', err);
		alert('Не удалось скачать файл');
	}
}

async function rename(item: any) {
	const newName = prompt('Новое имя файла', item.fileName);
	if (!newName || newName === item.fileName) return;
	try {
		await AttachmentsAPI.rename(item.id, newName);
		item.fileName = newName;
	} catch (err) {
		console.error('Rename failed', err);
		alert('Не удалось переименовать файл');
	}
}

async function remove(item: any) {
	if (!confirm('Удалить вложение?')) return;
	try {
		await AttachmentsAPI.delete(item.id);
		await load();
	} catch (err) {
		console.error('Delete failed', err);
		alert('Не удалось удалить');
	}
}

watch([filter, view], () => {
	load();
});

onMounted(() => {
	load();
});
</script>

<style scoped>
.attachments-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 1rem;
	border-bottom: 1px solid var(--border-color);
}
.attachments-body {
	padding: 1rem;
}
.attachments-list {
	list-style: none;
	padding: 0;
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}
.attachment-item {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 0.5rem;
	border: 1px solid var(--border-color);
	border-radius: 6px;
	background: var(--bg-secondary);
}
.attachment-item .meta {
	display: flex;
	gap: 0.75rem;
	align-items: center;
}
.attachment-item .actions button {
	margin-left: 0.5rem;
}
.empty {
	padding: 2rem;
	text-align: center;
	color: var(--text-tertiary);
}
</style>

