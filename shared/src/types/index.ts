// ==================== ENTITY TYPES ====================

export type EntityType =
  | 'paragraph'
  | 'heading'
  | 'image'
  | 'image-caption'
  | 'ordered-list'
  | 'unordered-list'
  | 'table'
  | 'table-caption'
  | 'formula'
  | 'formula-caption';

export const ALL_ENTITY_TYPES: EntityType[] = [
  'paragraph',
  'heading',
  'image',
  'image-caption',
  'ordered-list',
  'unordered-list',
  'table',
  'table-caption',
  'formula',
  'formula-caption',
];

export const ENTITY_LABELS: Record<EntityType, string> = {
  paragraph: 'Параграф',
  heading: 'Заголовок',
  image: 'Изображение',
  'image-caption': 'Подпись к изображению',
  'ordered-list': 'Нумерованный список',
  'unordered-list': 'Маркированный список',
  table: 'Таблица',
  'table-caption': 'Подпись к таблице',
  formula: 'Формула',
  'formula-caption': 'Подпись к формуле',
};

// ==================== ENTITY STYLE ====================

export interface EntityStyle {
  // Typography
  fontSize?: number;                                    // pt
  fontWeight?: 'normal' | 'bold';
  fontStyle?: 'normal' | 'italic';
  fontFamily?: string;

  // Text formatting
  textAlign?: 'left' | 'center' | 'right' | 'justify';
  textIndent?: number;                                  // cm (first line indent)
  lineHeight?: number;                                  // multiplier

  // Margins (external spacing)
  marginTop?: number;                                   // pt
  marginBottom?: number;                                // pt
  marginLeft?: number;                                  // pt
  marginRight?: number;                                 // pt

  // Padding (internal spacing for lists)
  paddingLeft?: number;                                 // pt

  // Border (for tables and images)
  borderWidth?: number;                                 // px
  borderColor?: string;
  borderStyle?: 'none' | 'solid' | 'dashed';

  // Size (for images)
  maxWidth?: number;                                    // %

  // Colors
  color?: string;
  backgroundColor?: string;
}

// ==================== PAGE SETTINGS ====================

export type PageSize = 'A4' | 'A5' | 'Letter';
export type PageOrientation = 'portrait' | 'landscape';

export interface PageNumberSettings {
  enabled: boolean;
  position: 'top' | 'bottom';
  align: 'left' | 'center' | 'right';
  format: string;                                       // "{n}", "Страница {n}", "{n} из {total}"
  fontSize?: number;
  fontStyle?: 'normal' | 'italic';
  fontFamily?: string;
}

export interface PageMargins {
  top: number;
  right: number;
  bottom: number;
  left: number;
}

export interface PageSettings {
  size: PageSize;
  orientation: PageOrientation;
  margins: PageMargins;
  pageNumbers?: PageNumberSettings;
}

// ==================== PROFILE ====================

export interface Profile {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  page: PageSettings;
  entities: Partial<Record<EntityType, EntityStyle>>;
}

export interface CreateProfileDTO {
  name: string;
}

export interface UpdateProfileDTO {
  name?: string;
  page?: PageSettings;
  entities?: Partial<Record<EntityType, EntityStyle>>;
}

// ==================== DOCUMENT ====================

export interface DocumentMeta {
  id: string;
  name: string;
  profileId: string;
  createdAt: string;
  updatedAt: string;
}

export interface Document extends DocumentMeta {
  content: string;
  overrides: Record<string, EntityStyle>;              // elementId -> style delta
}

export interface CreateDocumentDTO {
  name: string;
  profileId: string;
  content?: string;
}

export interface UpdateDocumentDTO {
  name?: string;
  profileId?: string;
  content?: string;
  overrides?: Record<string, EntityStyle>;
}

// ==================== DEFAULT VALUES ====================

export const DEFAULT_PAGE_NUMBER_SETTINGS: PageNumberSettings = {
  enabled: false,
  position: 'bottom',
  align: 'center',
  format: '{n}',
  fontSize: 12,
};

export const DEFAULT_PAGE_SETTINGS: PageSettings = {
  size: 'A4',
  orientation: 'portrait',
  margins: { top: 20, right: 20, bottom: 20, left: 20 },
  pageNumbers: DEFAULT_PAGE_NUMBER_SETTINGS,
};

