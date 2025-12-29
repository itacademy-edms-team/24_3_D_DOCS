<template>
	<div class="container">
		<div class="background">
			<div class="gridPattern" />
			<div class="glow1" />
			<div class="glow2" />
		</div>

		<div class="logo">
			<div class="logoIcon">📄</div>
			<h1 class="logoText">DDOCS</h1>
		</div>

		<Card class="card">
			<div class="tabs">
				<button
					type="button"
					:class="['tab', { active: activeTab === 'login' }]"
					@click="handleTabChange('login')"
				>
					Вход
				</button>
				<button
					type="button"
					:class="['tab', { active: activeTab === 'register' }]"
					@click="handleTabChange('register')"
				>
					Регистрация
				</button>
				<div
					class="tabIndicator"
					:style="{
						transform: `translateX(${activeTab === 'login' ? '0' : '100'}%)`,
					}"
				/>
			</div>

			<div class="formContainer" :key="activeTab">
				<LoginForm v-if="activeTab === 'login'" />
				<RegisterForm v-else />
			</div>
		</Card>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import Card from '@/shared/ui/Card/Card.vue';
import LoginForm from '@/features/auth/login/LoginForm.vue';
import RegisterForm from '@/features/auth/register/RegisterForm.vue';
import { useAuthStore } from '@/entities/auth/store/authStore';

type Tab = 'login' | 'register';

const activeTab = ref<Tab>('login');
const authStore = useAuthStore();

function handleTabChange(tab: Tab) {
	if (tab === activeTab.value) return;
	authStore.clearError();
	activeTab.value = tab;
}

onMounted(() => {
	authStore.clearError();
});
</script>

<style scoped>
.container {
	min-height: 100vh;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 20px;
	position: relative;
	overflow: hidden;
}

.background {
	position: absolute;
	top: 0;
	left: 0;
	width: 100%;
	height: 100%;
	z-index: 0;
}

.gridPattern {
	position: absolute;
	top: 0;
	left: 0;
	width: 100%;
	height: 100%;
	background-image: linear-gradient(rgba(var(--accent-rgb), 0.05) 1px, transparent 1px),
		linear-gradient(90deg, rgba(var(--accent-rgb), 0.05) 1px, transparent 1px);
	background-size: 50px 50px;
	animation: gridMove 20s linear infinite;
}

@keyframes gridMove {
	0% {
		transform: translate(0, 0);
	}
	100% {
		transform: translate(50px, 50px);
	}
}

.glow1,
.glow2 {
	position: absolute;
	border-radius: 50%;
	filter: blur(120px);
	opacity: 0.3;
	animation: float 20s ease-in-out infinite;
}

.glow1 {
	width: 600px;
	height: 600px;
	background: radial-gradient(circle, var(--accent) 0%, transparent 70%);
	top: -200px;
	right: -200px;
	animation-delay: 0s;
}

.glow2 {
	width: 500px;
	height: 500px;
	background: radial-gradient(circle, #8b5cf6 0%, transparent 70%);
	bottom: -150px;
	left: -150px;
	animation-delay: 7s;
}

@keyframes float {
	0%,
	100% {
		transform: translate(0, 0) scale(1);
	}
	33% {
		transform: translate(50px, -70px) scale(1.1);
	}
	66% {
		transform: translate(-40px, 40px) scale(0.9);
	}
}

.logo {
	display: flex;
	align-items: center;
	gap: 12px;
	margin-bottom: 48px;
	z-index: 1;
	animation: fadeInDown 0.6s ease;
}

.logoIcon {
	font-size: 48px;
	filter: drop-shadow(0 0 20px rgba(var(--accent-rgb), 0.5));
}

.logoText {
	font-size: 36px;
	font-weight: 700;
	background: linear-gradient(135deg, var(--accent) 0%, var(--accent-hover) 100%);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
	background-clip: text;
	letter-spacing: -0.02em;
}

@keyframes fadeInDown {
	from {
		opacity: 0;
		transform: translateY(-20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.card {
	width: 100%;
	max-width: 440px;
	position: relative;
	z-index: 1;
	animation: fadeInUp 0.6s ease 0.1s both;
}

@keyframes fadeInUp {
	from {
		opacity: 0;
		transform: translateY(20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.tabs {
	display: flex;
	gap: 8px;
	margin-bottom: 32px;
	background: #18181b;
	padding: 6px;
	border-radius: 14px;
	position: relative;
	border: 1px solid #27272a;
}

.tabIndicator {
	position: absolute;
	top: 6px;
	left: 6px;
	width: calc(50% - 10px);
	height: calc(100% - 12px);
	background: var(--accent);
	border-radius: 10px;
	transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: 0 4px 12px rgba(var(--accent-rgb), 0.3);
	z-index: 0;
}

.tab {
	flex: 1;
	padding: 12px 20px;
	font-size: 15px;
	font-weight: 600;
	background: transparent;
	color: #71717a;
	border: none;
	border-radius: 10px;
	cursor: pointer;
	transition: all 0.2s ease;
	font-family: inherit;
	position: relative;
	z-index: 1;
}

.tab:hover {
	color: #a1a1aa;
}

.tab.active {
	color: white;
}

.formContainer {
	animation: fadeInSlide 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes fadeInSlide {
	from {
		opacity: 0;
		transform: translateY(8px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@media (max-width: 640px) {
	.logo {
		margin-bottom: 32px;
	}

	.logoIcon {
		font-size: 36px;
	}

	.logoText {
		font-size: 28px;
	}

	.card {
		max-width: 100%;
	}

	.tab {
		padding: 10px 16px;
		font-size: 14px;
	}
}
</style>
