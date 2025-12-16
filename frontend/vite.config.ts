import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
	plugins: [vue()],
	resolve: {
		alias: {
			'@': fileURLToPath(new URL('./src', import.meta.url)),
		},
	},
	define: {
		BASE_URI: JSON.stringify(process.env.BASE_URI || 'http://localhost:5159'),
	},
	server: {
		port: process.env.PORT ? Number(process.env.PORT) : 8080,
	},
});
