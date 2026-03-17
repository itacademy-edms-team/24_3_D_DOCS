<template>
	<div class="settings-llm-page">
		<header class="settings-header content-header">
			<button class="settings-back" @click="router.push('/settings')">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span>Назад</span>
			</button>
			<h1 class="page-title">Настройки LLM</h1>
		</header>

		<main class="settings-body content-body">
			<section class="providers-section">
				<h3 class="section-title">Провайдеры LLM</h3>
				<Accordion title="Ollama" :defaultOpen="true">
					<div class="ollama-content">
						<!-- Состояние: проверка пройдена, ещё не сохранено -->
						<div v-if="verifiedKey" class="ollama-verified">
							<p class="ollama-tile ollama-tile--success">API ключ корректный</p>
							<div class="ollama-actions">
								<Button
									variant="secondary"
									class="ollama-action-btn"
									@click="handleCancel"
								>
									Отмена
								</Button>
								<Button
									variant="primary"
									class="ollama-action-btn"
									:isLoading="isSaving"
									@click="handleSave"
								>
									Сохранить ключ
								</Button>
							</div>
						</div>

						<!-- Состояние: ключ сохранён -->
						<div v-else-if="hasKey" class="ollama-key-saved">
							<p class="ollama-tile ollama-tile--success">Ключ сохранён</p>
							<div class="ollama-actions">
								<Button
									variant="secondary"
									class="ollama-action-btn"
									:isLoading="isCopying"
									@click="handleCopyKey"
								>
									Копировать ключ
								</Button>
								<Button
									variant="danger"
									class="ollama-action-btn"
									:isLoading="isDeleting"
									@click="handleDelete"
								>
									Удалить ключ
								</Button>
							</div>
							<div v-if="modelsLoadError" class="ollama-tile ollama-tile--error ollama-error">
								{{ modelsLoadError }}
							</div>
							<div v-else class="ollama-model-settings">
								<p class="ollama-model-settings__title">Модели</p>
								<div class="ollama-model-row">
									<div class="ollama-model-label">Модель агента</div>
									<Dropdown
										v-model="agentModel"
										:options="allModelOptions"
										:disabled="isLoadingModels || allModelOptions.length === 0"
										placeholder="Загрузка…"
										class="ollama-model-dropdown-wrap"
									/>
								</div>
								<div class="ollama-model-row">
									<div class="ollama-model-label">Модель для текста вложений</div>
									<Dropdown
										v-model="attachmentModel"
										:options="allModelOptions"
										:disabled="isLoadingModels || allModelOptions.length === 0"
										placeholder="Загрузка…"
										class="ollama-model-dropdown-wrap"
									/>
								</div>
								<div class="ollama-model-row">
									<div class="ollama-model-label">Модель для изображений</div>
									<Dropdown
										v-model="visionModel"
										:options="visionModelOptions"
										:disabled="isLoadingModels || visionModelOptions.length === 0"
										placeholder="Нет моделей с поддержкой изображений"
										class="ollama-model-dropdown-wrap"
									/>
								</div>
								<Button
									variant="primary"
									class="ollama-model-save"
									:isLoading="isSavingModels"
									:disabled="isLoadingModels || allModelOptions.length === 0"
									@click="handleSaveModels"
								>
									Сохранить настройки моделей
								</Button>
							</div>
						</div>

						<!-- Состояние: нет ключа -->
						<div v-else class="ollama-input">
							<p class="ollama-tile ollama-tile--error">API ключ отсутствует</p>
							<Input
								v-model="apiKeyInput"
								type="password"
								:showPasswordToggle="true"
								placeholder="API ключ"
								autocomplete="off"
								fullWidth
							/>
							<Button
								variant="primary"
								:isLoading="isVerifying"
								:disabled="!apiKeyInput.trim()"
								@click="handleVerify"
							>
								Проверить API ключ
							</Button>
						</div>

						<p v-if="errorMessage" class="ollama-tile ollama-tile--error ollama-error">{{ errorMessage }}</p>
					</div>
				</Accordion>
			</section>
		</main>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue';
import { useRouter } from 'vue-router';
import Icon from '@/components/Icon.vue';
import Button from '@/shared/ui/Button/Button.vue';
import Input from '@/shared/ui/Input/Input.vue';
import Accordion from '@/shared/ui/Accordion/Accordion.vue';
import Dropdown from '@/shared/ui/Dropdown/Dropdown.vue';
import AIAPI from '@/shared/api/AIAPI';
import type { LlmModelOptionDTO } from '@/shared/api/AIAPI';
import BadRequest from '@/shared/api/error/BadRequest';

const router = useRouter();

