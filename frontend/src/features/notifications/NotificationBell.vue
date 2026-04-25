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
			<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
				<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
				<path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
			</svg>
			<span v-if="unreadCount > 0" class="notif-bell__badge">{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
		</button>
		<Teleport to="body">
			<div
				v-if="open"
				ref="panelRef"
				class="notif-bell__panel"
				:style="panelStyle"
				role="dialog"
				aria-label="Уведомления"
			>
				<div class="notif-bell__panel-head">
					<span class="notif-bell__panel-title">Уведомления</span>
					<button
						v-if="items.length > 0 && !loading"
						type="button"
						class="notif-bell__clear"
						:disabled="clearing"
						@click="clearAll"
					>
						{{ clearing ? '…' : 'Очистить' }}
					</button>
				</div>
				<div v-if="loading" class="notif-bell__muted">Загрузка…</div>
				<div v-else-if="items.length === 0" class="notif-bell__muted notif-bell__muted--empty">Нет уведомлений</div>
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
		</Teleport>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { onClickOutside } from '@vueuse/core';
import NotificationsAPI, { type NotificationListItem } from '@/shared/api/NotificationsAPI';
import CollabAPI from '@/shared/api/CollabAPI';

const emit = defineEmits<{
	accepted: [];
}>();

const open = ref(false);
const loading = ref(false);
const clearing = ref(false);
const items = ref<NotificationListItem[]>([]);
const rootRef = ref<HTMLElement | null>(null);
const panelRef = ref<HTMLElement | null>(null);
const panelStyle = ref<Record<string, string>>({});

const unreadCount = computed(() => items.value.filter((n) => !n.readAt).length);

let pollTimer: ReturnType<typeof setInterval> | null = null;

function updatePanelPosition() {
	const root = rootRef.value;
	if (!root) return;
	const r = root.getBoundingClientRect();
	const gap = 8;
	const maxW = 360;
	const panelW = Math.min(maxW, Math.max(280, window.innerWidth - gap * 2));
	const rightFromViewport = window.innerWidth - r.right;
	const right = Math.max(gap, rightFromViewport);
	panelStyle.value = {
		position: 'fixed',
		top: `${Math.round(r.bottom + gap)}px`,
		right: `${Math.round(right)}px`,
		width: `${Math.round(panelW)}px`,
		zIndex: '10050',
	};
}

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
	if (open.value) {
		void load();
	}
}

onClickOutside(rootRef, (ev) => {
	const el = panelRef.value;
	if (el && ev.target instanceof Node && el.contains(ev.target)) return;
	open.value = false;
});

