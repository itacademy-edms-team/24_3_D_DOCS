import type { TitlePageElement } from '../../../../../../shared/src/types';

export function renderLineElement(element: TitlePageElement): string {
  const positionStyle = `left: ${element.x}mm; top: ${element.y}mm;`;
  const length = element.length || 100;
  const thickness = element.thickness || 1;
  return `<div class="element line-element" style="${positionStyle} width: ${length}mm; height: ${thickness}mm;"></div>`;
}

