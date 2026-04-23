import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';
import { tryRefreshAccessToken } from '@/shared/auth/tryRefreshAccessToken';
import { withFreshUploadAssetToken } from '@/shared/utils/withFreshUploadAssetToken';

/**
 * Render images with profile styles
 * Pure function - modifies document but has no other side effects
 */
export function renderImages(
	doc: Document,
	usedIds: Set<string>,
	profile: ProfileData | null,
	overrides: Record<string, EntityStyle>,
	selectable: boolean
): void {
	// Get all images in document order
	const images = Array.from(doc.querySelectorAll('img')) as Element[];
	
	// First pass: find all caption paragraphs and map them to previous images
	const imageCaptionMap = new Map<Element, { paragraph: Element; text: string; imageNumber: number; imageParagraph: Element }>();
	let imageCounter = 0;
	
	// Find all paragraphs with IMAGE-CAPTION
	const allParagraphs = Array.from(doc.querySelectorAll('p'));
	allParagraphs.forEach((captionP) => {
		const pText = (captionP.textContent || '').trim(); // Trim whitespace including \n
		const match = pText.match(/^\[IMAGE-CAPTION:\s*(.+)\]$/);
		if (match) {
			// Find the previous paragraph that contains an image
			let prevSibling: Element | null = captionP.previousElementSibling;
			
			// Look backwards to find a paragraph containing an image
			while (prevSibling) {
				if (prevSibling.tagName === 'P') {
					const imgInParagraph = prevSibling.querySelector('img');
					if (imgInParagraph && images.includes(imgInParagraph)) {
						imageCounter++;
						imageCaptionMap.set(imgInParagraph, {
							paragraph: captionP, // The caption paragraph
							text: match[1].trim(),
							imageNumber: imageCounter,
							imageParagraph: prevSibling, // The paragraph containing the image - save this for later insertion
						});
						break;
					}
				}
				prevSibling = prevSibling.previousElementSibling;
			}
		}
	});
	
	// Second pass: render images with their captions
	images.forEach((img) => {
		const rawSrc = img.getAttribute('src') || '';
		let src = rawSrc;
		const alt = img.getAttribute('alt') || '';
		const elId = generateElementId('img', rawSrc + alt, usedIds);

		if (src && (src.includes('/api/') || src.startsWith('api/'))) {
			src = withFreshUploadAssetToken(src);
		}

		if (src) {
			img.setAttribute('src', src);
			if (rawSrc.includes('/api/') || rawSrc.startsWith('api/')) {
				let assetRetried = false;
				img.addEventListener(
					'error',
					function onAssetImgError() {
						void (async () => {
							if (assetRetried) {
								img.removeEventListener('error', onAssetImgError);
								return;
							}
							assetRetried = true;
							if (await tryRefreshAccessToken()) {
								img.setAttribute('src', withFreshUploadAssetToken(rawSrc));
								return;
							}
							img.removeEventListener('error', onAssetImgError);
						})();
					},
					{ passive: true },
				);
			}
			// Don't set crossorigin attribute - it can cause issues with query parameters
			// Images will load normally without CORS preflight
		}

		const imageStyle = getFinalStyle('image' as EntityType, elId, profile, overrides);

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

			// Check if this image has a caption from the map
			const captionInfo = imageCaptionMap.get(img);
			if (captionInfo) {
				const captionText = captionInfo.text;
				const captionParagraph = captionInfo.paragraph;
				const imageNumber = captionInfo.imageNumber;
				const imageParagraph = captionInfo.imageParagraph; // The paragraph containing the image (before wrapping)
				
				const captionId = generateElementId('image-caption', captionText, usedIds);
				const captionStyle = getFinalStyle(
					'image-caption' as EntityType,
					captionId,
					profile,
					overrides
				);

				// Get caption format from profile
				const captionFormat =
					captionStyle.captionFormat ||
					profile?.entityStyles?.['image-caption']?.captionFormat ||
					'Рисунок {n} - {content}';
				
				// Apply template
				const formattedCaption = captionFormat
					.replace(/{n}/g, imageNumber.toString())
					.replace(/{content}/g, captionText);

				const caption = doc.createElement('div');
				caption.id = captionId;
				caption.setAttribute('data-type', 'image-caption');
				caption.setAttribute('style', styleToCSS(captionStyle));
				caption.textContent = formattedCaption;
				if (selectable) caption.classList.add('element-selectable');

				// Remove the original caption paragraph first
				if (captionParagraph.parentElement) {
					captionParagraph.remove();
				}
				
				// Insert caption after the paragraph containing the image
				// imageParagraph was saved BEFORE wrapping, but after wrapping, the image is now inside imgWrapper
				// and imgWrapper is inside that same paragraph, so the paragraph still exists and we can insert after it
				if (imageParagraph && imageParagraph.parentNode && doc.body.contains(imageParagraph)) {
					// Insert caption after the paragraph containing the image (which now contains imgWrapper)
					if (imageParagraph.nextSibling) {
						imageParagraph.parentNode.insertBefore(caption, imageParagraph.nextSibling);
					} else {
						imageParagraph.parentNode.appendChild(caption);
					}
				} else {
					// Fallback: find the paragraph containing imgWrapper
					let wrapperParent = imgWrapper.parentElement;
					while (wrapperParent && wrapperParent.tagName !== 'P' && wrapperParent !== doc.body) {
						wrapperParent = wrapperParent.parentElement;
					}
					
					if (wrapperParent && wrapperParent.parentElement) {
						if (wrapperParent.nextSibling) {
							wrapperParent.parentElement.insertBefore(caption, wrapperParent.nextSibling);
						} else {
							wrapperParent.parentElement.appendChild(caption);
						}
					} else {
						// Last resort: insert after wrapper
						if (imgWrapper.nextSibling) {
							parent.insertBefore(caption, imgWrapper.nextSibling);
						} else {
							parent.appendChild(caption);
						}
					}
				}
			}
		}
	});
}
