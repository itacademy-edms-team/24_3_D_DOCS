import type { Profile, EntityStyle } from '../../../../../shared/src/types';
import { generateElementId } from '../../../../../shared/src/utils';
import { applyStyles } from '../renderUtils';

export function renderParagraphs(
  doc: Document,
  usedIds: Set<string>,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>,
  selectable: boolean
): void {
  doc.querySelectorAll('p').forEach((el) => {
    if (el.closest('figure')) return;

    const text = el.textContent || '';
    if (text.match(/^\[(TABLE|FORMULA)-CAPTION:/)) return;

    const content = text;
    const elId = generateElementId('p', content, usedIds);
    applyStyles(el, 'paragraph', elId, profile, overrides, selectable);
  });
}

