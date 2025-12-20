<template>
	<div class="form">
		<h2 class="title">
			{{ step === 'email' ? 'Создать аккаунт' : 'Введите код' }}
		</h2>

		<form v-if="step === 'email'" @submit.prevent="handleSendVerification">
			<Input
				type="email"
				placeholder="Email"
				v-model="email"
				:error="errors.email"
				@update:model-value="handleEmailChange"
				@blur="validateField('email', email)"
			/>
			<Input
				type="password"
				placeholder="Пароль"
				v-model="password"
				:error="errors.password"
				show-password-toggle
				@update:model-value="handlePasswordChange"
				@blur="validateField('password', password)"
			/>
			<Input
				type="text"
				placeholder="Имя"
				v-model="name"
				:error="errors.name"
				@update:model-value="handleNameChange"
				@blur="validateField('name', name)"
			/>

			<ErrorMessage :message="authStore.error" />

			<Button type="submit" :is-loading="authStore.isLoading" full-width>
				{{ authStore.isLoading ? 'Отправляем код...' : 'Отправить код' }}
			</Button>
		</form>

		<template v-else>
			<p class="codeHint">
				Мы отправили код подтверждения на<br />
				<strong>{{ email }}</strong>
			</p>
			<form @submit.prevent="handleVerifyCode" class="codeForm">
				<div class="codeInputWrapper">
					<CodeInput
						:length="6"
						v-model="code"
						@update:model-value="handleCodeChange"
						@complete="validateCode"
						:auto-focus="true"
					/>
				</div>
				<ErrorMessage :message="codeError || authStore.error" />

				<Button
					type="submit"
					:is-loading="authStore.isLoading"
					full-width
					:disabled="code.length !== 6"
				>
					Подтвердить
				</Button>
			</form>

			<div class="resendSection">
				<button
					type="button"
					class="resendButton"
					@click="handleResendCode"
					:disabled="resendTimer > 0 || isResending"
				>
					{{
						isResending
							? 'Отправляем...'
							: resendTimer > 0
								? `Отправить заново (${resendTimer}с)`
								: 'Отправить код заново'
					}}
				</button>
			</div>

			<button type="button" class="backButton" @click="handleBackToEmail">
				← Назад
			</button>
		</template>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { z } from 'zod';
import Input from '@/shared/ui/Input/Input.vue';
import Button from '@/shared/ui/Button/Button.vue';
import CodeInput from '@/shared/ui/CodeInput/CodeInput.vue';
import ErrorMessage from '@/shared/ui/ErrorMessage/ErrorMessage.vue';
import { useAuthStore } from '@/entities/auth/store/authStore';

const router = useRouter();
const authStore = useAuthStore();

const step = ref<'email' | 'code'>('email');

const registerSchema = z.object({
	email: z.string().email('Введите корректный email'),
	password: z.string().min(6, 'Пароль должен быть минимум 6 символов'),
	name: z.string().min(2, 'Имя должно быть минимум 2 символа'),
});

const codeSchema = z.string().length(6, 'Код должен содержать 6 цифр');

const email = ref('');
const password = ref('');
const name = ref('');
const errors = ref<{ email?: string; password?: string; name?: string }>({});

const code = ref('');
const codeError = ref('');
const resendTimer = ref(0);
const isResending = ref(false);

let timerInterval: NodeJS.Timeout | null = null;

onMounted(() => {
	authStore.clearError();
});

onUnmounted(() => {
	if (timerInterval) {
		clearInterval(timerInterval);
	}
});

function validateField(field: 'email' | 'password' | 'name', value: string) {
	const result = registerSchema.shape[field].safeParse(value);
	if (!result.success) {
		errors.value[field] = result.error.errors[0]?.message;
	} else {
		errors.value[field] = undefined;
	}
}

function handleEmailChange() {
	if (errors.value.email) {
		validateField('email', email.value);
	}
}

function handlePasswordChange() {
	if (errors.value.password) {
		validateField('password', password.value);
	}
}

function handleNameChange() {
	if (errors.value.name) {
		validateField('name', name.value);
	}
}

