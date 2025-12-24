<template>
	<div
		:class="['ruler', orientation]"
		:style="rulerStyle"
	>
		<div
			v-for="mark in marks"
			:key="mark.value"
			:class="['mark', { major: mark.isMajor }]"
			:style="getMarkStyle(mark)"
		/>
		<div
			v-for="label in labels"
			:key="`label-${label.value}`"
			class="label"
			:style="getLabelStyle(label)"
		>
			{{ label.value }}
		</div>
		<div
			v-if="mousePos"
			class="indicator"
			:style="getIndicatorStyle()"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { mmToPx, PAGE_WIDTH_MM, PAGE_HEIGHT_MM } from '@/shared/utils/canvasUtils';

interface Props {
	orientation: 'horizontal' | 'vertical';
	mousePos?: { x: number; y: number } | null;
}

const props = defineProps<Props>();

const length = computed(() =>
	props.orientation === 'horizontal' ? PAGE_WIDTH_MM : PAGE_HEIGHT_MM
);

const step = computed(() => {
	if (length.value > 200) return 10;
	if (length.value > 100) return 5;
	return 1;
});

const marks = computed(() => {
	const result: Array<{ value: number; isMajor: boolean }> = [];
	for (let i = 0; i <= length.value; i += step.value) {
		result.push({
			value: i,
			isMajor: i % (step.value * 5) === 0,
		});
	}
	return result;
});

const labels = computed(() =>
	marks.value.filter((m) => m.isMajor)
);

const rulerStyle = computed(() => {
	if (props.orientation === 'horizontal') {
		return {
			position: 'absolute',
			top: '-20px',
			left: '0',
			width: `${mmToPx(PAGE_WIDTH_MM)}px`,
			height: '20px',
			background: '#27272a',
			borderBottom: '1px solid #3f3f46',
		};
	} else {
		return {
			position: 'absolute',
			top: '0',
			left: '-20px',
			width: '20px',
			height: `${mmToPx(PAGE_HEIGHT_MM)}px`,
			background: '#27272a',
			borderRight: '1px solid #3f3f46',
		};
	}
});

function getMarkStyle(mark: { value: number; isMajor: boolean }) {
	const size = mark.isMajor ? 15 : 8;
	if (props.orientation === 'horizontal') {
		return {
			position: 'absolute',
			left: `${mmToPx(mark.value)}px`,
			top: '0',
			width: '1px',
			height: `${size}px`,
			background: '#666',
		};
	} else {
		return {
			position: 'absolute',
			top: `${mmToPx(mark.value)}px`,
			left: '0',
			height: '1px',
			width: `${size}px`,
			background: '#666',
		};
	}
}

function getLabelStyle(label: { value: number }) {
	if (props.orientation === 'horizontal') {
		return {
			position: 'absolute',
			left: `${mmToPx(label.value) + 2}px`,
			top: '2px',
			fontSize: '10px',
			color: '#a1a1aa',
			pointerEvents: 'none',
		};
	} else {
		return {
			position: 'absolute',
			top: `${mmToPx(label.value) + 2}px`,
			left: '2px',
			fontSize: '10px',
			color: '#a1a1aa',
			pointerEvents: 'none',
			transform: 'rotate(-90deg)',
			transformOrigin: 'top left',
		};
	}
}

function getIndicatorStyle() {
	if (props.orientation === 'horizontal' && props.mousePos) {
		return {
			position: 'absolute',
			left: `${mmToPx(props.mousePos.x)}px`,
			top: '0',
			width: '1px',
			height: '20px',
			background: '#6366f1',
			pointerEvents: 'none',
		};
	} else if (props.orientation === 'vertical' && props.mousePos) {
		return {
			position: 'absolute',
			top: `${mmToPx(props.mousePos.y)}px`,
			left: '0',
			width: '20px',
			height: '1px',
			background: '#6366f1',
			pointerEvents: 'none',
		};
	}
	return {};
}
</script>

<style scoped>
.ruler {
	position: relative;
}

.mark {
	position: absolute;
}

.label {
	position: absolute;
	pointer-events: none;
}
</style>