function getErrorMessage(err: unknown, fallback: string): string {
	if (err instanceof BadRequest) return err.message;
	const e = err as { message?: string; response?: { data?: { message?: string } } };
	return e?.message || e?.response?.data?.message || fallback;
}

const apiKeyInput = ref('');
const hasKey = ref(false);
const verifiedKey = ref('');
const isVerifying = ref(false);
const isSaving = ref(false);
const isDeleting = ref(false);
const isCopying = ref(false);
const errorMessage = ref('');

const catalog = ref<LlmModelOptionDTO[]>([]);
const isLoadingModels = ref(false);
const isSavingModels = ref(false);
const modelsLoadError = ref('');
const agentModel = ref<string | undefined>(undefined);
const attachmentModel = ref<string | undefined>(undefined);
const visionModel = ref<string | undefined>(undefined);

const allModelOptions = computed(() =>
	catalog.value.map((m) => ({ value: m.modelName, label: m.modelName })),
);

const visionModelOptions = computed(() =>
	catalog.value
		.filter((m) => m.hasView)
		.map((m) => ({ value: m.modelName, label: m.modelName })),
);

function pickFromCatalog(
	saved: string | null | undefined,
	effective: string,
	names: string[],
): string | undefined {
	if (saved) return saved;
	if (names.includes(effective)) return effective;
	return names[0];
}

function pickVisionModel(
	saved: string | null | undefined,
	effective: string,
	visionNames: string[],
): string | undefined {
	if (saved && visionNames.includes(saved)) return saved;
	if (visionNames.includes(effective)) return effective;
	return visionNames[0];
}

async function loadModelSettings() {
	modelsLoadError.value = '';
	isLoadingModels.value = true;
	try {
		const [models, prefs] = await Promise.all([
			AIAPI.getOllamaModels(),
			AIAPI.getOllamaModelPreferences(),
		]);
		catalog.value = models;
		const allNames = models.map((m) => m.modelName);
		const visionNames = models.filter((m) => m.hasView).map((m) => m.modelName);
		agentModel.value = pickFromCatalog(
			prefs.agentModelName,
			prefs.effectiveAgentModel,
			allNames,
		);
		attachmentModel.value = pickFromCatalog(
			prefs.attachmentTextModelName,
			prefs.effectiveAttachmentTextModel,
			allNames,
		);
		visionModel.value = pickVisionModel(
			prefs.visionModelName,
			prefs.effectiveVisionModel,
			visionNames,
		);
	} catch (err) {
		modelsLoadError.value = getErrorMessage(
			err,
			'Не удалось загрузить список моделей или настройки.',
		);
		catalog.value = [];
	} finally {
		isLoadingModels.value = false;
	}
}

async function handleSaveModels() {
	modelsLoadError.value = '';
	clearError();
	isSavingModels.value = true;
	try {
		await AIAPI.putOllamaModelPreferences({
			agentModelName: agentModel.value ?? null,
			attachmentTextModelName: attachmentModel.value ?? null,
			visionModelName: visionModel.value ?? null,
		});
		await loadModelSettings();
	} catch (err) {
		errorMessage.value = getErrorMessage(err, 'Не удалось сохранить настройки моделей');
	} finally {
		isSavingModels.value = false;
	}
}

watch(hasKey, (v) => {
	if (v) loadModelSettings();
	else {
		catalog.value = [];
		modelsLoadError.value = '';
	}
});

async function loadKeyStatus() {
	try {
		const status = await AIAPI.getOllamaKeyStatus();
		hasKey.value = status.hasKey;
		verifiedKey.value = '';
	} catch (err) {
		console.error('Failed to load Ollama key status:', err);
		hasKey.value = false;
		verifiedKey.value = '';
	}
}

function clearError() {
	errorMessage.value = '';
}

async function handleVerify() {
	clearError();
	if (!apiKeyInput.value.trim()) return;
	isVerifying.value = true;
	try {
		await AIAPI.verifyOllamaKey(apiKeyInput.value.trim());
		verifiedKey.value = apiKeyInput.value.trim();
	} catch (err) {
		errorMessage.value = getErrorMessage(
			err,
			'Не удалось проверить API ключ. Попробуйте позже.',
		);
	} finally {
		isVerifying.value = false;
	}
}

async function handleSave() {
	clearError();
	if (!verifiedKey.value) return;
	isSaving.value = true;
	try {
		await AIAPI.setOllamaKey(verifiedKey.value);
		hasKey.value = true;
		verifiedKey.value = '';
		apiKeyInput.value = '';
	} catch (err) {
		errorMessage.value = getErrorMessage(err, 'Не удалось сохранить ключ');
	} finally {
		isSaving.value = false;
	}
}