function handleCodeChange() {
	if (codeError.value && code.value) {
		const result = codeSchema.safeParse(code.value);
		codeError.value = result.success ? '' : result.error.errors[0]?.message || '';
	}
}

async function handleSendVerification() {
	errors.value = {};
	const result = registerSchema.safeParse({
		email: email.value,
		password: password.value,
		name: name.value,
	});

	if (!result.success) {
		const fieldErrors: Record<string, string> = {};
		result.error.issues.forEach((issue) => {
			if (issue.path[0]) {
				fieldErrors[issue.path[0] as string] = issue.message;
			}
		});
		errors.value = fieldErrors;
		return;
	}

	try {
		authStore.clearError();
		await authStore.sendVerification(result.data);
		step.value = 'code';
		resendTimer.value = 60;
		startTimer();
	} catch (err) {
		console.error('Ошибка отправки кода:', err);
	}
}

function startTimer() {
	if (timerInterval) {
		clearInterval(timerInterval);
	}
	timerInterval = setInterval(() => {
		if (resendTimer.value > 0) {
			resendTimer.value--;
		} else {
			if (timerInterval) {
				clearInterval(timerInterval);
				timerInterval = null;
			}
		}
	}, 1000);
}

async function handleResendCode() {
	if (resendTimer.value > 0 || isResending.value) return;

	isResending.value = true;
	try {
		authStore.clearError();
		await authStore.sendVerification({
			email: email.value,
			password: password.value,
			name: name.value,
		});
		resendTimer.value = 60;
		startTimer();
	} catch (err) {
		console.error('Ошибка повторной отправки кода:', err);
	} finally {
		isResending.value = false;
	}
}

async function handleVerifyCode() {
	const result = codeSchema.safeParse(code.value);
	if (!result.success) {
		codeError.value = result.error.errors[0]?.message || '';
		return;
	}

	try {
		authStore.clearError();
		await authStore.register({ email: email.value, code: code.value });
		router.push('/dashboard');
	} catch (err) {
		console.error('Ошибка регистрации:', err);
	}
}

function handleBackToEmail() {
	step.value = 'email';
	code.value = '';
	codeError.value = '';
	resendTimer.value = 0;
	isResending.value = false;
	if (timerInterval) {
		clearInterval(timerInterval);
		timerInterval = null;
	}
	authStore.clearError();
}
</script>

<style scoped>
.form {
	display: flex;
	flex-direction: column;
	gap: 20px;
	width: 100%;
}

.form form {
	display: flex;
	flex-direction: column;
	gap: 16px;
	width: 100%;
}

.title {
	font-size: 28px;
	font-weight: 700;
	text-align: center;
	margin: 0 0 8px 0;
	color: #e4e4e7;
	letter-spacing: -0.02em;
}


.codeForm {
	display: flex;
	flex-direction: column;
	gap: 24px;
}

.codeInputWrapper {
	display: flex;
	justify-content: center;
	margin: 8px 0;
}

.codeHint {
	text-align: center;
	color: #a1a1aa;
	margin-bottom: 8px;
	font-size: 14px;
	line-height: 1.6;
}

.codeHint strong {
	color: #e4e4e7;
	font-weight: 600;
}

.backButton {
	background: transparent;
	border: none;
	color: #6366f1;
	font-size: 14px;
	cursor: pointer;
	padding: 8px;
	margin-top: 12px;
	width: 100%;
	transition: all 0.2s;
	font-weight: 600;
	border-radius: 8px;
}

.backButton:hover {
	background: rgba(99, 102, 241, 0.1);
	color: #818cf8;
}

.resendSection {
	display: flex;
	justify-content: center;
	margin: 16px 0;
}

.resendButton {
	background: transparent;
	border: 1px solid #3f3f46;
	color: #a1a1aa;
	font-size: 14px;
	cursor: pointer;
	padding: 8px 16px;
	transition: all 0.2s;
	font-weight: 500;
	border-radius: 8px;
	min-width: 200px;
}

.resendButton:hover:not(:disabled) {
	background: rgba(99, 102, 241, 0.1);
	border-color: #6366f1;
	color: #818cf8;
}

.resendButton:disabled {
	opacity: 0.5;
	cursor: not-allowed;
}

</style>
