import { defineConfig } from '@rsbuild/core';
import { pluginReact } from '@rsbuild/plugin-react';
import {
  pluginCssMinimizer,
  CssMinimizerWebpackPlugin,
} from '@rsbuild/plugin-css-minimizer';

export default defineConfig({
	plugins: [
		pluginReact(),
		pluginCssMinimizer({
      pluginOptions: {
        minify: CssMinimizerWebpackPlugin.lightningCssMinify,
      },
    }),
	],

	source: {
		define: {
			BASE_URI: JSON.stringify(process.env.BASE_URI),
		}
	},

	resolve: {
		alias: {
			'@': './src',
			'@app': './src/app',
			'@entities': './src/entities',
			'@features': './src/features',
			'@widgets': './src/widgets',
			'@ui': './src/shared/ui',
			'@store': './src/shared/lib/store',
			'@api': './src/shared/api/HttpClient.ts',
			'@model': './src/shared/model',
			'@bem': './src/shared/lib/bem/cn.ts',
		}
	},

	output: {
		minify: true,
	},

	server: {
		port: process.env.PORT ? Number(process.env.PORT) : 8080,
		// Proxy настраивается только для dev-сервера
		// В production nginx проксирует запросы
	},
});
