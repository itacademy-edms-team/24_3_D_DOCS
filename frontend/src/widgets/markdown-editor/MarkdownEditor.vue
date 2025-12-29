<template>
	<div ref="editorContainerRef" class="markdown-editor">
		<MdEditor
			:id="editorId"
			:modelValue="localContent"
			:theme="editorTheme"
			:preview="false"
			:toolbars="toolbars"
			:toolbarsExclude="excludedToolbars"
			:defToolbars="defToolbars"
			:onUploadImg="handleUploadImg"
			language="en-US"
			@onChange="handleChange"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, h, onMounted, onUnmounted, nextTick } from 'vue';
import { MdEditor } from 'md-editor-v3';
import type { UploadImgEvent } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';
import { useTheme } from '@/app/composables/useTheme';
import { useMarkdownEditorShortcuts } from '@/app/composables/useMarkdownEditorShortcuts';
import { useDebounceFn, useThrottleFn } from '@vueuse/core';
import UploadAPI from '@/shared/api/UploadAPI';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import HighlightToolbarButton from './components/HighlightToolbarButton.vue';
import CaptionToolbarButton from './components/CaptionToolbarButton.vue';
import DownloadPdfToolbarButton from './components/DownloadPdfToolbarButton.vue';
import type { EmbeddingStatus } from '@/entities/document/types';

interface DiffChange {
	lineNumber: number; // 0-based
	type: 'added' | 'deleted';
	timestamp: number;
	stepNumber: number;
	operation: 'insert' | 'update' | 'delete';
	originalText?: string;
	newText?: string;
	status: 'pending' | 'applied' | 'rejected';
}

interface Props {
	modelValue: string;
	documentId?: string;
	diffChanges?: DiffChange[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: string];
}>();

const { theme } = useTheme();
const editorId = 'markdown-editor';
const editorContainerRef = ref<HTMLElement>();
const localContent = ref(props.modelValue);

const editorTheme = computed(() => {
	return theme.value === 'dark' ? 'dark' : 'light';
});

const excludedToolbars = ['github', 'preview', 'previewOnly', 'htmlPreview'];

// Toolbars array with custom buttons
const toolbars = computed(() => [
	0, // Index for highlight button in defToolbars
	'bold',
	'underline',
	'italic',
	'strikeThrough',
	'-',
	'title',
	'sub',
	'sup',
	'quote',
	'unorderedList',
	'orderedList',
	'task',
	'-',
	'codeRow',
	'code',
	'link',
	'image',
	1, // Index for caption button in defToolbars
	'table',
	'-',
	2, // Index for download PDF button in defToolbars
	'revoke',
	'next',
]);

// Custom toolbars - md-editor-v3 expects a VNode or array of VNodes
const defToolbars = computed(() => [
	h(HighlightToolbarButton),
	h(CaptionToolbarButton),
	h(DownloadPdfToolbarButton, { documentId: props.documentId }),
]);

// Handle image upload
const handleUploadImg: UploadImgEvent = async (files, callBack) => {
	if (!props.documentId) {
		console.error('Document ID is required for image upload');
		callBack([]);
		return;
	}

	try {
		const uploadPromises = Array.from(files).map((file) =>
			UploadAPI.uploadAsset(props.documentId!, file)
		);

		const results = await Promise.all(uploadPromises);
		const urls = results.map((result) => result.url);
		callBack(urls);
	} catch (error) {
		console.error('Failed to upload images:', error);
		callBack([]);
	}
};

// Setup keyboard shortcuts with layout-independent key detection
useMarkdownEditorShortcuts({
	editorElementRef: editorContainerRef,
	onContentChange: (newContent: string) => {
		localContent.value = newContent;
		emit('update:modelValue', newContent);
	},
});

watch(
	() => props.modelValue,
	(newValue) => {
		if (localContent.value !== newValue) {
			localContent.value = newValue;
		}
	},
);

const handleChange = (value: string) => {
	localContent.value = value;
	emit('update:modelValue', value);
};

