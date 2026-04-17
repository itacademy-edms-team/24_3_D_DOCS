<script setup lang="ts">
import { ref } from 'vue';
import { DropdownToolbar } from 'md-editor-v3';
import type { Insert } from 'md-editor-v3';

const props = defineProps<{
  insert?: Insert;
}>();

const emit = defineEmits<{
  openMathLive: [];
}>();

const visible = ref(false);

function onChange(nextVisible: boolean) {
  visible.value = nextVisible;
}

function insertInline() {
  props.insert?.((selectedText) => ({
    targetValue: `$${selectedText || 'formula'}$`,
    select: true,
    deviationStart: 1,
    deviationEnd: -1,
  }));
  visible.value = false;
}

function insertBlock() {
  props.insert?.((selectedText) => ({
    targetValue: `$$\n${selectedText || 'formula'}\n$$`,
    select: true,
    deviationStart: 3,
    deviationEnd: -3,
  }));
  visible.value = false;
}

function openMathLiveBuilder() {
  emit('openMathLive');
  visible.value = false;
}
</script>

<template>
  <DropdownToolbar title="Math" :visible="visible" :onChange="onChange">
    <template #overlay>
      <div class="math-toolbar-overlay">
        <button type="button" class="math-toolbar-item" @click="insertInline">
          inline
        </button>
        <button type="button" class="math-toolbar-item" @click="insertBlock">
          block
        </button>
        <button type="button" class="math-toolbar-item" @click="openMathLiveBuilder">
          MathLive
        </button>
      </div>
    </template>
    <span class="md-editor-icon">∑</span>
  </DropdownToolbar>
</template>

<style scoped>
.math-toolbar-overlay {
  min-width: 130px;
  padding: 4px;
}

.math-toolbar-item {
  display: block;
  width: 100%;
  border: none;
  border-radius: 4px;
  background: transparent;
  padding: 6px 8px;
  text-align: left;
  color: inherit;
  cursor: pointer;
}

.math-toolbar-item:hover {
  background: rgb(63 63 70 / 0.7);
}
</style>