function handleCancel() {
	verifiedKey.value = '';
	apiKeyInput.value = '';
	clearError();
}

async function handleCopyKey() {
	clearError();
	isCopying.value = true;
	try {
		const { apiKey } = await AIAPI.getOllamaKey();
		await navigator.clipboard.writeText(apiKey);
	} catch (err) {
		errorMessage.value = 'Не удалось скопировать ключ';
	} finally {
		isCopying.value = false;
	}
}

async function handleDelete() {
	clearError();
	isDeleting.value = true;
	try {
		await AIAPI.deleteOllamaKey();
		hasKey.value = false;
		verifiedKey.value = '';
		apiKeyInput.value = '';
	} catch (err) {
		errorMessage.value = 'Не удалось удалить ключ';
	} finally {
		isDeleting.value = false;
	}
}

onMounted(() => {
	loadKeyStatus();
});
</script>

<style scoped>
.settings-llm-page {
	min-height: 100vh;
	background: var(--bg-primary);
	color: var(--text-primary);
	display: flex;
	flex-direction: column;
}

.settings-header {
	display: flex;
	align-items: center;
	gap: var(--spacing-md);
	padding: var(--spacing-xl) var(--spacing-xl) var(--spacing-md);
	border-bottom: 1px solid var(--border-color);
}

.settings-back {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
	padding: 0.6rem 1rem;
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	border-radius: var(--radius-md);
	cursor: pointer;
	font-size: 14px;
	font-weight: 500;
	color: var(--text-primary);
	font-family: inherit;
	transition: background 0.2s, border-color 0.2s;
}

.settings-back:hover {
	background: var(--bg-tertiary);
	border-color: var(--border-hover);
}

.page-title {
	margin: 0;
	font-size: 28px;
	font-weight: 700;
	color: var(--text-primary);
}

.settings-body {
	flex: 1;
	display: flex;
	justify-content: center;
	align-items: flex-start;
	padding: var(--spacing-xl);
}

.providers-section {
	width: 100%;
	max-width: 560px;
}

.section-title {
	margin: 0 0 var(--spacing-lg);
	font-size: 1rem;
	font-weight: 600;
	color: var(--text-secondary);
}

.ollama-content {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	align-items: center;
}

.ollama-key-saved,
.ollama-verified {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	align-items: center;
	width: 100%;
}

.ollama-input {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	width: 100%;
}

.ollama-tile {
	margin: 0;
	padding: 0.75rem 1.25rem;
	font-size: 14px;
	font-weight: 600;
	text-align: center;
	border-radius: var(--radius-md);
	width: 100%;
	box-sizing: border-box;
}

.ollama-tile--success {
	background: rgba(var(--accent-rgb, 16, 185, 129), 0.12);
	color: var(--accent);
	border: 1px solid rgba(var(--accent-rgb, 16, 185, 129), 0.3);
}

.ollama-tile--error {
	background: rgba(239, 68, 68, 0.12);
	color: var(--danger);
	border: 1px solid rgba(239, 68, 68, 0.3);
}

.ollama-actions {
	display: flex;
	gap: var(--spacing-md);
	justify-content: center;
	flex-wrap: wrap;
	width: 100%;
}

.ollama-action-btn {
	flex: 1;
	min-width: 140px;
}

.ollama-status {
	margin: 0;
	font-size: 14px;
	color: var(--text-tertiary);
}

.ollama-status--connected {
	color: var(--info);
}

.ollama-error {
	margin: 0;
	font-size: 14px;
	width: 100%;
}

.ollama-model-settings {
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	width: 100%;
	margin-top: var(--spacing-md);
	padding-top: var(--spacing-md);
	border-top: 1px solid var(--border-color);
}

.ollama-model-settings__title {
	margin: 0;
	font-size: 0.9rem;
	font-weight: 600;
	color: var(--text-secondary);
	text-align: left;
	width: 100%;
}

.ollama-model-row {
	display: flex;
	flex-direction: column;
	align-items: stretch;
	gap: var(--spacing-xs);
	width: 100%;
}

.ollama-model-label {
	font-size: 13px;
	font-weight: 500;
	color: var(--text-secondary);
}

.ollama-model-dropdown-wrap {
	width: 100%;
}

.ollama-model-dropdown-wrap :deep(.dropdown) {
	display: block;
	width: 100%;
}

.ollama-model-dropdown-wrap :deep(.dropdown__trigger) {
	width: 100%;
	box-sizing: border-box;
}

.ollama-model-save {
	width: 100%;
	max-width: 280px;
	align-self: center;
	margin-top: var(--spacing-sm);
}
</style>
