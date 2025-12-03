/**
 * Utility functions for working with localStorage
 */

const DEFAULT_TITLE_PAGE_ID_KEY = 'defaultTitlePageId';

/**
 * Get the default title page ID from localStorage
 */
export function getDefaultTitlePageId(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(DEFAULT_TITLE_PAGE_ID_KEY);
}

/**
 * Set the default title page ID in localStorage
 */
export function setDefaultTitlePageId(id: string | null): void {
  if (typeof window === 'undefined') return;
  if (id === null) {
    localStorage.removeItem(DEFAULT_TITLE_PAGE_ID_KEY);
  } else {
    localStorage.setItem(DEFAULT_TITLE_PAGE_ID_KEY, id);
  }
}

