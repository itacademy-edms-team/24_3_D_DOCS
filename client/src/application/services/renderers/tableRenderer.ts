import type { Profile, EntityStyle } from '../../../../../shared/src/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../../../../../shared/src/utils';

export function renderTables(
  doc: Document,
  usedIds: Set<string>,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>,
  selectable: boolean
): void {
  doc.querySelectorAll('table').forEach((table) => {
    const content = table.textContent || '';
    const elId = generateElementId('table', content.slice(0, 50), usedIds);
    const style = getFinalStyle('table', elId, profile, overrides);

    const borderStyle = style.borderStyle || 'solid';
    const borderWidth = style.borderWidth || 1;
    const borderColor = style.borderColor || '#333';
    const cellStyle = `border: ${borderWidth}px ${borderStyle} ${borderColor}; padding: 8px;`;

    table.querySelectorAll('th, td').forEach((cell) => {
      cell.setAttribute('style', cellStyle);
    });

    let captionText: string | null = null;
    const nextSibling = table.nextElementSibling;
    if (nextSibling && nextSibling.tagName === 'P') {
      const pText = nextSibling.textContent || '';
      const match = pText.match(/^\[TABLE-CAPTION:\s*(.+)\]$/);
      if (match) {
        captionText = match[1].trim();
        nextSibling.remove();
      }
    }

    const tableWrapper = doc.createElement('div');
    tableWrapper.id = elId;
    tableWrapper.setAttribute('data-type', 'table');
    tableWrapper.setAttribute('style', styleToCSS(style));
    if (selectable) tableWrapper.classList.add('element-selectable');

    const parent = table.parentNode;
    if (parent) {
      parent.insertBefore(tableWrapper, table);
      tableWrapper.appendChild(table);

      const tableFinalStyle: EntityStyle = {
        ...style,
        marginTop: undefined,
        marginBottom: undefined,
        marginLeft: undefined,
        marginRight: undefined,
      };
      table.setAttribute('style', styleToCSS(tableFinalStyle) + '; border-collapse: collapse; width: 100%;');

      if (captionText) {
        const captionId = generateElementId('table-caption', captionText, usedIds);
        const captionStyle = getFinalStyle('table-caption', captionId, profile, overrides);

        const caption = doc.createElement('div');
        caption.id = captionId;
        caption.setAttribute('data-type', 'table-caption');
        caption.setAttribute('style', styleToCSS(captionStyle));
        caption.textContent = captionText;
        if (selectable) caption.classList.add('element-selectable');

        parent.insertBefore(caption, tableWrapper.nextSibling);
      }
    }
  });
}

