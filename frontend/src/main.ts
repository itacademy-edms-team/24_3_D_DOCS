import { createApp } from 'vue';
import { createPinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate';
import App from './App.vue';
import router from './router';
import ThemeProvider from './app/providers/ThemeProvider.vue';
import './styles/global.css';
import './styles/chat-ui.css';
import './app/config/markdownEditor';

const app = createApp(App);
const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);

app.use(pinia);
app.use(router);

// Инициализируем auth store сразу после создания pinia, чтобы данные восстановились из localStorage
import { useAuthStore } from './entities/auth/store/authStore';
const authStore = useAuthStore();

app.mount('#app');
