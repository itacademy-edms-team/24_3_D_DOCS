import { useState, useMemo, DragEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import 'katex/dist/katex.min.css';
import { renderDocument } from '../../application/services/documentRenderer';
import { DocumentPreview } from '../components/DocumentPreview';
import { DocumentEditorToolbar } from '../components/DocumentEditorToolbar';
import { ImageUploadOverlay } from '../components/ImageUploadOverlay';
import { FrontmatterEditor } from '../components/FrontmatterEditor';
import { ResizableSplitView } from '../components/ResizableSplitView';
import { useDocumentEditor } from '../hooks/useDocumentEditor';
import { useMarkdownEditorShortcuts } from '../hooks/useMarkdownEditorShortcuts';
import { parseFrontmatter } from '../../utils/frontmatterUtils';

export function DocumentEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const {
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
  } = useDocumentEditor(id);

  const [isDragging, setIsDragging] = useState(false);
  const [isVariablesEditorOpen, setIsVariablesEditorOpen] = useState(false);

  // Setup keyboard shortcuts
  useMarkdownEditorShortcuts({
    textareaRef,
    onContentChange: handleContentChange,
    onUndo: handleUndo,
    onRedo: handleRedo,
  });

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

  // Extract frontmatter and variables
  const { variables: documentVariables, content: markdownContent } = useMemo(() => {
    if (!document) return { variables: {}, content: '' };
    return parseFrontmatter(document.content);
  }, [document?.content]);

  const renderedHtml = useMemo(() => {
    if (!document) return '';

    return renderDocument({
      markdown: markdownContent,
      profile,
      overrides: document.overrides || {},
      selectable: false,
    });
  }, [markdownContent, document?.overrides, profile]);

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
      <DocumentEditorToolbar
        document={document}
        profiles={profiles}
        onNameChange={handleNameChange}
        onProfileChange={handleProfileChange}
        onSave={handleSave}
        onGeneratePdf={handleGeneratePdf}
        onNavigateToCustomize={() => navigate(`/document/${id}/customize`)}
        onNavigateBack={() => navigate('/')}
        saving={saving}
        generating={generating}
        textareaRef={textareaRef}
        onContentChange={handleContentChange}
        onOpenVariablesEditor={() => setIsVariablesEditorOpen(true)}
      />

      {/* Split view */}
      <ResizableSplitView
        left={
          <div style={{ position: 'relative', height: '100%' }}>
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
                width: '100%',
                height: '100%',
                borderColor: isDragging ? 'var(--accent-primary)' : undefined,
                background: isDragging ? 'rgba(122, 162, 247, 0.1)' : undefined,
              }}
            />

            <ImageUploadOverlay uploading={uploading} isDragging={isDragging} />
          </div>
        }
        right={
          <DocumentPreview html={renderedHtml} profile={profile} documentVariables={documentVariables} />
        }
        initialLeftWidth={50}
      />

      {/* Frontmatter Editor Modal */}
      {isVariablesEditorOpen && document && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
          }}
          onClick={() => setIsVariablesEditorOpen(false)}
        >
          <div
            style={{
              background: 'white',
              borderRadius: '8px',
              padding: '1.5rem',
              maxWidth: '900px',
              maxHeight: '80vh',
              overflow: 'auto',
              width: '90%',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h2 style={{ margin: 0, fontSize: '1.25rem' }}>Редактор переменных Frontmatter</h2>
              <button
                onClick={() => setIsVariablesEditorOpen(false)}
                style={{
                  padding: '0.25rem 0.5rem',
                  background: 'transparent',
                  border: '1px solid #ddd',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '1.25rem',
                }}
              >
                ×
              </button>
            </div>
            <FrontmatterEditor
              markdown={document.content}
              onUpdate={(newContent) => {
                handleContentChange(newContent);
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
}
