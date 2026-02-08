import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';
import { getAccessToken } from '@/shared/auth/tokenStorage';

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
		let src = img.getAttribute('src') || '';
		const alt = img.getAttribute('alt') || '';
		const elId = generateElementId('img', src + alt, usedIds);
		
		
		
		// Update image src with fresh token if it's an API URL
		// This ensures images always have a valid token when rendering
		if (src && (src.includes('/api/') || src.startsWith('api/'))) {
			const currentToken = getAccessToken();
			if (currentToken) {
				try {
					// Parse URL and update token
					let url: URL;
					const base = typeof BASE_URI !== 'undefined' ? BASE_URI : window.location.origin;
					
					if (src.startsWith('http://') || src.startsWith('https://')) {
						// Absolute URL - parse directly
						url = new URL(src);
					} else {
						// Relative URL - use base
						url = new URL(src, base);
					}

					// Replace or add token parameter
					url.searchParams.set('token', currentToken);

					// Always use absolute URL for API calls to ensure correct origin
					src = url.toString();
				} catch (urlError) {
					// If URL parsing fails, try to replace or append token manually
					try {
						const base = typeof BASE_URI !== 'undefined' ? BASE_URI : '';
						let fullUrl = src;
						if (base && !src.startsWith('http')) {
							fullUrl = base.endsWith('/') ? base + src.replace(/^\//, '') : base + (src.startsWith('/') ? src : '/' + src);
						}
						
						// Remove existing token parameter if present
						const urlWithoutToken = fullUrl.split('?')[0];
						const existingParams = fullUrl.includes('?') ? fullUrl.split('?')[1] : '';
						const params = new URLSearchParams(existingParams);
						params.set('token', currentToken);

						src = urlWithoutToken + '?' + params.toString();
					} catch (fallbackError) {
						// Last resort: simple append
						// Remove existing token if present
						const baseUrl = src.split('?')[0];
						const existingParams = src.includes('?') ? src.split('?')[1] : '';
						const params = new URLSearchParams(existingParams);
						params.set('token', currentToken);
						src = baseUrl + '?' + params.toString();
					}
				}
			}
		}
		
		// Update src attribute with potentially updated URL
		if (src) {
			img.setAttribute('src', src);
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
