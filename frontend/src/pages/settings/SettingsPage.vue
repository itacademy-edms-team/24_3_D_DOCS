<template>
	<div class="settings-page">
		<header class="settings-header content-header">
			<button class="settings-back" @click="router.push('/dashboard')">
				<Icon name="arrow_back" size="18" ariaLabel="Назад" />
				<span>Назад</span>
			</button>
			<h1 class="page-title">Настройки</h1>
		</header>

		<main class="settings-body content-body">
			<ul class="settings-list">
				<li>
					<Button
						variant="secondary"
						fullWidth
						class="settings-btn"
						@click="router.push('/settings/llm')"
					>
						Настройки LLM
					</Button>
				</li>
				<li>
					<Button
						variant="secondary"
						fullWidth
						class="settings-btn"
						@click="router.push('/settings/keyboard')"
					>
						Клавиши редактора
					</Button>
				</li>
				<li>
					<Button
						variant="danger"
						fullWidth
						class="settings-btn"
						@click="openDeleteAccountModal"
					>
						Удалить аккаунт
					</Button>
				</li>
			</ul>
		</main>

		<Modal
			v-model="deleteModalOpen"
			title="Удаление аккаунта"
			size="sm"
			:close-on-backdrop="false"
			@update:model-value="onDeleteModalToggle"
		>
			<div class="delete-account-modal">
				<p class="delete-account-modal__lead">
					Вы собираетесь <strong>безвозвратно</strong> удалить свой аккаунт. Это нельзя отменить.
				</p>
				<ul class="delete-account-modal__list">
					<li>все документы и вложения;</li>
					<li>профили стилей и титульные листы;</li>
				</ul>
				<p class="delete-account-modal__hint">
					Чтобы подтвердить, введите слово <code class="delete-account-modal__code">{{ DELETE_CONFIRM_PHRASE }}</code> в поле ниже
					(заглавными буквами).
				</p>
				<input
					v-model="deleteConfirmInput"
					type="text"
					class="delete-account-modal__input"
					:placeholder="DELETE_CONFIRM_PHRASE"
					autocomplete="off"
					spellcheck="false"
					aria-label="Подтверждение удаления аккаунта"
				/>
			</div>
			<template #footer>
				<Button variant="secondary" @click="closeDeleteAccountModal">Отмена</Button>
				<Button
					variant="danger"
					:disabled="!canConfirmDelete || authStore.isLoading"
					:isLoading="authStore.isLoading"
					@click="executeDeleteAccount"
				>
					Удалить навсегда
				</Button>
			</template>
		</Modal>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import Icon from '@/components/Icon.vue';
import Button from '@/shared/ui/Button/Button.vue';
import Modal from '@/shared/ui/Modal/Modal.vue';
import { useAuthStore } from '@/entities/auth/store/authStore';

const DELETE_CONFIRM_PHRASE = 'УДАЛИТЬ';

const router = useRouter();
const authStore = useAuthStore();

const deleteModalOpen = ref(false);
const deleteConfirmInput = ref('');

const canConfirmDelete = computed(
	() => deleteConfirmInput.value.trim() === DELETE_CONFIRM_PHRASE,
);

function openDeleteAccountModal() {
	deleteConfirmInput.value = '';
	deleteModalOpen.value = true;
}

function closeDeleteAccountModal() {
	deleteModalOpen.value = false;
}

function onDeleteModalToggle(open: boolean) {
	if (!open) {
		deleteConfirmInput.value = '';
	}
}

async function executeDeleteAccount() {
	if (!canConfirmDelete.value) {
		return;
	}
	try {
		await authStore.deleteAccount();
		deleteModalOpen.value = false;
		await router.push('/auth');
	} catch {
		alert(authStore.error || 'Не удалось удалить аккаунт');
	}
}
</script>

<style scoped>
.settings-page {
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

.settings-list {
	list-style: none;
	padding: 0;
	margin: 0;
	display: flex;
	flex-direction: column;
	gap: var(--spacing-md);
	width: 100%;
	max-width: 480px;
}

.settings-btn {
	padding: 1rem 1.5rem;
	font-size: 16px;
	font-weight: 600;
	justify-content: flex-start;
}

.delete-account-modal__lead {
	margin: 0 0 var(--spacing-md);
	font-size: 15px;
	line-height: 1.5;
	color: var(--text-primary);
}

.delete-account-modal__list {
	margin: 0 0 var(--spacing-lg);
	padding-left: 1.25rem;
	color: var(--text-secondary);
	font-size: 14px;
	line-height: 1.55;
}

.delete-account-modal__list li {
	margin-bottom: 0.35rem;
}

.delete-account-modal__hint {
	margin: 0 0 var(--spacing-sm);
	font-size: 13px;
	line-height: 1.5;
	color: var(--text-secondary);
}

.delete-account-modal__code {
	padding: 0.15rem 0.4rem;
	border-radius: var(--radius-sm);
	background: var(--bg-secondary);
	border: 1px solid var(--border-color);
	font-size: 0.95em;
	font-weight: 700;
	color: var(--danger, #ef4444);
}

.delete-account-modal__input {
	width: 100%;
	box-sizing: border-box;
	margin-top: var(--spacing-sm);
	padding: 0.75rem 1rem;
	font-size: 16px;
	font-weight: 600;
	font-family: inherit;
	letter-spacing: 0.02em;
	color: var(--text-primary);
	background: var(--bg-secondary);
	border: 2px solid var(--border-color);
	border-radius: var(--radius-md);
}

.delete-account-modal__input:focus {
	outline: none;
	border-color: rgba(239, 68, 68, 0.55);
}
</style>
