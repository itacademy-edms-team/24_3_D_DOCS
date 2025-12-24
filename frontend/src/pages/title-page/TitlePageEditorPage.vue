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
				<input
					v-model="titlePage.name"
					class="name-input"
					@input="handleNameChange(titlePage.name)"
				/>
				<div class="header-actions">
					<button
						class="save-btn"
						:disabled="saving"
						@click="handleSave"
					>
						{{ saving ? 'Сохранение...' : 'Сохранить' }}
					</button>
					<button
						class="back-btn"
						@click="router.push('/')"
					>
						Назад
					</button>
				</div>
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
	padding: 1rem;
	height: 100vh;
	display: flex;
	flex-direction: column;
}

.loading,
.not-found {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
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
	margin-bottom: 1rem;
	padding: 1rem;
	border-bottom: 1px solid #ddd;
}

.name-input {
	font-size: 1.25rem;
	font-weight: 600;
	border: none;
	outline: none;
	padding: 0.5rem;
}

.header-actions {
	display: flex;
	gap: 0.5rem;
}

.save-btn,
.back-btn {
	padding: 0.5rem 1rem;
	border: 1px solid #ddd;
	border-radius: 4px;
	cursor: pointer;
	background: #f0f0f0;
}

.save-btn {
	background: #0066ff;
	color: #fff;
	border-color: #0066ff;
}

.save-btn:disabled {
	opacity: 0.6;
	cursor: not-allowed;
}

.content {
	display: grid;
	grid-template-columns: 1fr 350px;
	gap: 1.5rem;
	flex: 1;
	overflow: hidden;
}

.canvas-area {
	overflow: auto;
}

.sidebar {
	overflow-y: auto;
}
</style>
