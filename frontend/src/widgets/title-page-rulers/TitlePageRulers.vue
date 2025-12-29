<template>
	<div class="title-page-rulers">
		<div class="title-page-rulers__corner"></div>
		<div 
			class="title-page-rulers__horizontal"
			:style="{ width: `${width}px` }"
		>
			<div
				v-for="mark in horizontalMarks"
				:key="mark.value"
				class="title-page-rulers__mark"
				:style="{ left: `${mark.position}px` }"
			>
				<span v-if="mark.isMajor">{{ mark.value }}</span>
			</div>
			<div
				v-if="mousePos"
				class="title-page-rulers__indicator"
				:style="{ left: `${mousePos.x * MM_TO_PX}px` }"
			></div>
		</div>
		<div 
			class="title-page-rulers__vertical"
			:style="{ height: `${height}px` }"
		>
			<div
				v-for="mark in verticalMarks"
				:key="mark.value"
				class="title-page-rulers__mark"
				:style="{ top: `${mark.position}px` }"
			>
				<span v-if="mark.isMajor">{{ mark.value }}</span>
			</div>
			<div
				v-if="mousePos"
				class="title-page-rulers__indicator"
				:style="{ top: `${mousePos.y * MM_TO_PX}px` }"
			></div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { MM_TO_PX } from '@/entities/title-page/constants';
import type { RulerMark, MousePosition } from '@/entities/title-page/constants';

interface Props {
	width: number;
	height: number;
	horizontalMarks: RulerMark[];
	verticalMarks: RulerMark[];
	mousePos: MousePosition | null;
}

defineProps<Props>();
</script>

<style scoped>
.title-page-rulers {
	position: relative;
	display: inline-block;
}

.title-page-rulers__corner {
	position: absolute;
	top: 0;
	left: 0;
	width: 20px;
	height: 20px;
	background: var(--bg-secondary);
	border-right: 1px solid var(--border-color);
	border-bottom: 1px solid var(--border-color);
	z-index: 2;
}

.title-page-rulers__horizontal {
	position: relative;
	height: 20px;
	background: var(--bg-secondary);
	border-bottom: 1px solid var(--border-color);
	margin-left: 20px;
	z-index: 1;
}

.title-page-rulers__vertical {
	position: absolute;
	top: 20px;
	left: 0;
	width: 20px;
	background: var(--bg-secondary);
	border-right: 1px solid var(--border-color);
	z-index: 1;
}

.title-page-rulers__mark {
	position: absolute;
	font-size: 9px;
	color: var(--text-secondary);
	pointer-events: none;
	white-space: nowrap;
}

.title-page-rulers__horizontal .title-page-rulers__mark {
	top: 2px;
	transform: translateX(-50%);
}

.title-page-rulers__vertical .title-page-rulers__mark {
	left: 2px;
	transform: translateY(-50%) rotate(-90deg);
	transform-origin: center;
}

.title-page-rulers__indicator {
	position: absolute;
	background: #0066ff;
	pointer-events: none;
	z-index: 10;
}

.title-page-rulers__horizontal .title-page-rulers__indicator {
	width: 1px;
	height: 100%;
	top: 0;
}

.title-page-rulers__vertical .title-page-rulers__indicator {
	height: 1px;
	width: 100%;
	left: 0;
}
</style>
