<template>
	<div class="title-page-editor">
		<div
			v-if="loading"
			class="loading"
		>
			Загрузка...
		</div>

		<div
			v-else-if="!titlePage"
			class="not-found"
		>
			Титульный лист не найден
		</div>

		<div
			v-else
			class="editor-layout"
		>
			<!-- Header -->
			<div class="header">
				<div class="header-left">
					<button
						class="back-btn"
						@click="router.push('/')"
					>
						← Назад
					</button>
					<input
						v-model="titlePage.name"
						class="name-input"
						@input="handleNameChange(titlePage.name)"
					/>
				</div>
				<button
					class="save-btn"
					:disabled="saving"
					@click="handleSave"
				>
					{{ saving ? 'Сохранение...' : 'Сохранить' }}
				</button>
			</div>

			<!-- Main content -->
			<div class="content">
				<!-- Canvas area -->
				<div class="canvas-area">
					<TitlePageCanvas
						:elements="titlePage.elements"
						:selected-element-ids="selectedElementIds"
						:initial-tool="currentTool"
						@element-select="handleElementSelect"
						@element-toggle="handleElementToggle"
						@element-move="handleElementMove"
						@elements-move="handleElementsMove"
						@element-add="handleElementAdd"
						@move-end="handleSave"
						@tool-change="handleToolChange"
						@grid-toggle="() => {}"
					/>
				</div>

				<!-- Sidebar -->
				<div class="sidebar">
					<ElementEditor
						:element="selectedElement"
						@update="handleElementUpdate"
						@delete="handleElementDelete"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import TitlePageCanvas from '@/widgets/title-page/TitlePageCanvas.vue';
import ElementEditor from '@/widgets/title-page/ElementEditor.vue';
import { useTitlePageEditor } from '@/widgets/title-page/useTitlePageEditor';

const router = useRouter();

const {
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
} = useTitlePageEditor();

async function handleSave() {
	try {
		await saveTitlePage();
	} catch (error) {
		console.error('Failed to save:', error);
		alert('Ошибка при сохранении: ' + (error instanceof Error ? error.message : String(error)));
	}
}

onMounted(() => {
	loadTitlePage();
});
</script>

<style scoped>
.title-page-editor {
	height: 100vh;
	display: flex;
	flex-direction: column;
	background: #0a0a0a;
	color: #e4e4e7;
	overflow: hidden;
}

.loading,
.not-found {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	color: #e4e4e7;
}

.editor-layout {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 1.5rem 2rem;
	border-bottom: 1px solid #27272a;
	background: #0a0a0a;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.name-input {
	font-size: 1.25rem;
	font-weight: 600;
	border: none;
	outline: none;
	padding: 0.5rem;
	background: transparent;
	color: #e4e4e7;
	flex: 1;
	min-width: 200px;
}

.name-input::placeholder {
	color: #71717a;
}

.header-actions {
	display: flex;
	gap: 0.5rem;
}

.save-btn,
.back-btn {
	padding: 0.5rem 1rem;
	border: 1px solid #27272a;
	border-radius: 6px;
	cursor: pointer;
	font-size: 14px;
	font-weight: 600;
	transition: all 0.2s;
	font-family: inherit;
}

.back-btn {
	background: #18181b;
	color: #e4e4e7;
}

.back-btn:hover {
	background: #27272a;
}

.save-btn {
	background: #6366f1;
	color: #fff;
	border-color: #6366f1;
}

.save-btn:hover:not(:disabled) {
	background: #818cf8;
}

.save-btn:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

.content {
	display: grid;
	grid-template-columns: 1fr 350px;
	gap: 0;
	flex: 1;
	overflow: hidden;
	background: #0a0a0a;
}

.canvas-area {
	overflow: auto;
	background: #0a0a0a;
	padding: 1rem;
}

.sidebar {
	overflow-y: auto;
	background: #09090b;
	border-left: 1px solid #27272a;
	padding: 1rem;
}
</style>
