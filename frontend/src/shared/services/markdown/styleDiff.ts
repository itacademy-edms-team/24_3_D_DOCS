import type { EntityStyle } from './renderUtils';

export function computeStyleDelta(newStyle: EntityStyle, baseStyle: EntityStyle): EntityStyle {
	const delta: EntityStyle = {};
	const keys = Object.keys(newStyle) as (keyof EntityStyle)[];

	for (const key of keys) {
		if (newStyle[key] !== baseStyle[key]) {
			(delta as Record<string, unknown>)[key] = newStyle[key];
		}
	}

	return delta;
}

export function isDeltaEmpty(delta: EntityStyle): boolean {
	return Object.keys(delta).length === 0;
}

