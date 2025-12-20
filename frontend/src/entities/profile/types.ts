export interface ProfileMeta {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
}

export interface Profile extends ProfileMeta {
	page: {
		size: string;
		orientation: 'portrait' | 'landscape';
		margins: {
			top: number;
			right: number;
			bottom: number;
			left: number;
		};
		pageNumbers?: {
			enabled: boolean;
			position: string;
			align: string;
			format: string;
			fontSize: number;
			[key: string]: any;
		};
	};
	entities: Record<string, any>;
}

export interface CreateProfileDTO {
	name: string;
}

export interface UpdateProfileDTO {
	name?: string;
	page?: Partial<Profile['page']>;
	entities?: Partial<Profile['entities']>;
}
