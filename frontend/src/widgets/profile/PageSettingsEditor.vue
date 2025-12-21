<template>
	<div class="page-settings-card">
		<h3 class="card-title">Настройки страницы</h3>

		<div class="form-group">
			<label class="form-label">Размер</label>
			<select class="form-select" :value="pageSettings.size" @change="handleSizeChange">
				<option value="A4">A4</option>
				<option value="A5">A5</option>
				<option value="Letter">Letter</option>
			</select>
		</div>

		<div class="form-group">
			<label class="form-label">Ориентация</label>
			<select
				class="form-select"
				:value="pageSettings.orientation"
				@change="handleOrientationChange"
			>
				<option value="portrait">Книжная</option>
				<option value="landscape">Альбомная</option>
			</select>
		</div>

		<div class="form-group">
			<label class="form-label">Поля (мм)</label>
			<div class="form-row">
				<div class="form-group-small">
					<label class="form-label-small">Сверху</label>
					<input
						type="number"
						class="form-input form-input-sm"
						:value="pageSettings.margins.top"
						@input="handleMarginChange('top', $event)"
						min="0"
						max="100"
					/>
				</div>
				<div class="form-group-small">
					<label class="form-label-small">Снизу</label>
					<input
						type="number"
						class="form-input form-input-sm"
						:value="pageSettings.margins.bottom"
						@input="handleMarginChange('bottom', $event)"
						min="0"
						max="100"
					/>
				</div>
				<div class="form-group-small">
					<label class="form-label-small">Слева</label>
					<input
						type="number"
						class="form-input form-input-sm"
						:value="pageSettings.margins.left"
						@input="handleMarginChange('left', $event)"
						min="0"
						max="100"
					/>
				</div>
				<div class="form-group-small">
					<label class="form-label-small">Справа</label>
					<input
						type="number"
						class="form-input form-input-sm"
						:value="pageSettings.margins.right"
						@input="handleMarginChange('right', $event)"
						min="0"
						max="100"
					/>
				</div>
			</div>
		</div>

		<div class="form-group">
			<label class="form-label checkbox-label">
				<input
					type="checkbox"
					:checked="pageSettings.pageNumbers?.enabled || false"
					@change="handlePageNumbersToggle"
				/>
				Нумерация страниц
			</label>
		</div>

		<template v-if="pageSettings.pageNumbers?.enabled">
			<div class="form-group">
				<label class="form-label">Позиция</label>
				<select
					class="form-select form-select-sm"
					:value="pageSettings.pageNumbers?.position || 'bottom'"
					@change="handlePageNumberSettingChange('position', $event)"
				>
					<option value="top">Сверху</option>
					<option value="bottom">Снизу</option>
				</select>
			</div>

			<div class="form-group">
				<label class="form-label">Выравнивание</label>
				<select
					class="form-select form-select-sm"
					:value="pageSettings.pageNumbers?.align || 'center'"
					@change="handlePageNumberSettingChange('align', $event)"
				>
					<option value="left">По левому краю</option>
					<option value="center">По центру</option>
					<option value="right">По правому краю</option>
				</select>
			</div>

			<div class="form-group">
				<label class="form-label">Формат</label>
				<input
					type="text"
					class="form-input form-input-sm"
					:value="pageSettings.pageNumbers?.format || '{n}'"
					@input="handlePageNumberSettingChange('format', $event)"
					placeholder="{n}, Страница {n}, {n} из {total}"
				/>
			</div>
		</template>
	</div>
</template>

<script setup lang="ts">
import type { Profile } from '@/entities/profile/types';

interface Props {
	pageSettings: Profile['page'];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'page-setting-change': [key: keyof Profile['page'], value: any];
	'margin-change': [side: 'top' | 'right' | 'bottom' | 'left', value: number];
}>();

function handleSizeChange(event: Event) {
	const target = event.target as HTMLSelectElement;
	emit('page-setting-change', 'size', target.value);
}

function handleOrientationChange(event: Event) {
	const target = event.target as HTMLSelectElement;
	emit('page-setting-change', 'orientation', target.value as 'portrait' | 'landscape');
}

function handleMarginChange(side: 'top' | 'right' | 'bottom' | 'left', event: Event) {
	const target = event.target as HTMLInputElement;
	const value = parseInt(target.value) || 0;
	emit('margin-change', side, value);
}

function handlePageNumbersToggle(event: Event) {
	const target = event.target as HTMLInputElement;
	const currentPageNumbers = props.pageSettings.pageNumbers || {
		enabled: false,
		position: 'bottom',
		align: 'center',
		format: '{n}',
	};
	emit('page-setting-change', 'pageNumbers', {
		...currentPageNumbers,
		enabled: target.checked,
	});
}

function handlePageNumberSettingChange(key: string, event: Event) {
	const target = event.target as HTMLInputElement | HTMLSelectElement;
	const currentPageNumbers = props.pageSettings.pageNumbers || {
		enabled: true,
		position: 'bottom',
		align: 'center',
		format: '{n}',
	};
	emit('page-setting-change', 'pageNumbers', {
		...currentPageNumbers,
		[key]: target.value,
	});
}
</script>

<style scoped>
.page-settings-card {
	background: #18181b;
	border: 1px solid #27272a;
	border-radius: 8px;
	padding: 1.5rem;
	margin-bottom: 1.5rem;
}

.card-title {
	font-size: 16px;
	font-weight: 600;
	color: #e4e4e7;
	margin: 0 0 1rem 0;
}

.form-group {
	margin-bottom: 1rem;
}

.form-group:last-child {
	margin-bottom: 0;
}

.form-label {
	display: block;
	font-size: 13px;
	font-weight: 500;
	color: #a1a1aa;
	margin-bottom: 0.5rem;
}

.form-label-small {
	display: block;
	font-size: 11px;
	font-weight: 500;
	color: #71717a;
	margin-bottom: 0.25rem;
}

.checkbox-label {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	cursor: pointer;
}

.checkbox-label input[type='checkbox'] {
	width: 16px;
	height: 16px;
	cursor: pointer;
	accent-color: #6366f1;
}

.form-row {
	display: grid;
	grid-template-columns: repeat(4, 1fr);
	gap: 0.75rem;
}

.form-group-small {
	display: flex;
	flex-direction: column;
}

.form-select,
.form-input {
	width: 100%;
	padding: 0.5rem 0.75rem;
	background: #0a0a0a;
	border: 1px solid #27272a;
	border-radius: 6px;
	color: #e4e4e7;
	font-size: 14px;
	outline: none;
	transition: border-color 0.2s;
	font-family: inherit;
}

.form-select-sm,
.form-input-sm {
	padding: 0.4rem 0.6rem;
	font-size: 13px;
}

.form-select:focus,
.form-input:focus {
	border-color: #6366f1;
}

.form-select option {
	background: #18181b;
	color: #e4e4e7;
}

.form-input::placeholder {
	color: #71717a;
}
</style>
