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
	const uploading = ref(false);
	const textareaRef = ref<HTMLTextAreaElement | null>(null);

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
				overrides: document.value.overrides,
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

	async function handleImageUpload(file: File) {
		if (!documentId || !document.value) return;

		uploading.value = true;
		try {
			const result = await DocumentAPI.uploadImage(documentId, file);
			// Keep relative URL - it will be processed in DocumentPreview
			const imageMarkdown = `\n![${file.name}](${result.url})\n`;

			const textarea = textareaRef.value;
			if (textarea) {
				const start = textarea.selectionStart;
				const newContent =
					document.value.content.substring(0, start) +
					imageMarkdown +
					document.value.content.substring(textarea.selectionEnd);

				document.value = { ...document.value, content: newContent };

				// Restore cursor position after content update
				setTimeout(() => {
					if (textarea) {
						const newPosition = start + imageMarkdown.length;
						textarea.selectionStart = textarea.selectionEnd = newPosition;
						textarea.focus();
					}
				}, 0);
			} else {
				document.value = { ...document.value, content: document.value.content + imageMarkdown };
			}
		} catch (error) {
			console.error('Upload failed:', error);
			alert('Ошибка загрузки изображения');
		} finally {
			uploading.value = false;
		}
	}

	return {
		document,
		profile,
		profiles,
		loading,
		saving,
		uploading,
		textareaRef,
		handleSave,
		handleNameChange,
		handleContentChange,
		handleProfileChange,
		handleImageUpload,
	};
}
