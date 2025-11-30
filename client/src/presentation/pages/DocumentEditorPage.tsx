import { useState, useEffect, useMemo, useRef, DragEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import 'katex/dist/katex.min.css';
import type { Document, Profile } from '../../../../shared/src/types';
import { documentApi, profileApi } from '../../infrastructure/api';
import { renderDocument } from '../../application/services/documentRenderer';
import { DocumentPreview } from '../components/DocumentPreview';

export function DocumentEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [document, setDocument] = useState<Document | null>(null);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);

  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    if (id) {
      loadData(id);
    }
  }, [id]);

  async function loadData(docId: string) {
    try {
      const [docData, profilesData] = await Promise.all([
        documentApi.getById(docId),
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
    if (!document || !id) return;

    setSaving(true);
    try {
      await documentApi.update(id, {
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
    if (!id || !document) return;

    setGenerating(true);
    try {
      // Save first
      await documentApi.update(id, {
        name: document.name,
        content: document.content,
        profileId: document.profileId,
      });

      // Small delay to ensure file is written
      await new Promise((resolve) => setTimeout(resolve, 100));

      const blob = await documentApi.generatePdf(id);
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

  function handleContentChange(content: string) {
    if (!document) return;
    setDocument({ ...document, content });
  }

  function handleProfileChange(profileId: string) {
    if (!document) return;
    const newProfile = profiles.find((p) => p.id === profileId) || null;
    setProfile(newProfile);
    setDocument({ ...document, profileId });
  }

  async function handleImageUpload(file: File) {
    if (!id || !document) return;

    setUploading(true);
    try {
      const result = await documentApi.uploadImage(id, file);
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

  function handleDragOver(e: DragEvent<HTMLTextAreaElement>) {
    e.preventDefault();
    setIsDragging(true);
  }

  function handleDragLeave(e: DragEvent<HTMLTextAreaElement>) {
    e.preventDefault();
    setIsDragging(false);
  }

  function handleDrop(e: DragEvent<HTMLTextAreaElement>) {
    e.preventDefault();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files.length > 0 && files[0].type.startsWith('image/')) {
      handleImageUpload(files[0]);
    }
  }

  function handlePaste(e: React.ClipboardEvent<HTMLTextAreaElement>) {
    const items = e.clipboardData.items;
    for (let i = 0; i < items.length; i++) {
      if (items[i].type.startsWith('image/')) {
        e.preventDefault();
        const file = items[i].getAsFile();
        if (file) handleImageUpload(file);
        break;
      }
    }
  }

  const renderedHtml = useMemo(() => {
    if (!document) return '';

    return renderDocument({
      markdown: document.content,
      profile,
      overrides: document.overrides || {},
      selectable: false,
    });
  }, [document?.content, document?.overrides, profile]);

  if (loading) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Загрузка...</div>
      </div>
    );
  }

  if (!document) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Документ не найден</div>
      </div>
    );
  }

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Toolbar */}
      <div className="toolbar">
        <div className="toolbar-group">
          <button className="btn btn-ghost btn-sm" onClick={() => navigate('/')}>
            ← Назад
          </button>
          <span style={{ color: 'var(--text-muted)' }}>|</span>
          <input
            type="text"
            value={document.name}
            onChange={(e) => handleNameChange(e.target.value)}
            className="form-input"
            style={{
              width: '200px',
              padding: '0.25rem 0.5rem',
              fontSize: '1rem',
              fontWeight: 600,
              background: 'transparent',
              border: '1px solid transparent',
              borderRadius: 'var(--radius-sm)',
            }}
            onFocus={(e) => {
              e.target.style.background = 'var(--bg-tertiary)';
              e.target.style.borderColor = 'var(--border-color)';
            }}
            onBlur={(e) => {
              e.target.style.background = 'transparent';
              e.target.style.borderColor = 'transparent';
            }}
          />

          <select
            value={document.profileId || ''}
            onChange={(e) => handleProfileChange(e.target.value)}
            style={{
              padding: '0.25rem 0.5rem',
              fontSize: '0.875rem',
              background: 'var(--bg-tertiary)',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--accent-primary)',
            }}
          >
            <option value="">Без профиля</option>
            {profiles.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </div>

        <div className="toolbar-group">
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => navigate(`/document/${id}/customize`)}
          >
            ✏️ Редактировать стили
          </button>
          <button className="btn btn-secondary btn-sm" onClick={handleSave} disabled={saving}>
            {saving ? '💾 Сохранение...' : '💾 Сохранить'}
          </button>
          <button
            className="btn btn-primary btn-sm"
            onClick={handleGeneratePdf}
            disabled={generating}
          >
            {generating ? '⏳ Генерация...' : '📥 Скачать PDF'}
          </button>
        </div>
      </div>

      {/* Split view */}
      <div className="split-view">
        {/* Editor */}
        <div className="split-pane split-pane-left" style={{ position: 'relative' }}>
          <textarea
            ref={textareaRef}
            className="code-editor"
            value={document.content}
            onChange={(e) => handleContentChange(e.target.value)}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onPaste={handlePaste}
            placeholder={`Введите Markdown...

# Заголовок
Обычный параграф текста.

Формулы: $E=mc^2$ или $$\\int_0^1 x^2 dx$$

Перетащите изображение или Ctrl+V`}
            spellCheck={false}
            style={{
              borderColor: isDragging ? 'var(--accent-primary)' : undefined,
              background: isDragging ? 'rgba(122, 162, 247, 0.1)' : undefined,
            }}
          />

          {uploading && (
            <div
              style={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                background: 'var(--bg-elevated)',
                padding: '1rem 2rem',
                borderRadius: 'var(--radius-md)',
                boxShadow: 'var(--shadow-lg)',
              }}
            >
              ⏳ Загрузка изображения...
            </div>
          )}

          {isDragging && (
            <div
              style={{
                position: 'absolute',
                inset: 0,
                background: 'rgba(122, 162, 247, 0.2)',
                border: '3px dashed var(--accent-primary)',
                borderRadius: 'var(--radius-md)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                pointerEvents: 'none',
              }}
            >
              <span
                style={{
                  fontSize: '1.5rem',
                  color: 'var(--accent-primary)',
                  background: 'var(--bg-elevated)',
                  padding: '1rem 2rem',
                  borderRadius: 'var(--radius-md)',
                }}
              >
                📷 Отпустите для загрузки
              </span>
            </div>
          )}
        </div>

        {/* Preview */}
        <div className="split-pane">
          <DocumentPreview html={renderedHtml} profile={profile} />
        </div>
      </div>
    </div>
  );
}

