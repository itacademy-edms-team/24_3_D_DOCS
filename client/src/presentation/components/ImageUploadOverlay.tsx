interface ImageUploadOverlayProps {
  uploading: boolean;
  isDragging: boolean;
}

export function ImageUploadOverlay({ uploading, isDragging }: ImageUploadOverlayProps) {
  return (
    <>
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
    </>
  );
}