export const DEFAULT_ENTITY_STYLES: Record<EntityType, EntityStyle> = {
  paragraph: {
    fontSize: 14,
    fontWeight: 'normal',
    fontStyle: 'normal',
    textAlign: 'justify',
    textIndent: 1.25,
    lineHeight: 1.5,
    marginTop: 0,
    marginBottom: 10,
  },
  heading: {
    fontSize: 14,
    fontWeight: 'bold',
    fontStyle: 'normal',
    textAlign: 'left',
    textIndent: 0,
    lineHeight: 1.5,
    marginTop: 0,
    marginBottom: 0,
  },
  image: {
    textAlign: 'center',
    marginTop: 10,
    marginBottom: 5,
    maxWidth: 100,
  },
  'image-caption': {
    fontSize: 12,
    fontStyle: 'italic',
    textAlign: 'center',
    marginTop: 5,
    marginBottom: 15,
  },
  'ordered-list': {
    fontSize: 14,
    lineHeight: 1.5,
    marginTop: 10,
    marginBottom: 10,
    marginLeft: 20,
  },
  'unordered-list': {
    fontSize: 14,
    lineHeight: 1.5,
    marginTop: 10,
    marginBottom: 10,
    marginLeft: 20,
  },
  table: {
    fontSize: 14,
    textAlign: 'left',
    marginTop: 10,
    marginBottom: 10,
    borderWidth: 1,
    borderColor: '#333333',
    borderStyle: 'solid',
  },
  'table-caption': {
    fontSize: 12,
    fontStyle: 'italic',
    textAlign: 'center',
    marginTop: 5,
    marginBottom: 15,
  },
  formula: {
    textAlign: 'center',
    marginTop: 15,
    marginBottom: 15,
  },
  'formula-caption': {
    fontSize: 12,
    fontStyle: 'italic',
    textAlign: 'center',
    marginTop: 5,
    marginBottom: 15,
  },
};

// ==================== LAYOUT SYSTEM (для маппинга сущностей на листе) ====================

/**
 * Позиция элемента на странице (в процентах от размера страницы)
 */
export interface LayoutPosition {
  x: number;       // % от левого края
  y: number;       // % от верхнего края
  width: number;   // % ширины страницы
  height: number;  // % высоты страницы (auto если не задано)
}

/**
 * Элемент размещения - связывает тип сущности с позицией
 */
export interface LayoutElement {
  id: string;
  entityType: EntityType | 'custom';     // тип сущности или кастомный
  position: LayoutPosition;
  style?: EntityStyle;                   // переопределение стилей для этого элемента
  content?: string;                      // статический контент (для custom)
  zIndex?: number;                       // порядок наложения
}

/**
 * Раскладка страницы - набор элементов
 */
export interface PageLayout {
  id: string;
  name: string;
  elements: LayoutElement[];
  backgroundColor?: string;
}

/**
 * Шаблон документа - может содержать несколько раскладок
 * Например: титульная страница, страница оглавления, обычная страница
 */
export interface LayoutTemplate {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  layouts: {
    titlePage?: PageLayout;      // титульная страница
    tocPage?: PageLayout;        // оглавление
    contentPage?: PageLayout;    // страница с контентом
    custom?: PageLayout[];       // кастомные страницы
  };
}

export interface CreateLayoutTemplateDTO {
  name: string;
  description?: string;
}

export interface UpdateLayoutTemplateDTO {
  name?: string;
  description?: string;
  layouts?: LayoutTemplate['layouts'];
}

// ==================== FONT OPTIONS ====================

export const FONT_FAMILIES = [
  { value: '', label: 'По умолчанию' },
  { value: 'Times New Roman', label: 'Times New Roman' },
  { value: 'Arial', label: 'Arial' },
  { value: 'Calibri', label: 'Calibri' },
  { value: 'Georgia', label: 'Georgia' },
  { value: 'Verdana', label: 'Verdana' },
  { value: 'Courier New', label: 'Courier New' },
  { value: 'Helvetica', label: 'Helvetica' },
  { value: 'Tahoma', label: 'Tahoma' },
  { value: 'Palatino', label: 'Palatino' },
  { value: 'Garamond', label: 'Garamond' },
] as const;

