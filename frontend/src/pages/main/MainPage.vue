<template>
	<div class="container">
		<div class="content">
			<h1>Добро пожаловать в DDOCS</h1>
			<div class="userCard">
				<div class="userInfo">
					<div class="userAvatar">👤</div>
					<div class="userDetails">
						<span class="userName">{{ user?.name || 'Пользователь' }}</span>
						<span class="userEmail">{{ user?.email }}</span>
					</div>
				</div>
				<button class="logoutButton" @click="handleLogout" type="button">
					🚪 Выход
				</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/entities/auth/store/authStore';

const router = useRouter();
const authStore = useAuthStore();

const user = computed(() => authStore.user);

async function handleLogout() {
	await authStore.logout();
	router.push('/auth');
}
</script>

<style scoped>
.container {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	height: 100vh;
	gap: 2rem;
}

.content {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2rem;
}

h1 {
	font-size: 2rem;
	font-weight: 700;
}

.userCard {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 1rem;
	padding: 2rem;
	background: rgba(255, 255, 255, 0.1);
	border-radius: 12px;
	backdrop-filter: blur(10px);
}

.userInfo {
	display: flex;
	align-items: center;
	gap: 12px;
}

.userAvatar {
	width: 40px;
	height: 40px;
	display: flex;
	align-items: center;
	justify-content: center;
	background: #6366f1;
	border-radius: 50%;
	font-size: 20px;
	flex-shrink: 0;
}

.userDetails {
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.userName {
	font-size: 14px;
	font-weight: 600;
	color: #e4e4e7;
}

.userEmail {
	font-size: 12px;
	color: #71717a;
}

.logoutButton {
	padding: 10px 20px;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 8px;
	color: #f87171;
	font-size: 14px;
	font-weight: 600;
	cursor: pointer;
	transition: all 0.2s;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 8px;
}

.logoutButton:hover {
	background: rgba(239, 68, 68, 0.1);
	border-color: #ef4444;
}
</style>
