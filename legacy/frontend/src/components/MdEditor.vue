<script setup lang="ts">
import { MdEditor as MdEditorComponent } from 'md-editor-v3';
import type { ToolbarNames } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';
import { computed, ref } from 'vue';
import { useEditorStore } from '../stores/editor';
import { API_BASE } from '../config';
import MdEditorMathToolbar from './MdEditorMathToolbar.vue';
import MathLiveBuilderModal from './MathLiveBuilderModal.vue';

const props = defineProps<{
  modelValue?: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

const editorStore = useEditorStore();
const content = computed({
  get: () => props.modelValue ?? editorStore.content,
  set: (v) => {
    emit('update:modelValue', v);
    editorStore.setContent(v);
  },
});

const editorRef = ref<{ insert: (fn: (s: string) => { targetValue: string }) => void } | null>(null);
const mathBuilderVisible = ref(false);
const toolbars: ToolbarNames[] = [
  'bold',
  'underline',
  'italic',
  '-',
  'strikeThrough',
  'title',
  'sub',
  'sup',
  'quote',
  'unorderedList',
  'orderedList',
  '-',
  'codeRow',
  'code',
  'link',
  'image',
  'table',
  'mermaid',
  0,
  '-',
  'revoke',
  'next',
  'save',
  '=',
  'pageFullscreen',
  'fullscreen',
  'preview',
  'htmlPreview',
  'catalog',
  'github',
];

function insertAtCursor(text: string) {
  editorRef.value?.insert(() => ({ targetValue: text }));
}

function handleMathInsert(value: string) {
  insertAtCursor(value);
  mathBuilderVisible.value = false;
}

/** Upload images via POST /upload-image; call callBack with URLs /image-assets/{id}/file (md-editor-v3 contract). */
function onUploadImg(
  files: File[],
  callBack: (urls: string[]) => void
): void {
  const urls: string[] = [];
  let done = 0;
  const total = files.length;
  if (total === 0) {
    callBack(urls);
    return;
  }
  function maybeDone() {
    done += 1;
    if (done === total) callBack(urls);
  }
  for (const file of files) {
    const formData = new FormData();
    formData.append('file', file);
    fetch(`${API_BASE}/upload-image`, { method: 'POST', body: formData })
      .then((res) => {
        if (!res.ok) return res.json().then((err: { detail?: string }) => { throw new Error(typeof err.detail === 'string' ? err.detail : 'Upload failed'); });
        return res.json();
      })
      .then((data: { id?: number }) => {
        if (data.id != null) urls.push(`${API_BASE}/image-assets/${data.id}/file`);
        maybeDone();
      })
      .catch((e) => {
        console.error('Image upload failed:', e);
        maybeDone();
      }); // maybeDone in both then and catch so callBack is always called
  }
}

function handleDrop(e: DragEvent) {
  e.preventDefault();
  const files = e.dataTransfer?.files;
  if (!files?.length) return;
  const file = files[0];
  if (!file || !file.name.match(/\.(md|txt)$/i)) return;
  const reader = new FileReader();
  reader.onload = () => {
    const text = reader.result as string;
    content.value = text;
    editorStore.setCurrentFileName(file.name);
  };
  reader.readAsText(file);
}

function handleDragOver(e: DragEvent) {
  e.preventDefault();
  (e as any).dataTransfer.dropEffect = 'copy';
}

defineExpose({
  insertAtCursor,
});

// --- Crop flow: file input → crop modal → upload → insert
const cropModalOpen = ref(false);
const cropFile = ref<File | null>(null);
const cropImageUrl = ref('');
const cropInputRef = ref<HTMLInputElement | null>(null);
const cropCanvasRef = ref<HTMLCanvasElement | null>(null);
const cropPreviewRef = ref<HTMLImageElement | null>(null);
const showSourceEditor = ref(true);
let cropImage: HTMLImageElement | null = null;
let cropX = 0;
let cropY = 0;
let cropW = 100;
let cropH = 100;

function openCropInput() {
  cropInputRef.value?.click();
}

function onCropFileSelected(e: Event) {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file || !file.type.startsWith('image/')) return;
  cropFile.value = file;
  cropImageUrl.value = URL.createObjectURL(file);
  cropModalOpen.value = true;
}

function closeCropModal() {
  if (cropImageUrl.value) URL.revokeObjectURL(cropImageUrl.value);
  cropImageUrl.value = '';
  cropFile.value = null;
  cropModalOpen.value = false;
}

function drawCropPreview() {
  const img = cropPreviewRef.value;
  const canvas = cropCanvasRef.value;
  if (!img || !canvas || !img.complete) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const scale = Math.min(200 / img.naturalWidth, 200 / img.naturalHeight);
  const w = Math.round(img.naturalWidth * scale);
  const h = Math.round(img.naturalHeight * scale);
  canvas.width = w;
  canvas.height = h;
  cropX = Math.max(0, Math.min(cropX, w - cropW));
  cropY = Math.max(0, Math.min(cropY, h - cropH));
  cropW = Math.min(cropW, w);
  cropH = Math.min(cropH, h);
  ctx.drawImage(img, 0, 0, w, h);
  ctx.strokeStyle = '#22c55e';
  ctx.lineWidth = 2;
  ctx.strokeRect(cropX, cropY, cropW, cropH);
}

