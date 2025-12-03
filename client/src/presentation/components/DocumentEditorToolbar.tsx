import { RefObject } from 'react';
import type { Document, Profile } from '../../../../shared/src/types';
import { MarkdownFormattingToolbar } from './MarkdownFormattingToolbar';

interface DocumentEditorToolbarProps {
  document: Document;
  profiles: Profile[];
  onNameChange: (name: string) => void;
  onProfileChange: (profileId: string) => void;
  onSave: () => void;
  onGeneratePdf: () => void;
  onNavigateToCustomize: () => void;
  onNavigateBack: () => void;
  saving: boolean;
  generating: boolean;
  textareaRef: RefObject<HTMLTextAreaElement>;
  onContentChange: (content: string) => void;
  onOpenVariablesEditor?: () => void;
}

export function DocumentEditorToolbar({
  document,
  profiles,
  onNameChange,
  onProfileChange,
  onSave,
  onGeneratePdf,
  onNavigateToCustomize,
  onNavigateBack,
  saving,
  generating,
  textareaRef,
  onContentChange,
  onOpenVariablesEditor,
}: DocumentEditorToolbarProps) {
  return (
    <div className="toolbar">
      <div className="toolbar-group">
        <button className="btn btn-ghost btn-sm" onClick={onNavigateBack}>
          ← Назад
        </button>
        <span style={{ color: 'var(--text-muted)' }}>|</span>
        <input
          type="text"
          value={document.name}
          onChange={(e) => onNameChange(e.target.value)}
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
          onChange={(e) => onProfileChange(e.target.value)}
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

      <MarkdownFormattingToolbar
        textareaRef={textareaRef}
        onContentChange={onContentChange}
        onOpenVariablesEditor={onOpenVariablesEditor}
      />

      <div className="toolbar-group">
        <button
          className="btn btn-secondary btn-sm"
          onClick={onNavigateToCustomize}
        >
          ✏️ Редактировать стили
        </button>
        <button className="btn btn-secondary btn-sm" onClick={onSave} disabled={saving}>
          {saving ? '💾 Сохранение...' : '💾 Сохранить'}
        </button>
        <button
          className="btn btn-primary btn-sm"
          onClick={onGeneratePdf}
          disabled={generating}
        >
          {generating ? '⏳ Генерация...' : '📥 Скачать PDF'}
        </button>
      </div>
    </div>
  );
}

