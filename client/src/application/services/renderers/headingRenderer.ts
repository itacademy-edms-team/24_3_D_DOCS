import type { Profile, EntityStyle } from '../../../../../shared/src/types';
import { generateElementId } from '../../../../../shared/src/utils';
import { applyStyles } from '../renderUtils';

export function renderHeadings(
  doc: Document,
  usedIds: Set<string>,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>,
  selectable: boolean
): void {
  doc.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach((el) => {
    const content = el.textContent || '';
    const level = parseInt(el.tagName[1]);
    const elId = generateElementId(`h${level}`, content, usedIds);
    applyStyles(el, 'heading', elId, profile, overrides, selectable);
  });
}