// Embedding status
const embeddingStatus = ref<EmbeddingStatus | null>(null);
const isLoadingStatus = ref(false);
const lastContentHash = ref<string>('');
const autoUpdateInterval = ref<ReturnType<typeof setInterval> | null>(null);
const observerRef = ref<MutationObserver | null>(null);
const editorListenersRef = ref<Array<{ element: HTMLElement; event: string; handler: EventListener }>>([]);
const focusTimeoutRef = ref<ReturnType<typeof setTimeout> | null>(null);

// Simple hash function for content change detection
const computeContentHash = async (content: string): Promise<string> => {
	const encoder = new TextEncoder();
	const data = encoder.encode(content);
	const hashBuffer = await crypto.subtle.digest('SHA-256', data);
	const hashArray = Array.from(new Uint8Array(hashBuffer));
	return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
};

const updateEmbeddingsIfNeeded = async () => {
	if (!props.documentId || !localContent.value) return;

	try {
		const currentHash = await computeContentHash(localContent.value);
		if (currentHash !== lastContentHash.value) {
			lastContentHash.value = currentHash;
			// Silently update embeddings in background
			await DocumentAPI.updateEmbeddings(props.documentId);
		}
	} catch (error) {
		console.error('Failed to auto-update embeddings:', error);
		// Don't throw - this is a background operation
	}
};

const loadEmbeddingStatus = async () => {
	if (!props.documentId || !localContent.value) {
		embeddingStatus.value = null;
		updateGutterStyles();
		return;
	}

	isLoadingStatus.value = true;
	try {
		const status = await DocumentAPI.getEmbeddingStatus(props.documentId);
		embeddingStatus.value = status;
		
		// Debug: log status for troubleshooting
		if (status.lineStatuses.length > 0) {
			const sampleLines = [0, 2, 20].filter((i) => i < status.lineStatuses.length);
			console.log('Embedding status loaded:', {
				coveragePercentage: status.coveragePercentage.toFixed(1) + '%',
				totalLines: status.totalLines,
				coveredLines: status.coveredLines,
				sampleStatuses: sampleLines.map((i) => {
					const s = status.lineStatuses[i];
					return {
						lineNumber: s.lineNumber,
						isCovered: s.isCovered,
						isEmpty: s.isEmpty,
						content: localContent.value.split('\n')[s.lineNumber]?.substring(0, 50) || '',
					};
				}),
			});
		}
		
		updateGutterStyles();
	} catch (error) {
		console.error('Failed to load embedding status:', error);
		embeddingStatus.value = null;
		updateGutterStyles();
	} finally {
		isLoadingStatus.value = false;
	}
};

