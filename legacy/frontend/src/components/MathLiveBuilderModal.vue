<script setup lang="ts">
import 'mathlive';
import 'mathlive/static.css';
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = defineProps<{
  visible: boolean;
}>();

const emit = defineEmits<{
  close: [];
  insert: [value: string];
}>();

const latex = ref('');
const displayMode = ref(false);
const feedback = ref('');
const mathFieldRef = ref<MathfieldElement | null>(null);
let feedbackTimer: number | null = null;

const wrappedLatex = computed(() => {
  const value = latex.value.trim();
  if (!value) return '';
  return displayMode.value ? `$$${value}$$` : `$${value}$`;
});

function getVirtualKeyboard() {
  return window.mathVirtualKeyboard;
}

function close() {
  emit('close');
}

function handleMathInput(event: Event) {
  const field = event.target as MathfieldElement;
  latex.value = field.value;
}

async function copyLatex() {
  if (!wrappedLatex.value) return;
  await navigator.clipboard.writeText(wrappedLatex.value);
  feedback.value = 'Скопировано';
  if (feedbackTimer) window.clearTimeout(feedbackTimer);
  feedbackTimer = window.setTimeout(() => {
    feedback.value = '';
  }, 1200);
}

function insertLatex() {
  if (!wrappedLatex.value) return;
  emit('insert', wrappedLatex.value);
}

function onWindowKeydown(event: KeyboardEvent) {
  if (!props.visible) return;
  if (event.key === 'Escape') close();
}

watch(
  () => props.visible,
  async (next) => {
    if (!next) {
      feedback.value = '';
      if (feedbackTimer) {
        window.clearTimeout(feedbackTimer);
        feedbackTimer = null;
      }
      return;
    }
    await nextTick();
    const field = mathFieldRef.value;
    if (!field) return;
    field.focus();
    requestAnimationFrame(() => {
      getVirtualKeyboard()?.show();
    });
  }
);

onMounted(() => {
  window.addEventListener('keydown', onWindowKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onWindowKeydown);
  getVirtualKeyboard()?.hide();
  if (feedbackTimer) window.clearTimeout(feedbackTimer);
});
</script>

<template>
  <Teleport to="body">
    <div
      v-if="visible"
      class="pointer-events-none fixed inset-0 z-[1200] flex items-center justify-center p-4"
    >
      <div class="pointer-events-auto w-full max-w-2xl rounded-lg border border-zinc-700 bg-zinc-900 p-4 shadow-xl">
        <div class="mb-3 flex items-center justify-between">
          <h3 class="text-sm font-medium text-zinc-100">Конструктор формулы</h3>
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-zinc-400 hover:bg-zinc-800 hover:text-zinc-100"
            @click="close"
          >
            Закрыть
          </button>
        </div>
        <div class="space-y-3">
          <math-field
            ref="mathFieldRef"
            class="w-full min-h-14 rounded border border-zinc-700 bg-zinc-950 px-2 py-2 text-zinc-100"
            virtual-keyboard-mode="manual"
            @input="handleMathInput"
          />
          <div class="flex items-center justify-between gap-3">
            <label class="flex items-center gap-2 text-xs text-zinc-300">
              <input
                v-model="displayMode"
                type="checkbox"
                class="rounded border-zinc-600 bg-zinc-900 text-emerald-500"
              />
              Копировать как block ($$...$$)
            </label>
            <button
              type="button"
              class="rounded bg-emerald-600 px-3 py-1.5 text-sm text-white hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-60"
              :disabled="!wrappedLatex"
              @click="copyLatex"
            >
              Скопировать LaTeX
            </button>
            <button
              type="button"
              class="rounded bg-indigo-600 px-3 py-1.5 text-sm text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-60"
              :disabled="!wrappedLatex"
              @click="insertLatex"
            >
              Вставить
            </button>
          </div>
          <p class="min-h-5 text-xs text-emerald-400">{{ feedback }}</p>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
:global(math-virtual-keyboard) {
  z-index: 1400 !important;
}
</style>
