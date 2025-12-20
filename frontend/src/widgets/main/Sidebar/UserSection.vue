<template>
	<div class="user-section">
		<div class="user-info">
			<div class="user-avatar">{{ userInitials }}</div>
			<div class="user-details">
				<span class="user-name">{{ user?.name || 'Пользователь' }}</span>
				<span class="user-email">{{ user?.email }}</span>
			</div>
		</div>
		<div class="user-actions">
			<button class="user-action-btn" @click="$emit('settings')">
				Настройки
			</button>
			<button class="user-action-btn logout" @click="$emit('logout')">
				Выход
			</button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '@/entities/auth/store/authStore';
import { getUserInitials } from '@/shared/utils/user';

defineEmits<{
	logout: [];
	settings: [];
}>();

const authStore = useAuthStore();
const user = computed(() => authStore.user);
const userInitials = computed(() => getUserInitials(user.value));
</script>

<style scoped>
.user-section {
	margin-top: auto;
	padding-top: 1.5rem;
	border-top: 1px solid #27272a;
}

.user-info {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	margin-bottom: 1rem;
}

.user-avatar {
	width: 36px;
	height: 36px;
	border-radius: 50%;
	background: #6366f1;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	font-weight: 600;
	color: white;
	flex-shrink: 0;
}

.user-details {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0;
	gap: 2px;
}

.user-name {
	font-size: 14px;
	color: #e4e4e7;
	font-weight: 500;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.user-email {
	font-size: 12px;
	color: #a1a1aa;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.user-actions {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.user-action-btn {
	padding: 0.5rem 1rem;
	background: transparent;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #a1a1aa;
	font-size: 13px;
	cursor: pointer;
	text-align: left;
	transition: all 0.2s;
}

.user-action-btn:hover {
	background: #27272a;
	color: #e4e4e7;
}

.user-action-btn.logout {
	color: #f87171;
	border-color: #27272a;
}

.user-action-btn.logout:hover {
	background: rgba(248, 113, 113, 0.1);
	border-color: #ef4444;
}
</style>
