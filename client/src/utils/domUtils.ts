/**
 * Check if element is an input or textarea
 */
export function isInputElement(element: HTMLElement | null): boolean {
  if (!element) return false;
  return element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.isContentEditable;
}

/**
 * Get element by ID safely
 */
export function getElementById(id: string): HTMLElement | null {
  return document.getElementById(id);
}

/**
 * Create and trigger download
 */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

