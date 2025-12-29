import type { ProfileData, EntityStyle, HeadingNumberingSettings } from '@/entities/profile/types';
import { applyStyles, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

/**
 * Calculate hierarchical heading numbers (1, 1.1, 1.1.1, etc.)
 * Pure function - no side effects
 */
function calculateHeadingNumbers(
	headings: Element[],
	settings: HeadingNumberingSettings | undefined
): Map<Element, string> {
	const numbers = new Map<Element, string>();
	const counters: number[] = [0, 0, 0, 0, 0, 0]; // Counters for levels 1-6 (index 0-5)
	
	headings.forEach((heading) => {
		const level = parseInt(heading.tagName[1], 10); // Level 1-6
		const levelIndex = level - 1; // Convert to 0-based index (0-5)
		const template = settings?.templates?.[level];
		
		// If numbering is enabled for this level
		if (template?.enabled) {
			// Reset counters for deeper levels
			for (let i = levelIndex + 1; i < 6; i++) {
				counters[i] = 0;
			}
			
			// Increment counter for current level
			counters[levelIndex]++;
			
			// Build hierarchical number (e.g., "1.2.3")
			const numberParts: string[] = [];
			for (let i = 0; i <= levelIndex; i++) {
				numberParts.push(counters[i].toString());
			}
			const number = numberParts.join('.');
			
			numbers.set(heading, number);
		}
	});
	
	return numbers;
}

/**
 * Render headings (h1-h6) with profile styles and numbering
 * Pure function - modifies document but has no other side effects
 */
export function renderHeadings(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	// Get all headings in document order
	const headings = Array.from(
		doc.querySelectorAll('h1, h2, h3, h4, h5, h6')
	) as Element[];
	
	// Calculate numbering if enabled
	const numberingSettings = profile?.headingNumbering;
	const headingNumbers = calculateHeadingNumbers(headings, numberingSettings);
	
	// Render each heading
	headings.forEach((el) => {
		const originalContent = el.textContent || '';
		const level = parseInt(el.tagName[1], 10);
		const elId = generateElementId(`h${level}`, originalContent, usedIds);
		
		// Get template for this level
		const template = numberingSettings?.templates?.[level];
		
		// Apply numbering if template exists and is enabled
		if (template?.enabled && template.format) {
			const number = headingNumbers.get(el) || '';
			const formattedText = template.format
				.replace(/{n}/g, number)
				.replace(/{content}/g, originalContent);
			el.textContent = formattedText;
		}
		
		// Use specific heading type (heading-1, heading-2, etc.) for styling
		// Fallback to general 'heading' type if specific level style is not defined
		const headingType = (`heading-${level}` as EntityType);
		applyStyles(
			el,
			headingType,
			elId,
			profile,
			overrides,
			selectable
		);
	});
}
