import { ref, onMounted } from 'vue';
import DocumentAPI from '@/entities/document/api/DocumentAPI';
import ProfileAPI from '@/entities/profile/api/ProfileAPI';
import type { Document, UpdateDocumentDTO } from '@/entities/document/types';
import type { Profile, ProfileMeta } from '@/entities/profile/types';

export function useDocumentEditor(documentId: string | undefined) {
	const document = ref<Document | null>(null);
	const profile = ref<Profile | null>(null);
	const profiles = ref<ProfileMeta[]>([]);
	const loading = ref(true);
	const saving = ref(false);

	function init() {
		if (documentId) {
			loadDocument(documentId);
		} else {
			loading.value = false;
		}
		loadProfiles();
	}

	onMounted(() => {
		init();
	});

	async function loadProfiles() {
		try {
			const profilesData = await ProfileAPI.getAll();
			profiles.value = profilesData;
		} catch (error) {
			console.error('Failed to load profiles:', error);
		}
	}

	async function loadDocument(id: string) {
		try {
			loading.value = true;
			const docData = await DocumentAPI.getById(id);
			document.value = docData;

			// Load profile if document has profileId
			if (docData.profileId) {
				try {
					const profileData = await ProfileAPI.getById(docData.profileId);
					profile.value = profileData;
				} catch (error) {
					console.error('Failed to load profile:', error);
					// Continue without profile
				}
			}
		} catch (error) {
			console.error('Failed to load document:', error);
		} finally {
			loading.value = false;
		}
	}

	async function handleSave() {
		if (!document.value || !documentId) return;

		saving.value = true;
		try {
			const updateData: UpdateDocumentDTO = {
				name: document.value.name,
				content: document.value.content,
				profileId: document.value.profileId,
			};
			const updated = await DocumentAPI.update(documentId, updateData);
			if (updated) {
				document.value = updated;
			}
		} catch (error) {
			console.error('Failed to save document:', error);
		} finally {
			saving.value = false;
		}
	}

	function handleNameChange(name: string) {
		if (!document.value) return;
		document.value = { ...document.value, name };
	}

	function handleContentChange(content: string) {
		if (!document.value) return;
		document.value = { ...document.value, content };
	}

	async function handleProfileChange(profileId: string) {
		if (!document.value) return;
		
		document.value = { ...document.value, profileId: profileId || undefined };
		
		// Load profile if profileId is set
		if (profileId) {
			try {
				const profileData = await ProfileAPI.getById(profileId);
				profile.value = profileData;
			} catch (error) {
				console.error('Failed to load profile:', error);
				profile.value = null;
			}
		} else {
			profile.value = null;
		}
	}

	return {
		document,
		profile,
		profiles,
		loading,
		saving,
		handleSave,
		handleNameChange,
		handleContentChange,
		handleProfileChange,
	};
}
