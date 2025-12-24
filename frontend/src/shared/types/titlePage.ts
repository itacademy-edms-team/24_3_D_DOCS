export type TitlePageElementType = 'text' | 'variable' | 'line';

export interface TitlePageElement {
	id: string;
	type: TitlePageElementType;
	x: number; // mm
	y: number; // mm
	// For text/variable
	content?: string;
	fontSize?: number;
	fontFamily?: string;
	fontWeight?: 'normal' | 'bold';
	fontStyle?: 'normal' | 'italic';
	lineHeight?: number; // multiplier
	textAlign?: 'left' | 'center' | 'right';
	// For variable
	variableKey?: string;
	// For line
	length?: number; // mm
	thickness?: number; // mm
}

export interface TitlePage {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
	elements: TitlePageElement[];
	variables: Record<string, string>;
}

export interface TitlePageMeta {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
}
