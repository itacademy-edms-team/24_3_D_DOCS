interface TitlePageEditorHeaderProps {
  name: string;
  onNameChange: (name: string) => void;
  onSave: () => void;
  onGeneratePdf: () => void;
  onNavigateBack: () => void;
  saving: boolean;
  generating: boolean;
}

export function TitlePageEditorHeader({
  name,
  onNameChange,
  onSave,
  onGeneratePdf,
  onNavigateBack,
  saving,
  generating,
}: TitlePageEditorHeaderProps) {
  return (
    <div className="flex justify-between items-center mb-lg">
      <div>
        <input
          type="text"
          value={name}
          onChange={(e) => onNameChange(e.target.value)}
          style={{
            fontSize: '1.75rem',
            fontWeight: 700,
            border: 'none',
            background: 'transparent',
            padding: '0.25rem',
            width: '100%',
            maxWidth: '500px',
          }}
        />
      </div>
      <div style={{ display: 'flex', gap: '0.5rem' }}>
        <button
          className="btn btn-primary"
          onClick={onSave}
          disabled={saving}
        >
          {saving ? 'Сохранение...' : 'Сохранить'}
        </button>
        <button
          className="btn btn-primary"
          onClick={onGeneratePdf}
          disabled={generating}
        >
          {generating ? 'Генерация...' : 'Скачать PDF'}
        </button>
        <button
          className="btn btn-ghost"
          onClick={onNavigateBack}
        >
          Назад
        </button>
      </div>
    </div>
  );
}

