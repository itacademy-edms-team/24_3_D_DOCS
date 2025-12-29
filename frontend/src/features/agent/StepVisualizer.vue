<template>
	<!-- If compact and not key step -> render minimal inline text -->
	<div v-if="compact && !isKey" class="step-mini" :class="{ 'step-mini--active': isActive }" @click="toggle" tabindex="0" role="button" :aria-expanded="expanded">
		<span class="step-mini__text">{{ step.description || summaryText || ('Шаг ' + step.stepNumber) }}</span>
	</div>

	<!-- Full visualizer card for key steps or expanded view -->
	<div v-else :class="['step-visualizer', { 'step-visualizer--key': isKey, 'step-visualizer--expanded': expanded }]" @keydown.enter.prevent="toggle" tabindex="0" role="button" :aria-expanded="expanded">
		<div class="step-visualizer__header" @click="toggle">
			<LottiePlayer :src="animationSrc" :width="40" :height="40" :loop="!expanded" aria-label="step animation" />
			<div class="step-visualizer__meta">
				<div class="step-visualizer__title">
					<span class="step-visualizer__number">Шаг {{ step.stepNumber }}</span>
					<span class="step-visualizer__desc">{{ step.description }}</span>
				</div>
				<div v-if="compact && summaryText" class="step-visualizer__summary">{{ summaryText }}</div>
			</div>
			<button class="step-visualizer__toggle" @click.stop="toggle" :aria-label="expanded ? 'Свернуть' : 'Развернуть'">
				{{ expanded ? '▾' : '▸' }}
			</button>
		</div>

		<div v-if="expanded" class="step-visualizer__body">
			<!-- details: tool calls, results -->
			<div v-if="step.toolCalls && step.toolCalls.length" class="step-visualizer__tool-calls">
				<div v-for="(tc, idx) in step.toolCalls" :key="idx" class="step-visualizer__tool-call">
					<strong>{{ tc.toolName }}</strong>
					<div v-if="tc.result" class="step-visualizer__tool-result">{{ tc.result }}</div>
				</div>
			</div>
			<!-- accept/reject controls are emitted up -->
			<div v-if="hasDocChanges" class="step-visualizer__actions">
				<button class="step-visualizer__btn accept" @click.stop="accept">Применить</button>
				<button class="step-visualizer__btn reject" @click.stop="reject">Отменить</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import LottiePlayer from '@/shared/ui/LottiePlayer.vue';

interface Step {
	stepNumber: number;
	description: string;
	toolCalls?: Array<{ toolName: string; result?: string }>;
	isKey?: boolean;
	summary?: string;
}

const props = defineProps<{ step: Step; compact?: boolean; currentStepNumber?: number }>();
const emit = defineEmits<{
	toggle: [stepNumber: number, expanded: boolean];
	accept: [stepNumber: number];
	reject: [stepNumber: number];
}>();

const expanded = ref(false);
const compact = props.compact ?? true;

const isKey = computed(() => {
	return Boolean(props.step.isKey || props.step.summary);
});

const summaryText = computed(() => {
	return props.step.summary ?? '';
});

const hasDocChanges = computed(() => {
	return !!(props.step.toolCalls && props.step.toolCalls.some(tc => ['insert','edit','delete'].includes(tc.toolName)));
});

const animationSrc = computed(() => {
	// heuristics: if step has 'summary' show summary animation, else working
	if (props.step.summary) return '/lottie/summary.json';
	return '/lottie/working.json';
});

function toggle() {
	expanded.value = !expanded.value;
	emit('toggle', props.step.stepNumber, expanded.value);
}

function accept() {
	emit('accept', props.step.stepNumber);
}

function reject() {
	emit('reject', props.step.stepNumber);
}
const isActive = computed(() => props.currentStepNumber === props.step?.stepNumber);
</script>

<style scoped>
.step-visualizer {
	display: flex;
	flex-direction: column;
	border: 1px solid var(--border-color);
	border-radius: var(--radius-sm);
	padding: var(--spacing-sm);
	background: var(--bg-primary);
}
.step-visualizer__header {
	display: flex;
	align-items: center;
	gap: var(--spacing-sm);
	cursor: pointer;
}
.step-visualizer__meta {
	flex: 1;
	min-width: 0;
}
.step-visualizer__title {
	display: flex;
	gap: var(--spacing-xs);
	align-items: baseline;
	font-weight: 600;
}
.step-visualizer__summary {
	color: var(--text-secondary);
	font-size: 0.95rem;
	margin-top: 4px;
}
.step-visualizer__toggle {
	background: transparent;
	border: none;
	cursor: pointer;
	font-size: 14px;
}
.step-visualizer__body {
	margin-top: var(--spacing-sm);
	display: flex;
	flex-direction: column;
	gap: var(--spacing-sm);
}
.step-visualizer__tool-call {
	padding: var(--spacing-xs);
	background: var(--bg-secondary);
	border-radius: var(--radius-sm);
}
.step-visualizer__actions {
	display: flex;
	gap: var(--spacing-xs);
}
.step-visualizer__btn {
	padding: var(--spacing-xs) var(--spacing-md);
	border-radius: var(--radius-sm);
	border: 1px solid var(--border-color);
	cursor: pointer;
}
.step-visualizer__btn.accept {
	background: rgba(34,197,94,0.08);
	color: rgb(34,197,94);
}
.step-visualizer__btn.reject {
	background: rgba(239,68,68,0.08);
	color: rgb(239,68,68);
}
.step-visualizer--key {
	box-shadow: var(--shadow-md);
	border-color: var(--accent);
}
.step-mini {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	padding: 2px 6px;
	color: var(--text-secondary);
	font-size: 0.9rem;
	cursor: pointer;
	border-radius: 9999px;
	transition: transform 0.18s ease, opacity 0.18s ease;
}
.step-mini--active {
	color: var(--text-primary);
	animation: pulseMini 1.2s infinite ease-in-out;
	transform: translateY(-1px);
	opacity: 1;
}
@keyframes pulseMini {
	0% { transform: scale(1); opacity: 0.85; }
	50% { transform: scale(1.03); opacity: 1; }
	100% { transform: scale(1); opacity: 0.85; }
}
</style>

