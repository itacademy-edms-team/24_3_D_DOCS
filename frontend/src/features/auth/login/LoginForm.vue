<template>
	<form class="form" @submit.prevent="handleSubmit">
		<h2 class="title">Вход</h2>

		<Input
			name="email"
			type="email"
			label="Email"
			placeholder="your@email.com"
			v-model="formData.email"
			:error="errors.email"
			full-width
			autocomplete="email"
		/>

		<Input
			name="password"
			type="password"
			label="Пароль"
			placeholder="••••••••"
			v-model="formData.password"
			:error="errors.password"
			full-width
			autocomplete="current-password"
			show-password-toggle
		/>

		<div v-if="authStore.error" class="apiError">{{ authStore.error }}</div>

		<Button type="submit" variant="primary" full-width :is-loading="authStore.isLoading">
			Войти
		</Button>
	</form>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { z } from 'zod';
import Input from '@/shared/ui/Input/Input.vue';
import Button from '@/shared/ui/Button/Button.vue';
import { useAuthStore } from '@/entities/auth/store/authStore';

const router = useRouter();
const authStore = useAuthStore();

const loginSchema = z.object({
	email: z.string().email('Неверный формат email'),
	password: z.string().min(6, 'Пароль должен быть минимум 6 символов'),
});

const formData = ref({ email: '', password: '' });
const errors = ref<Record<string, string>>({});

onMounted(() => {
	authStore.clearError();
});

async function handleSubmit() {
	errors.value = {};
	authStore.clearError();

	const result = loginSchema.safeParse(formData.value);

	if (!result.success) {
		const fieldErrors: Record<string, string> = {};
		const issues = result.error?.issues || [];
		for (const issue of issues) {
			if (issue.path && issue.path.length > 0) {
				fieldErrors[issue.path[0] as string] = issue.message;
			}
		}
		errors.value = fieldErrors;
		return;
	}

	try {
		authStore.clearError();
		await authStore.login(formData.value);
		router.push('/dashboard');
	} catch (error: any) {
		console.error('Login error:', error);
	}
}
</script>

<style scoped>
.form {
	display: flex;
	flex-direction: column;
	gap: 16px;
	width: 100%;
	padding: 0;
}

.title {
	font-size: 28px;
	font-weight: 700;
	text-align: center;
	margin: 0 0 8px 0;
	color: #e4e4e7;
	letter-spacing: -0.02em;
}

.apiError {
	padding: 12px;
	background: rgba(239, 68, 68, 0.1);
	border: 1px solid rgba(239, 68, 68, 0.2);
	border-radius: 8px;
	color: #f87171;
	font-size: 13px;
	text-align: center;
	animation: shake 0.3s ease;
}

@keyframes shake {
	0%,
	100% {
		transform: translateX(0);
	}
	25% {
		transform: translateX(-4px);
	}
	75% {
		transform: translateX(4px);
	}
}
</style>