// Update gutter element styles based on embedding status and diff changes
// Throttled to prevent excessive updates
const updateGutterStyles = useThrottleFn(() => {
	nextTick(() => {
		if (!editorContainerRef.value) return;

		const gutterElements = editorContainerRef.value.querySelectorAll<HTMLElement>(
			'.cm-gutterElement'
		);

		// Build diff changes map (only pending changes)
		const diffChangesMap = new Map<number, DiffChange>();
		if (props.diffChanges) {
			props.diffChanges
				.filter(c => c.status === 'pending')
				.forEach((change) => {
					diffChangesMap.set(change.lineNumber, change);
				});
		}

		if (!embeddingStatus.value) {
			// Clear all styles if no status, but keep diff styles
			gutterElements.forEach((el, index) => {
				const lineNumber = index - 1;
				const diffChange = diffChangesMap.get(lineNumber);
				
			// Remove existing diff classes
			el.classList.remove('diff-added-gutter', 'diff-deleted-gutter');
			
			if (diffChange) {
				// Apply diff color to gutter using CSS classes
				if (diffChange.type === 'added') {
					el.classList.add('diff-added-gutter');
					el.title = 'Добавлено агентом (ожидает подтверждения)';
				} else if (diffChange.type === 'deleted') {
					el.classList.add('diff-deleted-gutter');
					el.title = 'Удалено агентом (ожидает подтверждения)';
				}
			} else {
				el.title = '';
			}
			});
			return;
		}

		// Get actual content lines for validation
		const contentLines = localContent.value.split('\n');

		// Build maps for quick lookup
		const lineStatusMap = new Map<number, typeof embeddingStatus.value.lineStatuses[0]>();
		embeddingStatus.value.lineStatuses.forEach((status) => {
			lineStatusMap.set(status.lineNumber, status); // status.lineNumber is 0-based
		});

		// Mark lines as:
		// 1. Green: covered with meaningful content
		// 2. Orange: covered but separator/empty (not meaningful)
		// 3. No color: not covered
		const coveredLines = new Set<number>();
		const separatorLines = new Set<number>();

		embeddingStatus.value.lineStatuses.forEach((status) => {
			const lineNum = status.lineNumber;
			if (!status.isCovered) return;

			if (status.isEmpty) {
				separatorLines.add(lineNum);
			} else {
				const lineContent = contentLines[status.lineNumber]?.trim() || '';
				// Check if it's a separator line (like |---|---|)
				if (/^[\s|:\-]+$/.test(lineContent)) {
					separatorLines.add(lineNum);
				} else {
					coveredLines.add(lineNum);
				}
			}
		});

		gutterElements.forEach((el, index) => {
			// Gutter index starts at 0, need to shift down by 2 lines
			const lineNumber = index - 1;
			const status = lineStatusMap.get(lineNumber);
			const diffChange = diffChangesMap.get(lineNumber);

			// Remove existing diff classes
			el.classList.remove('diff-added-gutter', 'diff-deleted-gutter');
			
			// Diff changes have priority over embedding status
			if (diffChange) {
				if (diffChange.type === 'added') {
					el.classList.add('diff-added-gutter');
					el.title = 'Добавлено агентом (ожидает подтверждения)';
				} else if (diffChange.type === 'deleted') {
					el.classList.add('diff-deleted-gutter');
					el.title = 'Удалено агентом (ожидает подтверждения)';
				}
			} else if (coveredLines.has(lineNumber)) {
				el.style.backgroundColor = 'rgba(16, 185, 129, 0.2)'; // accent green with low opacity
				el.title = 'Покрыто эмбеддингом';
			} else if (separatorLines.has(lineNumber)) {
				el.style.backgroundColor = 'rgba(245, 158, 11, 0.2)'; // warning orange with low opacity
				el.title = 'Входит в блок с эмбеддингом, но не содержит полезного текста (разделитель таблицы или пустая строка)';
			} else {
				el.style.backgroundColor = '';
				el.title = status?.isEmpty ? 'Пустая строка' : 'Не покрыто эмбеддингом';
			}
		});
	});
}, 100); // Throttle to max once per 100ms

const debouncedLoadStatus = useDebounceFn(loadEmbeddingStatus, 1500);

// Load status on mount and when content/documentId changes
watch(
	() => [props.documentId, localContent.value],
	() => {
		debouncedLoadStatus();
	},
	{ immediate: true }
);

// Watch for editor DOM changes to update gutter styles
// Debounced to prevent excessive updates during typing
const debouncedUpdateGutterStyles = useDebounceFn(() => {
	updateGutterStyles();
}, 300);

watch(
	() => localContent.value,
	async () => {
		debouncedUpdateGutterStyles();
		// Update content hash when content changes
		if (localContent.value) {
			lastContentHash.value = await computeContentHash(localContent.value);
		}
	}
);

// Watch for diff changes to update styles
// Use shallow watch and manual check to avoid deep reactivity
watch(
	() => props.diffChanges?.length,
	() => {
		updateGutterStyles();
	}
);

// Also watch for status changes in diffChanges
watch(
	() => props.diffChanges?.map(c => c.status).join(','),
	() => {
		updateGutterStyles();
	}
);