watch(open, (v) => {
	if (v) {
		void nextTick(() => {
			updatePanelPosition();
			window.addEventListener('scroll', updatePanelPosition, true);
			window.addEventListener('resize', updatePanelPosition);
		});
	} else {
		window.removeEventListener('scroll', updatePanelPosition, true);
		window.removeEventListener('resize', updatePanelPosition);
	}
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

async function clearAll() {
	if (!items.value.length) return;
	if (!confirm('Удалить все уведомления?')) return;
	clearing.value = true;
	try {
		await NotificationsAPI.clearAll();
		items.value = [];
		open.value = false;
	} catch (e) {
		console.error(e);
		alert('Не удалось очистить уведомления');
	} finally {
		clearing.value = false;
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
	window.removeEventListener('scroll', updatePanelPosition, true);
	window.removeEventListener('resize', updatePanelPosition);
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
	border: 1px solid transparent;
	border-radius: var(--radius-md, 8px);
	background: transparent;
	color: var(--text-secondary, #71717a);
	cursor: pointer;
	transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.notif-bell__btn:hover,
.notif-bell__btn--open {
	background: var(--bg-secondary, #f4f4f5);
	color: var(--text-primary, #18181b);
	border-color: var(--border-color, #e4e4e7);
}

.notif-bell__badge {
	position: absolute;
	top: 4px;
	right: 4px;
	min-width: 16px;
	height: 16px;
	padding: 0 4px;
	font-size: 10px;
	font-weight: 700;
	line-height: 16px;
	text-align: center;
	color: #fff;
	background: var(--danger, #ef4444);
	border-radius: 999px;
}

.notif-bell__panel {
	overflow: hidden;
	display: flex;
	flex-direction: column;
	padding: 0;
	background: var(--bg-primary, #fff);
	border: 1px solid var(--border-color, #e4e4e7);
	border-radius: var(--radius-md, 8px);
	box-shadow: var(--shadow-lg, 0 10px 15px -3px rgba(0, 0, 0, 0.1));
}

.notif-bell__panel-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: var(--spacing-sm, 0.5rem);
	padding: var(--spacing-sm, 0.5rem) var(--spacing-md, 1rem);
	border-bottom: 1px solid var(--border-color, #e4e4e7);
	background: var(--bg-secondary, #f4f4f5);
	flex-shrink: 0;
}

.notif-bell__panel-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--text-primary, #18181b);
	letter-spacing: 0.02em;
}

.notif-bell__clear {
	padding: 4px 10px;
	font-size: 12px;
	font-weight: 500;
	color: var(--text-secondary, #71717a);
	background: var(--bg-primary, #fff);
	border: 1px solid var(--border-color, #e4e4e7);
	border-radius: var(--radius-sm, 6px);
	cursor: pointer;
	transition: color 0.15s ease, border-color 0.15s ease, background 0.15s ease;
}

.notif-bell__clear:hover:not(:disabled) {
	color: var(--danger, #ef4444);
	border-color: var(--danger, #ef4444);
	background: rgba(239, 68, 68, 0.06);
}

.notif-bell__clear:disabled {
	opacity: 0.6;
	cursor: not-allowed;
}

.notif-bell__muted {
	padding: var(--spacing-md, 1rem);
	color: var(--text-secondary, #71717a);
	font-size: 13px;
}

.notif-bell__muted--empty {
	text-align: center;
	padding: var(--spacing-lg, 1.5rem);
}

.notif-bell__list {
	list-style: none;
	margin: 0;
	padding: 0;
	max-height: min(320px, calc(100dvh - 120px));
	overflow-y: auto;
	overscroll-behavior: contain;
}

.notif-bell__item {
	padding: var(--spacing-md, 1rem);
	border-bottom: 1px solid var(--border-color, #e4e4e7);
	font-size: 13px;
	line-height: 1.45;
	color: var(--text-primary, #18181b);
}

.notif-bell__item:last-child {
	border-bottom: none;
}

.notif-bell__item--unread {
	background: var(--accent-light, #e0f2fe);
}

.notif-bell__doc-name {
	font-weight: 600;
	color: var(--accent, #2563eb);
}

.notif-bell__actions {
	display: flex;
	gap: var(--spacing-sm, 0.5rem);
	margin-top: var(--spacing-sm, 0.5rem);
}

.notif-bell__accept,
.notif-bell__decline,
.notif-bell__mark-read {
	padding: 6px 12px;
	border-radius: var(--radius-sm, 6px);
	font-size: 12px;
	font-weight: 500;
	cursor: pointer;
	border: 1px solid transparent;
	transition: background 0.15s ease, border-color 0.15s ease;
}

.notif-bell__accept {
	background: var(--accent, #2563eb);
	color: #fff;
}

.notif-bell__accept:hover {
	background: var(--accent-hover, #1d4ed8);
}

.notif-bell__decline {
	background: var(--bg-primary, #fff);
	border-color: var(--border-color, #e4e4e7);
	color: var(--text-primary, #18181b);
}

.notif-bell__decline:hover {
	background: var(--bg-secondary, #f4f4f5);
}

.notif-bell__mark-read {
	background: var(--bg-secondary, #f4f4f5);
	border-color: var(--border-color, #e4e4e7);
	color: var(--text-secondary, #71717a);
}

.notif-bell__mark-read:hover {
	background: var(--bg-tertiary, #e4e4e7);
}
</style>
