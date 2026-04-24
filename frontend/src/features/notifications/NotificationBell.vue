<template>
	<div class="notif-bell" ref="rootRef">
		<button
			type="button"
			class="notif-bell__btn"
			:class="{ 'notif-bell__btn--open': open }"
			title="Уведомления"
			aria-label="Уведомления"
			@click="toggle"
		>
			<svg viewBox="0 0 24 24" width="22" height="22" fill="currentColor" aria-hidden="true">
				<path
					d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 1.99 2zM18 16v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"
				/>
			</svg>
			<span v-if="unreadCount > 0" class="notif-bell__badge">{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
		</button>
		<div v-if="open" class="notif-bell__panel" role="dialog" aria-label="Уведомления">
			<div v-if="loading" class="notif-bell__muted">Загрузка…</div>
			<div v-else-if="items.length === 0" class="notif-bell__muted">Нет уведомлений</div>
			<ul v-else class="notif-bell__list">
				<li
					v-for="n in items"
					:key="n.id"
					class="notif-bell__item"
					:class="{ 'notif-bell__item--unread': !n.readAt }"
				>
					<template v-if="n.type === 'collab_invite' && n.payload?.inviteId">
						<div class="notif-bell__text">
							<strong>{{ n.payload.inviterName || 'Пользователь' }}</strong>
							приглашает вас редактировать документ
							<span class="notif-bell__doc-name">«{{ n.payload.documentName || 'Документ' }}»</span>
						</div>
						<div class="notif-bell__actions">
							<button type="button" class="notif-bell__accept" @click="accept(n)">Принять</button>
							<button type="button" class="notif-bell__decline" @click="decline(n)">Отклонить</button>
						</div>
					</template>
					<template v-else>
						<div class="notif-bell__text">{{ n.type }}</div>
						<button
							v-if="!n.readAt"
							type="button"
							class="notif-bell__mark-read"
							@click="markReadOnly(n)"
						>
							Ок
						</button>
					</template>
				</li>
			</ul>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { onClickOutside } from '@vueuse/core';
import NotificationsAPI, { type NotificationListItem } from '@/shared/api/NotificationsAPI';
import CollabAPI from '@/shared/api/CollabAPI';

const emit = defineEmits<{
	accepted: [];
}>();

const open = ref(false);
const loading = ref(false);
const items = ref<NotificationListItem[]>([]);
const rootRef = ref<HTMLElement | null>(null);

const unreadCount = computed(() => items.value.filter((n) => !n.readAt).length);

let pollTimer: ReturnType<typeof setInterval> | null = null;

async function load() {
	loading.value = true;
	try {
		items.value = await NotificationsAPI.list();
	} catch (e) {
		console.error('notifications load', e);
	} finally {
		loading.value = false;
	}
}

function toggle() {
	open.value = !open.value;
	if (open.value) void load();
}

onClickOutside(rootRef, () => {
	open.value = false;
});

async function accept(n: NotificationListItem) {
	const inviteId = n.payload?.inviteId;
	if (!inviteId) return;
	try {
		await CollabAPI.acceptInvite(inviteId);
		await NotificationsAPI.markRead(n.id);
		emit('accepted');
		await load();
		open.value = false;
	} catch (e: any) {
		alert(e?.response?.data?.message || e?.message || 'Не удалось принять приглашение');
	}
}

async function decline(n: NotificationListItem) {
	const inviteId = n.payload?.inviteId;
	if (!inviteId) return;
	try {
		await CollabAPI.declineInvite(inviteId);
		await NotificationsAPI.markRead(n.id);
		await load();
	} catch (e: any) {
		alert(e?.response?.data?.message || e?.message || 'Не удалось отклонить');
	}
}

async function markReadOnly(n: NotificationListItem) {
	try {
		await NotificationsAPI.markRead(n.id);
		await load();
	} catch (e) {
		console.error(e);
	}
}

onMounted(() => {
	void load();
	pollTimer = setInterval(() => {
		if (!open.value) void load();
	}, 60_000);
});

onUnmounted(() => {
	if (pollTimer) clearInterval(pollTimer);
});
</script>

<style scoped>
.notif-bell {
	position: relative;
}

.notif-bell__btn {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 40px;
	height: 40px;
	padding: 0;
	border: none;
	border-radius: 8px;
	background: transparent;
	color: inherit;
	cursor: pointer;
}

.notif-bell__btn:hover,
.notif-bell__btn--open {
	background: rgba(127, 127, 127, 0.15);
}

.notif-bell__badge {
	position: absolute;
	top: 2px;
	right: 2px;
	min-width: 18px;
	height: 18px;
	padding: 0 4px;
	font-size: 11px;
	font-weight: 700;
	line-height: 18px;
	text-align: center;
	color: #fff;
	background: #c62828;
	border-radius: 9px;
}

.notif-bell__panel {
	position: absolute;
	right: 0;
	top: calc(100% + 6px);
	width: min(360px, 92vw);
	max-height: 70vh;
	overflow: auto;
	padding: 8px;
	background: var(--color-surface, #fff);
	border: 1px solid rgba(127, 127, 127, 0.25);
	border-radius: 10px;
	box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
	z-index: 200;
}

.notif-bell__muted {
	padding: 12px;
	color: #666;
	font-size: 14px;
}

.notif-bell__list {
	list-style: none;
	margin: 0;
	padding: 0;
}

.notif-bell__item {
	padding: 10px 8px;
	border-bottom: 1px solid rgba(127, 127, 127, 0.15);
	font-size: 14px;
}

.notif-bell__item:last-child {
	border-bottom: none;
}

.notif-bell__item--unread {
	background: rgba(25, 118, 210, 0.06);
}

.notif-bell__doc-name {
	font-weight: 600;
}

.notif-bell__actions {
	display: flex;
	gap: 8px;
	margin-top: 8px;
}

.notif-bell__accept,
.notif-bell__decline,
.notif-bell__mark-read {
	padding: 6px 12px;
	border-radius: 6px;
	font-size: 13px;
	cursor: pointer;
	border: 1px solid transparent;
}

.notif-bell__accept {
	background: #1976d2;
	color: #fff;
}

.notif-bell__decline {
	background: transparent;
	border-color: rgba(0, 0, 0, 0.2);
}

.notif-bell__mark-read {
	background: rgba(0, 0, 0, 0.06);
}
</style>
