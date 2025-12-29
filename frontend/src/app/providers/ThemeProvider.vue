<template>
  <slot />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useTheme } from '../composables/useTheme';

const { theme } = useTheme();

onMounted(() => {
  // Применяем тему при монтировании
  document.documentElement.setAttribute('data-theme', theme.value);
});
</script>
