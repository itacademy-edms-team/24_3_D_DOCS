<template>
	<Teleport to="body">
		<Transition name="modal">
			<div v-if="modelValue" class="modal" @click.self="handleBackdropClick">
				<div class="modal__overlay" />
				<div class="modal__container" :class="`modal__container--${size}`">
					<div class="modal__header" v-if="title || $slots.header">
						<slot name="header">
							<h2 class="modal__title">{{ title }}</h2>
						</slot>
						<button
							v-if="closable"
							class="modal__close"
							@click="handleClose"
							aria-label="Закрыть"
						>
							×
						</button>
					</div>
					<div class="modal__body">
						<slot />
					</div>
					<div class="modal__footer" v-if="$slots.footer">
						<slot name="footer" />
					</div>
				</div>
			</div>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
interface Props {
	modelValue: boolean;
	title?: string;
	size?: 'sm' | 'md' | 'lg' | 'xl';
	closable?: boolean;
	closeOnBackdrop?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	size: 'md',
	closable: true,
	closeOnBackdrop: true,
});

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
}>();

const handleClose = () => {
	emit('update:modelValue', false);
};

const handleBackdropClick = () => {
	if (props.closeOnBackdrop) {
		handleClose();
	}
};
</script>

<style scoped>
.modal {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	z-index: 1000;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: var(--spacing-lg);
}

.modal__overlay {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: rgba(0, 0, 0, 0.5);
	backdrop-filter: blur(4px);
}

.modal__container {
	position: relative;
	background: var(--bg-primary);
	border-radius: var(--radius-lg);
	box-shadow: var(--shadow-lg);
	max-height: 90vh;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	z-index: 1;
	width: 100%;
}

.modal__container--sm {
	max-width: 400px;
}

.modal__container--md {
	max-width: 600px;
}

.modal__container--lg {
	max-width: 900px;
}

.modal__container--xl {
	max-width: 1200px;
}

.modal__header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: var(--spacing-lg);
	border-bottom: 1px solid var(--border-color);
}

.modal__title {
	margin: 0;
	font-size: 20px;
	font-weight: 600;
	color: var(--text-primary);
}

.modal__close {
	width: 32px;
	height: 32px;
	border-radius: var(--radius-sm);
	background: transparent;
	border: none;
	font-size: 24px;
	color: var(--text-secondary);
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	transition: all 0.2s ease;
	line-height: 1;
}

.modal__close:hover {
	background: var(--bg-secondary);
	color: var(--text-primary);
}

.modal__body {
	flex: 1;
	padding: var(--spacing-lg);
	overflow-y: auto;
}

.modal__footer {
	padding: var(--spacing-lg);
	border-top: 1px solid var(--border-color);
	display: flex;
	justify-content: flex-end;
	gap: var(--spacing-sm);
}

/* Transitions */
.modal-enter-active,
.modal-leave-active {
	transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
	opacity: 0;
}

.modal-enter-active .modal__container,
.modal-leave-active .modal__container {
	transition: transform 0.3s ease, opacity 0.3s ease;
}

.modal-enter-from .modal__container,
.modal-leave-to .modal__container {
	opacity: 0;
	transform: scale(0.95);
}
</style>
