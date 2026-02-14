export interface Profile {
	id: string;
	creatorId: string;
	name: string;
	description?: string;
	isPublic: boolean;
	createdAt: string;
	updatedAt: string;
}

export interface ProfileWithData extends Profile {
	data: ProfileData;
}

export interface ProfileData {
	pageSettings: PageSettings;
	entityStyles: Record<string, EntityStyle>;
	headingNumbering?: HeadingNumberingSettings;
	tableOfContents?: TableOfContentsSettings;
}

export interface TableOfContentsSettings {
	fontStyle: 'normal' | 'italic';
	fontWeight: 'normal' | 'bold';
	fontSize: number;
	indentPerLevel: number;
	nestingEnabled: boolean;
	numberingEnabled: boolean;
}

export interface HeadingNumberingSettings {
	templates: Record<number, HeadingTemplate>;
}

export interface HeadingTemplate {
	format: string;
	enabled: boolean;
}

export interface PageSettings {
	size: string;
	orientation: 'portrait' | 'landscape';
	margins: {
		top: number;
		right: number;
		bottom: number;
		left: number;
	};
	pageNumbers: {
		enabled: boolean;
		position: string;
		align: string;
		format: string;
		fontSize: number;
		fontStyle: string;
		fontFamily: string;
		bottomOffset?: number;
	};
	globalLineHeight?: number;
}

export interface EntityStyle {
	fontFamily?: string;
	fontSize?: number;
	fontWeight?: string;
	fontStyle?: string;
	textAlign?: string;
	textIndent?: number;
	lineHeight?: number;
	lineHeightUseGlobal?: boolean;
	color?: string;
	backgroundColor?: string;
	highlightColor?: string;
	highlightBackgroundColor?: string;
	marginTop?: number;
	marginBottom?: number;
	marginLeft?: number;
	marginRight?: number;
	paddingLeft?: number;
	listAdditionalIndent?: number;
	listUseParagraphTextIndent?: boolean;
	borderWidth?: number;
	borderColor?: string;
	borderStyle?: string;
	maxWidth?: number;
	captionFormat?: string;
}

export interface CreateProfileDTO {
	name: string;
	description?: string;
	isPublic?: boolean;
	data?: ProfileData;
}

export interface UpdateProfileDTO {
	name?: string;
	description?: string;
	isPublic?: boolean;
	data?: ProfileData;
}