function onCropImageLoad() {
  const img = cropPreviewRef.value;
  if (!img) return;
  cropImage = img;
  const scale = Math.min(200 / img.naturalWidth, 200 / img.naturalHeight);
  const w = Math.round(img.naturalWidth * scale);
  const h = Math.round(img.naturalHeight * scale);
  cropW = Math.min(150, w);
  cropH = Math.min(150, h);
  cropX = (w - cropW) / 2;
  cropY = (h - cropH) / 2;
  drawCropPreview();
}

async function applyCrop() {
  const img = cropImage;
  const file = cropFile.value;
  if (!img || !file) {
    closeCropModal();
    return;
  }
  const scale = Math.min(200 / img.naturalWidth, 200 / img.naturalHeight);
  const srcX = Math.round(cropX / scale);
  const srcY = Math.round(cropY / scale);
  const srcCropW = Math.round(cropW / scale);
  const srcCropH = Math.round(cropH / scale);
  const outCanvas = document.createElement('canvas');
  outCanvas.width = srcCropW;
  outCanvas.height = srcCropH;
  const ctx = outCanvas.getContext('2d');
  if (!ctx) {
    closeCropModal();
    return;
  }
  ctx.drawImage(img, srcX, srcY, srcCropW, srcCropH, 0, 0, srcCropW, srcCropH);
  outCanvas.toBlob(
    async (blob) => {
      if (!blob) {
        closeCropModal();
        return;
      }
      const ext = file.name.split('.').pop()?.toLowerCase() || 'png';
      const croppedFile = new File([blob], `cropped.${ext}`, { type: blob.type });
      const formData = new FormData();
      formData.append('file', croppedFile);
      try {
        const res = await fetch(`${API_BASE}/upload-image`, { method: 'POST', body: formData });
        if (!res.ok) throw new Error('Upload failed');
        const data = await res.json();
        if (data.id != null) insertAtCursor(`![](${API_BASE}/image-assets/${data.id}/file)`);
      } catch (e) {
        console.error('Crop upload failed:', e);
      }
      closeCropModal();
    },
    file.type || 'image/png',
    0.92
  );
}
</script>

<template>
  <div
    class="md-editor-wrapper h-full flex flex-col min-h-0 flex-1"
    @drop="handleDrop"
    @dragover="handleDragOver"
  >
    <input
      ref="cropInputRef"
      type="file"
      accept="image/png,image/jpeg,image/gif,image/webp"
      class="hidden"
      @change="onCropFileSelected"
    />
    <div class="flex items-center gap-2 px-2 py-1 border-b border-zinc-700 bg-zinc-800/50">
      <button
        type="button"
        class="text-xs text-zinc-400 hover:text-zinc-200 px-2 py-1 rounded"
        title="Обрезать изображение"
        @click="openCropInput"
      >
        Обрезать
      </button>
      <button
        type="button"
        class="text-xs text-zinc-400 hover:text-zinc-200 px-2 py-1 rounded"
        :aria-pressed="showSourceEditor"
        @click="showSourceEditor = !showSourceEditor"
      >
        {{ showSourceEditor ? 'Скрыть исходник' : 'Показать исходник' }}
      </button>
    </div>
    
    <div
      v-show="showSourceEditor"
      class="flex-1 min-h-0 overflow-hidden"
    >
      <MdEditorComponent
        ref="editorRef"
        v-model="content"
        :toolbars="toolbars"
        theme="dark"
        :preview="false"
        language="ru-RU"
        placeholder="Введите Markdown..."
        :no-upload-img="false"
        :on-upload-img="onUploadImg"
      >
        <template #defToolbars>
          <MdEditorMathToolbar @open-math-live="mathBuilderVisible = true" />
        </template>
      </MdEditorComponent>
    </div>
    <MathLiveBuilderModal
      :visible="mathBuilderVisible"
      @close="mathBuilderVisible = false"
      @insert="handleMathInsert"
    />
    <Teleport to="body">
      <div
        v-if="cropModalOpen"
        class="fixed inset-0 z-[100] flex items-center justify-center bg-black/70"
        @click.self="closeCropModal"
      >
        <div class="bg-zinc-800 rounded-lg p-4 shadow-xl max-w-lg w-full mx-4">
          <h3 class="text-zinc-100 font-medium mb-3">Обрезать изображение</h3>
          <div class="flex gap-4 items-start">
            <img
              ref="cropPreviewRef"
              :src="cropImageUrl"
              class="max-w-[200px] max-h-[200px] object-contain hidden"
              @load="onCropImageLoad"
            />
            <canvas
              ref="cropCanvasRef"
              class="border border-zinc-600 rounded bg-zinc-900"
              width="200"
              height="200"
            />
          </div>
          <div class="flex justify-end gap-2 mt-4">
            <button
              type="button"
              class="px-3 py-1.5 rounded text-zinc-400 hover:bg-zinc-700"
              @click="closeCropModal"
            >
              Отмена
            </button>
            <button
              type="button"
              class="px-3 py-1.5 rounded bg-emerald-600 hover:bg-emerald-500 text-white"
              @click="applyCrop"
            >
              Вставить
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.md-editor-wrapper :deep(.md-editor) {
  flex: 1;
  min-height: 0;
  height: 100% !important;
}
</style>
