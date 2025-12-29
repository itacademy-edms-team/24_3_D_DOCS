<template>
	<div class="embedding-coverage-chart" :title="tooltipText">
		<svg
			class="chart-svg"
			viewBox="0 0 42 42"
			width="48"
			height="48"
		>
			<circle
				class="chart-background"
				cx="21"
				cy="21"
				r="15.91549430918954"
				fill="transparent"
				:stroke="backgroundColor"
				stroke-width="3"
			/>
			<circle
				class="chart-progress"
				cx="21"
				cy="21"
				r="15.91549430918954"
				fill="transparent"
				:stroke="progressColor"
				stroke-width="3"
				:stroke-dasharray="circumference"
				:stroke-dashoffset="offset"
				stroke-linecap="round"
				transform="rotate(-90 21 21)"
			/>
		</svg>
		<div class="chart-text">
			{{ Math.round(coveragePercentage) }}%
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

interface Props {
	coveragePercentage: number; // 0-100
}

const props = defineProps<Props>();

const circumference = 2 * Math.PI * 15.91549430918954; // radius * 2 * PI
const offset = computed(() => {
	const percentage = Math.max(0, Math.min(100, props.coveragePercentage));
	return circumference - (percentage / 100) * circumference;
});

const backgroundColor = computed(() => {
	return 'var(--embedding-chart-bg, #374151)'; // gray-700
});

const progressColor = computed(() => {
	if (props.coveragePercentage >= 100) {
		return 'var(--embedding-chart-success, #4ade80)'; // green-400
	}
	return 'var(--embedding-chart-progress, #60a5fa)'; // blue-400
});

const tooltipText = computed(() => {
	const percentage = Math.round(props.coveragePercentage);
	if (percentage >= 100) {
		return `Покрыто: ${percentage}%`;
	}
	return `Покрыто: ${percentage}%`;
});
</script>

<style scoped>
.embedding-coverage-chart {
	position: relative;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	cursor: pointer;
	width: 48px;
	height: 48px;
}

.chart-svg {
	width: 48px;
	height: 48px;
	transform: rotate(-90deg);
}

.chart-background {
	transition: stroke 0.2s ease;
}

.chart-progress {
	transition: stroke-dashoffset 0.3s ease, stroke 0.2s ease;
}

.chart-text {
	position: absolute;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	font-size: 11px;
	font-weight: 600;
	color: var(--text-primary, #e4e4e7);
	pointer-events: none;
	user-select: none;
}
</style>
