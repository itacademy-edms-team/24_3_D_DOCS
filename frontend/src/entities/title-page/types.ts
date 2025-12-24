import type {
	TitlePage,
	TitlePageMeta,
	TitlePageElement,
} from '@/shared/types/titlePage';

export type { TitlePage, TitlePageMeta, TitlePageElement };

export interface CreateTitlePageDTO {
	name: string;
}

export interface UpdateTitlePageDTO {
	name?: string;
	elements?: TitlePageElement[];
	variables?: Record<string, string>;
}
