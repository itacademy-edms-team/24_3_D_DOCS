import type { ProfileData, EntityStyle } from '@/entities/profile/types';
import { getFinalStyle, styleToCSS, generateElementId } from '../renderUtils';
import type { EntityType } from '../renderUtils';

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
		
		// #region agent log
		const logData1 = {location:'imageRenderer.ts:16',message:'Image found in render',data:{originalSrc:src,hasToken:src.includes('token')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'};
		if (typeof console !== 'undefined') console.log('[DEBUG]', logData1);
		fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(logData1)}).catch((e)=>{if(typeof console !== 'undefined')console.warn('[DEBUG] Log fetch failed:',e)});
		// #endregion
		
		// Update image src with fresh token if it's a local asset URL
		// This ensures images always have a valid token when rendering
		if (src && (src.startsWith('/api/upload/') || src.includes('/api/upload/'))) {
			try {
				// Get current token from localStorage
				const authData = typeof localStorage !== 'undefined' ? localStorage.getItem('auth-storage') : null;
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:25',message:'Checking auth data',data:{hasAuthData:!!authData,isUploadUrl:true},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
				// #endregion
				if (authData) {
					try {
						const parsed = JSON.parse(authData);
						const currentToken = parsed?.state?.accessToken || parsed?.accessToken;
						// #region agent log
						fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:30',message:'Token extracted',data:{hasToken:!!currentToken,tokenLength:currentToken?.length||0},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
						// #endregion
						if (currentToken) {
							// Parse URL and update token
							try {
								// Handle both absolute and relative URLs
								let url: URL;
								if (src.startsWith('http://') || src.startsWith('https://')) {
									// Absolute URL - parse directly
									url = new URL(src);
								} else {
									// Relative URL - use window.location.origin as base
									url = new URL(src, window.location.origin);
								}
								
								// Replace or add token parameter
								url.searchParams.set('token', currentToken);
								
								// Preserve relative URL format if original was relative
								if (!src.startsWith('http://') && !src.startsWith('https://')) {
									src = url.pathname + url.search;
								} else {
									src = url.toString();
								}
								// #region agent log
								fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:44',message:'URL updated with token',data:{newSrc:src,hasTokenInUrl:src.includes('token')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
								// #endregion
							} catch (urlError) {
								// If URL parsing fails, try to replace or append token manually
								try {
									// Remove existing token parameter if present
									const urlWithoutToken = src.split('?')[0];
									const existingParams = src.includes('?') ? src.split('?')[1] : '';
									const params = new URLSearchParams(existingParams);
									params.set('token', currentToken);
									
									const separator = '?';
									src = urlWithoutToken + separator + params.toString();
									// #region agent log
									fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:62',message:'URL updated with token (fallback)',data:{newSrc:src,hasTokenInUrl:src.includes('token')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
									// #endregion
								} catch (fallbackError) {
									// Last resort: simple append
									const separator = src.includes('?') ? '&' : '?';
									// Remove existing token if present
									const baseUrl = src.split('?')[0];
									const existingParams = src.includes('?') ? src.split('?')[1] : '';
									const params = new URLSearchParams(existingParams);
									params.set('token', currentToken);
									src = baseUrl + '?' + params.toString();
									// #region agent log
									fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:71',message:'URL updated with token (last resort)',data:{newSrc:src,hasTokenInUrl:src.includes('token'),error:fallbackError?.toString()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
									// #endregion
								}
							}
						}
					} catch (parseError) {
						// Ignore parse errors
						console.warn('Failed to parse auth data for image token:', parseError);
						// #region agent log
						fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:52',message:'Failed to parse auth data',data:{error:parseError?.toString()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
						// #endregion
					}
				}
			} catch (error) {
				// Ignore errors in token update
				console.warn('Failed to update image token:', error);
				// #region agent log
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'imageRenderer.ts:57',message:'Failed to update token',data:{error:error?.toString()},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
				// #endregion
			}
		}
		
		// Update src attribute with potentially updated URL
		if (src) {
			img.setAttribute('src', src);
			// Don't set crossorigin attribute - it can cause issues with query parameters
			// Images will load normally without CORS preflight
				// #region agent log
				const logData2 = {location:'imageRenderer.ts:88',message:'Src attribute set',data:{finalSrc:src,hasToken:src.includes('token')},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'};
				if (typeof console !== 'undefined') console.log('[DEBUG]', logData2);
				fetch('http://127.0.0.1:7246/ingest/55665079-6617-4fe4-9acd-dbe7baa4d7c6',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(logData2)}).catch((e)=>{if(typeof console !== 'undefined')console.warn('[DEBUG] Log fetch failed:',e)});
				// #endregion
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
