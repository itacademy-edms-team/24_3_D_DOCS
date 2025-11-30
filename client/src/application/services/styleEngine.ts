import type { EntityStyle, EntityType, Profile } from '../../../../shared/src/types';
import { DEFAULT_ENTITY_STYLES } from '../../../../shared/src/types';

/**
 * Get base style for entity type (DEFAULT + Profile overrides)
 */
export function getBaseStyle(entityType: EntityType, profile: Profile | null): EntityStyle {
  const defaultStyle = DEFAULT_ENTITY_STYLES[entityType];
  const profileStyle = profile?.entities?.[entityType] || {};
  return { ...defaultStyle, ...profileStyle };
}

/**
 * Get final style for element (BASE + Element overrides)
 */
export function getFinalStyle(
  entityType: EntityType,
  elementId: string,
  profile: Profile | null,
  overrides: Record<string, EntityStyle>
): EntityStyle {
  const baseStyle = getBaseStyle(entityType, profile);
  const override = overrides[elementId] || {};
  return { ...baseStyle, ...override };
}

/**
 * Compute style delta (only changed fields compared to base)
 */
export function computeStyleDelta(newStyle: EntityStyle, baseStyle: EntityStyle): EntityStyle {
  const delta: EntityStyle = {};
  const keys = Object.keys(newStyle) as (keyof EntityStyle)[];

  for (const key of keys) {
    if (newStyle[key] !== baseStyle[key]) {
      (delta as Record<string, unknown>)[key] = newStyle[key];
    }
  }

  return delta;
}

/**
 * Check if style delta is empty
 */
export function isDeltaEmpty(delta: EntityStyle): boolean {
  return Object.keys(delta).length === 0;
}

/**
 * Convert EntityStyle to CSS string
 */
export function styleToCSS(style: EntityStyle): string {
  const rules: string[] = [];

  // Typography
  if (style.fontSize !== undefined) rules.push(`font-size: ${style.fontSize}pt`);
  if (style.fontWeight) rules.push(`font-weight: ${style.fontWeight}`);
  if (style.fontStyle) rules.push(`font-style: ${style.fontStyle}`);
  if (style.fontFamily) rules.push(`font-family: ${style.fontFamily}`);

  // Text formatting
  if (style.textAlign) rules.push(`text-align: ${style.textAlign}`);
  if (style.textIndent !== undefined) rules.push(`text-indent: ${style.textIndent}cm`);
  if (style.lineHeight !== undefined) rules.push(`line-height: ${style.lineHeight}`);

  // Margins
  if (style.marginTop !== undefined) rules.push(`margin-top: ${style.marginTop}pt`);
  if (style.marginBottom !== undefined) rules.push(`margin-bottom: ${style.marginBottom}pt`);
  if (style.marginLeft !== undefined) rules.push(`margin-left: ${style.marginLeft}pt`);
  if (style.marginRight !== undefined) rules.push(`margin-right: ${style.marginRight}pt`);
  if (style.paddingLeft !== undefined) rules.push(`padding-left: ${style.paddingLeft}pt`);

  // Border
  if (style.borderWidth && style.borderStyle && style.borderStyle !== 'none') {
    rules.push(`border: ${style.borderWidth}px ${style.borderStyle} ${style.borderColor || '#333'}`);
  }

  // Size
  if (style.maxWidth !== undefined) rules.push(`max-width: ${style.maxWidth}%`);

  // Colors
  if (style.color) rules.push(`color: ${style.color}`);
  if (style.backgroundColor) rules.push(`background-color: ${style.backgroundColor}`);

  return rules.join('; ');
}

/**
 * Generate stable element ID based on content hash
 */
export function generateElementId(type: string, content: string, usedIds: Set<string>): string {
  let hash = 0;
  const str = content.slice(0, 100).trim();

  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash) + str.charCodeAt(i);
    hash = hash & hash;
  }

  const hashStr = Math.abs(hash).toString(36);
  let id = `${type}-${hashStr}`;
  let counter = 0;

  while (usedIds.has(id)) {
    counter++;
    id = `${type}-${hashStr}-${counter}`;
  }

  usedIds.add(id);
  return id;
}

