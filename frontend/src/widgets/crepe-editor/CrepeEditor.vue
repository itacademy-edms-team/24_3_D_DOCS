<template>
	<div class="crepe-editor crepe-editor--minimal-ui">
		<div ref="rootRef" class="crepe-editor__root" />
	</div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import { Crepe, CrepeFeature } from '@milkdown/crepe';
import 'katex/dist/katex.min.css';
import UploadAPI from '@/shared/api/UploadAPI';
import { useTheme } from '@/app/composables/useTheme';

/** Явно совпадает с ключами defaultFeatures в @milkdown/crepe (строки «toolbar», «link-tooltip», …) */
const CREPE_UI_FLAGS: Partial<Record<CrepeFeature, boolean>> = {
	[CrepeFeature.TopBar]: false,
	[CrepeFeature.Toolbar]: false,
	[CrepeFeature.BlockEdit]: false,
	[CrepeFeature.LinkTooltip]: false,
	[CrepeFeature.Latex]: false,
};

const props = defineProps<{
	modelValue: string;
	documentId?: string;
}>();

const emit = defineEmits<{
	'update:modelValue': [string];
}>();

const { isDark } = useTheme();
const rootRef = ref<HTMLElement | null>(null);
let crepe: InstanceType<typeof Crepe> | null = null;

const emitDebounced = useDebounceFn((md: string) => {
	emit('update:modelValue', md);
}, 400);

function emitImmediate(md: string) {
	emitDebounced.cancel();
	emit('update:modelValue', md);
}

function getMarkdownOrEmpty(): string {
	if (!crepe) {
		return props.modelValue;
	}
	return crepe.getMarkdown();
}

async function loadCrepeTheme() {
	if (isDark.value) {
		await import('@milkdown/crepe/theme/classic-dark.css');
	} else {
		await import('@milkdown/crepe/theme/classic.css');
	}
}

async function mountEditor() {
	await nextTick();
	const el = rootRef.value;
	if (!el) {
		return;
	}

	destroyEditor();
	await loadCrepeTheme();

	const c = new Crepe({
		root: el,
		defaultValue: props.modelValue,
		features: { ...CREPE_UI_FLAGS },
		featureConfigs: {
			[CrepeFeature.ImageBlock]: {
				onUpload: async (file: File) => {
					if (!props.documentId) {
						return URL.createObjectURL(file);
					}
					const { url } = await UploadAPI.uploadAsset(props.documentId, file);
					return url;
				},
			},
		},
	});

	c.on((listener) => {
		listener.markdownUpdated((_, markdown) => {
			emitDebounced(markdown);
		});
	});

	await c.create();
	crepe = c;
}

function destroyEditor() {
	if (crepe) {
		crepe.destroy();
		crepe = null;
	}
}

onMounted(() => {
	void mountEditor();
});

watch(
	() => isDark.value,
	() => {
		void mountEditor();
	}
);

onBeforeUnmount(() => {
	emitImmediate(getMarkdownOrEmpty());
	destroyEditor();
});
</script>

<style scoped>
.crepe-editor {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	min-width: 0;
}

.crepe-editor__root {
	flex: 1;
	min-height: 0;
	width: 100%;
	max-width: 100%;
	min-width: 0;
	overflow: auto;
}

/* Макет: Crepe .milkdown внутри flex-панели */
.crepe-editor :deep(.milkdown) {
	width: 100%;
	max-width: 100%;
	min-width: 0;
	box-sizing: border-box;
}

.crepe-editor :deep(.milkdown .ProseMirror) {
	outline: none;
	max-width: 100%;
	min-width: 0;
	overflow-wrap: break-word;
	word-wrap: break-word;
}
</style>

/* Не scoped: !important и селекторы как в crepe theme (там .tools, не .tool) */
<style>
.crepe-editor--minimal-ui .milkdown-toolbar,
.crepe-editor--minimal-ui .milkdown-block-handle,
.crepe-editor--minimal-ui .milkdown-link-edit,
.crepe-editor--minimal-ui .milkdown-latex-inline-edit,
.crepe-editor--minimal-ui .milkdown-code-block .tools,
.crepe-editor--minimal-ui .milkdown-code-block .language-picker,
.crepe-editor--minimal-ui .milkdown-code-block .list-wrapper {
	display: none !important;
}

.crepe-editor--minimal-ui .milkdown .milkdown-code-block {
	padding: 4px 12px 12px;
	margin: 4px 0;
}

.crepe-editor--minimal-ui .milkdown .milkdown-code-block .cm-editor,
.crepe-editor--minimal-ui .milkdown .milkdown-code-block .cm-scroller {
	max-width: 100%;
}
</style>