onMounted(async () => {
	await loadEmbeddingStatus();
	
	// Initialize content hash
	if (localContent.value) {
		lastContentHash.value = await computeContentHash(localContent.value);
	}

	// Use MutationObserver to watch for CodeMirror DOM changes
	// Only observe structure changes, not style/class changes to avoid cycles
	observerRef.value = new MutationObserver(() => {
		// Only update if there are pending diff changes
		if (props.diffChanges && props.diffChanges.some(c => c.status === 'pending')) {
			updateGutterStyles();
		}
	});

	if (editorContainerRef.value && observerRef.value) {
		observerRef.value.observe(editorContainerRef.value, {
			childList: true,
			subtree: false, // Don't watch deep subtree to reduce callbacks
		});
	}

	// Set up event listeners for focus/blur to maintain styles
	const handleEditorFocus = () => {
		// Clear any existing timeout
		if (focusTimeoutRef.value) {
			clearTimeout(focusTimeoutRef.value);
		}
		// Delay to ensure DOM is updated after focus
		focusTimeoutRef.value = setTimeout(() => {
			updateGutterStyles();
			focusTimeoutRef.value = null;
		}, 50);
	};

	const handleEditorBlur = () => {
		updateGutterStyles();
	};

	// Find CodeMirror editor and add focus/blur listeners
	const setupEditorListeners = () => {
		if (!editorContainerRef.value) return;
		
		// Remove existing listeners first to prevent duplicates
		editorListenersRef.value.forEach(({ element, event, handler }) => {
			element.removeEventListener(event, handler, true);
		});
		editorListenersRef.value = [];
		
		const cmEditor = editorContainerRef.value.querySelector('.cm-editor') as HTMLElement;
		if (cmEditor) {
			// Store listeners for cleanup
			const listeners = [
				{ element: cmEditor, event: 'focus', handler: handleEditorFocus },
				{ element: cmEditor, event: 'blur', handler: handleEditorBlur },
				{ element: cmEditor, event: 'focusin', handler: handleEditorFocus },
				{ element: cmEditor, event: 'focusout', handler: handleEditorBlur },
			];
			
			listeners.forEach(({ element, event, handler }) => {
				element.addEventListener(event, handler, true);
				editorListenersRef.value.push({ element, event, handler });
			});
		}
	};

	// Set up periodic auto-update (every 60 seconds)
	autoUpdateInterval.value = setInterval(() => {
		updateEmbeddingsIfNeeded();
	}, 60000);

	// Initial setup of listeners after a delay to ensure editor is mounted
	// Only set up once, don't recreate on every DOM change
	setTimeout(() => {
		setupEditorListeners();
	}, 1000);

	onUnmounted(() => {
		// Disconnect main observer
		if (observerRef.value) {
			observerRef.value.disconnect();
			observerRef.value = null;
		}
		
		// Clear intervals
		if (autoUpdateInterval.value) {
			clearInterval(autoUpdateInterval.value);
			autoUpdateInterval.value = null;
		}
		
		// Clear timeout
		if (focusTimeoutRef.value) {
			clearTimeout(focusTimeoutRef.value);
			focusTimeoutRef.value = null;
		}
		
		// Remove all event listeners
		editorListenersRef.value.forEach(({ element, event, handler }) => {
			element.removeEventListener(event, handler, true);
		});
		editorListenersRef.value = [];
	});
});

// Expose embeddingStatus for parent component
defineExpose({
	embeddingStatus,
	loadEmbeddingStatus,
});
</script>

<style scoped>
.markdown-editor {
	height: 100%;
	width: 100%;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

:deep(.md-editor) {
	height: 100%;
	width: 100%;
	display: flex;
	flex-direction: column;
}

:deep(.md-editor-content-wrapper) {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

:deep(.md-editor-content) {
	flex: 1;
	overflow: auto;
}

/* Diff highlighting styles - only in gutter */
:deep(.cm-gutterElement.diff-added-gutter) {
	background-color: rgba(34, 197, 94, 0.4) !important;
}

:deep(.cm-gutterElement.diff-deleted-gutter) {
	background-color: rgba(239, 68, 68, 0.4) !important;
}
</style>
