import { useState, useEffect, useRef, useCallback } from 'react';
import type { Document, Profile } from '../../../../shared/src/types';
import { documentApi, profileApi } from '../../infrastructure/api';
import { getDefaultTitlePageId } from '../../utils/storageUtils';

export function useDocumentEditor(docId: string | undefined) {
  const [document, setDocument] = useState<Document | null>(null);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [uploading, setUploading] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  
  // Undo/Redo history
  const historyRef = useRef<string[]>([]);
  const historyIndexRef = useRef<number>(-1);
  const MAX_HISTORY = 50;
  const isHistoryUpdateRef = useRef(false);

  useEffect(() => {
    if (docId) {
      loadData(docId);
    }
  }, [docId]);

  async function loadData(id: string) {
    try {
      const [docData, profilesData] = await Promise.all([
        documentApi.getById(id),
        profileApi.getAll(),
      ]);

      setDocument(docData);
      setProfiles(profilesData);

      if (docData.profileId) {
        const p = profilesData.find((x) => x.id === docData.profileId);
        if (p) setProfile(p);
      }
    } catch (error) {
      console.error('Failed to load:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!document || !docId) return;

    setSaving(true);
    try {
      await documentApi.update(docId, {
        name: document.name,
        content: document.content,
        profileId: document.profileId,
      });
    } catch (error) {
      console.error('Save failed:', error);
    } finally {
      setSaving(false);
    }
  }

  function handleNameChange(name: string) {
    if (!document) return;
    setDocument({ ...document, name });
  }

  async function handleGeneratePdf() {
    if (!docId || !document) return;

    setGenerating(true);
    try {
      // Save first
      await documentApi.update(docId, {
        name: document.name,
        content: document.content,
        profileId: document.profileId,
      });

      // Small delay to ensure file is written
      await new Promise((resolve) => setTimeout(resolve, 100));

      // Get title page ID from localStorage
      const titlePageId = getDefaultTitlePageId();
      const blob = await documentApi.generatePdf(docId, titlePageId);
      const url = URL.createObjectURL(blob);
      const a = window.document.createElement('a');
      a.href = url;
      a.download = `${document.name}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('PDF generation failed:', error);
      alert('Ошибка генерации PDF');
    } finally {
      setGenerating(false);
    }
  }

  const saveToHistory = useCallback((content: string) => {
    if (isHistoryUpdateRef.current) return;
    
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    // Remove any history after current index
    const newHistory = history.slice(0, index + 1);
    
    // Add new state
    newHistory.push(content);
    
    // Limit history size
    if (newHistory.length > MAX_HISTORY) {
      newHistory.shift();
    } else {
      historyIndexRef.current = newHistory.length - 1;
    }
    
    historyRef.current = newHistory;
  }, []);

  function handleContentChange(content: string) {
    if (!document) return;
    saveToHistory(content);
    setDocument({ ...document, content });
  }

  function handleUndo() {
    if (!document) return;
    
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    if (index > 0) {
      isHistoryUpdateRef.current = true;
      historyIndexRef.current = index - 1;
      const previousContent = history[historyIndexRef.current];
      setDocument({ ...document, content: previousContent });
      
      // Restore cursor position
      setTimeout(() => {
        const textarea = textareaRef.current;
        if (textarea) {
          textarea.focus();
        }
        isHistoryUpdateRef.current = false;
      }, 0);
    }
  }

  function handleRedo() {
    if (!document) return;
    
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    if (index < history.length - 1) {
      isHistoryUpdateRef.current = true;
      historyIndexRef.current = index + 1;
      const nextContent = history[historyIndexRef.current];
      setDocument({ ...document, content: nextContent });
      
      // Restore cursor position
      setTimeout(() => {
        const textarea = textareaRef.current;
        if (textarea) {
          textarea.focus();
        }
        isHistoryUpdateRef.current = false;
      }, 0);
    }
  }

  function handleProfileChange(profileId: string) {
    if (!document) return;
    const newProfile = profiles.find((p) => p.id === profileId) || null;
    setProfile(newProfile);
    setDocument({ ...document, profileId });
  }

  async function handleImageUpload(file: File) {
    if (!docId || !document) return;

    setUploading(true);
    try {
      const result = await documentApi.uploadImage(docId, file);
      const imageMarkdown = `\n![${file.name}](${result.url})\n`;

      const textarea = textareaRef.current;
      if (textarea) {
        const start = textarea.selectionStart;
        const newContent =
          document.content.substring(0, start) +
          imageMarkdown +
          document.content.substring(textarea.selectionEnd);

        setDocument({ ...document, content: newContent });

        setTimeout(() => {
          textarea.selectionStart = textarea.selectionEnd = start + imageMarkdown.length;
          textarea.focus();
        }, 0);
      } else {
        setDocument({ ...document, content: document.content + imageMarkdown });
      }
    } catch (error) {
      console.error('Upload failed:', error);
      alert('Ошибка загрузки изображения');
    } finally {
      setUploading(false);
    }
  }

  return {
    document,
    profile,
    profiles,
    loading,
    saving,
    generating,
    uploading,
    textareaRef,
    handleSave,
    handleNameChange,
    handleGeneratePdf,
    handleContentChange,
    handleProfileChange,
    handleImageUpload,
    handleUndo,
    handleRedo,
  };
}

