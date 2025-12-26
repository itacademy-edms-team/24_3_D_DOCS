import type { TitlePage } from '@/entities/title-page/types';

/**
 * Extract variable keys from title page elements
 * Returns array of unique variable keys used in elements with type 'variable'
 */
export function getTitlePageVariableKeys(titlePage: TitlePage): string[] {
	if (!titlePage || !titlePage.elements) {
		return [];
	}

	const keys = new Set<string>();

	for (const element of titlePage.elements) {
		if (element.type === 'variable' && element.variableKey) {
			keys.add(element.variableKey);
		}
	}

	return Array.from(keys);
}
