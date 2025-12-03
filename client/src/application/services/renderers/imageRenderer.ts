import type { Profile, EntityStyle } from '../../../../../shared/src/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../../../../../shared/src/utils';

export function renderImages(
  doc: Document,
  usedIds: Set<string>,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>,
  selectable: boolean
): void {
  doc.querySelectorAll('img').forEach((img) => {
    const src = img.getAttribute('src') || '';
    const alt = img.getAttribute('alt') || '';
    const elId = generateElementId('img', src + alt, usedIds);

    const imageStyle = getFinalStyle('image', elId, profile, overrides);

    const imgWrapper = doc.createElement('div');
    imgWrapper.id = elId;
    imgWrapper.setAttribute('data-type', 'image');
    imgWrapper.setAttribute('style', styleToCSS(imageStyle));
    if (selectable) imgWrapper.classList.add('element-selectable');

    const imgStyle = `max-width: ${imageStyle.maxWidth || 100}%; height: auto; display: block;`;
    let alignStyle = '';
    if (imageStyle.textAlign === 'center') {
      alignStyle = ' margin: 0 auto;';
    } else if (imageStyle.textAlign === 'right') {
      alignStyle = ' margin-left: auto;';
    } else if (imageStyle.textAlign === 'left') {
      alignStyle = ' margin-left: 0;';
    }
    img.setAttribute('style', imgStyle + alignStyle);

    const parent = img.parentNode;
    if (parent) {
      parent.insertBefore(imgWrapper, img);
      imgWrapper.appendChild(img);

      if (alt) {
        const captionId = generateElementId('caption', alt, usedIds);
        const captionStyle = getFinalStyle('image-caption', captionId, profile, overrides);

        const caption = doc.createElement('div');
        caption.id = captionId;
        caption.setAttribute('data-type', 'image-caption');
        caption.setAttribute('style', styleToCSS(captionStyle));
        caption.textContent = alt;
        if (selectable) caption.classList.add('element-selectable');

        parent.insertBefore(caption, imgWrapper.nextSibling);
      }
    }
  });
}

