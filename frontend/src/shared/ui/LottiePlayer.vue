<template>
	<div :class="['lottie-player', { 'lottie--disabled': disabled }]" :style="playerStyle" ref="container" role="img" :aria-label="ariaLabel">
		<div v-if="disabled" class="lottie-fallback">
			<!-- simple CSS dots fallback -->
			<span class="dot"></span><span class="dot"></span><span class="dot"></span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';

interface Props {
	src: string;
	loop?: boolean;
	autoplay?: boolean;
	width?: number | string;
	height?: number | string;
	ariaLabel?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
	loaded: [];
	completed: [];
	error: [err: any];
}>();

const container = ref<HTMLElement | null>(null);
let animationInstance: any = null;
const disabled = ref(false);

const playerStyle = computed(() => {
	return {
		width: props.width ? (typeof props.width === 'number' ? `${props.width}px` : props.width) : '48px',
		height: props.height ? (typeof props.height === 'number' ? `${props.height}px` : props.height) : '48px'
	};
});

const ariaLabel = props.ariaLabel ?? 'Animation';

async function ensureLottie(): Promise<any> {
	// Use global lottie if already present
	if ((window as any).lottie) return (window as any).lottie;

	// Dynamically load lottie-web from CDN
	return new Promise((resolve, reject) => {
		const script = document.createElement('script');
		script.src = 'https://cdnjs.cloudflare.com/ajax/libs/bodymovin/5.10.2/lottie.min.js';
		script.async = true;
		script.onload = () => {
			if ((window as any).lottie) resolve((window as any).lottie);
			else reject(new Error('lottie failed to initialize'));
		};
		script.onerror = (e) => reject(e);
		document.head.appendChild(script);
	});
}

async function mountAnimation() {
	if (!container.value) return;
	try {
		const lottie = await ensureLottie();
		if (!props.src) {
			disabled.value = true;
			return;
		}
		animationInstance = lottie.loadAnimation({
			container: container.value,
			renderer: 'svg',
			loop: props.loop ?? true,
			autoplay: props.autoplay ?? true,
			path: props.src
		});
		animationInstance.addEventListener && animationInstance.addEventListener('DOMLoaded', () => {
			emit('loaded');
		});
		animationInstance.addEventListener && animationInstance.addEventListener('complete', () => {
			emit('completed');
		});
	} catch (err) {
		disabled.value = true;
		emit('error', err);
	}
}

onMounted(() => {
	mountAnimation();
});

onBeforeUnmount(() => {
	if (animationInstance && animationInstance.destroy) {
		animationInstance.destroy();
		animationInstance = null;
	}
});

watch(() => props.src, async () => {
	if (animationInstance && animationInstance.destroy) {
		animationInstance.destroy();
		animationInstance = null;
	}
	disabled.value = false;
	await mountAnimation();
});
</script>

<style scoped>
.lottie-player {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	overflow: hidden;
}
.lottie--disabled .lottie-fallback {
	display: inline-flex;
	gap: 6px;
	align-items: center;
}
.lottie-fallback .dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: var(--typing-indicator);
	animation: fallbackBounce 1s infinite ease-in-out;
}
.lottie-fallback .dot:nth-child(2) { animation-delay: 0.1s; }
.lottie-fallback .dot:nth-child(3) { animation-delay: 0.2s; }
@keyframes fallbackBounce {
	0%, 80%, 100% { transform: translateY(0); opacity: 0.5; }
	40% { transform: translateY(-6px); opacity: 1; }
}
</style>

