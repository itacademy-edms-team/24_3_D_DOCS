import { ref, watch, onMounted } from 'vue';
import { useStorage } from '@vueuse/core';

type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'ddocs-theme';

export function useTheme() {
  const theme = useStorage<Theme>(THEME_STORAGE_KEY, 'dark');
  const isDark = ref(theme.value === 'dark');

  const setTheme = (newTheme: Theme) => {
    theme.value = newTheme;
    isDark.value = newTheme === 'dark';
    updateDocumentTheme(newTheme);
  };

  const toggleTheme = () => {
    const newTheme = theme.value === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
  };

  const updateDocumentTheme = (newTheme: Theme) => {
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', newTheme);
    }
  };

  watch(theme, (newTheme) => {
    updateDocumentTheme(newTheme);
  });

  onMounted(() => {
    updateDocumentTheme(theme.value);
  });

  return {
    theme,
    isDark,
    setTheme,
    toggleTheme,
  };
}
