interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorMessage({ message, onRetry }: ErrorMessageProps) {
  return (
    <div className="page flex items-center justify-center">
      <div style={{ textAlign: 'center' }}>
        <div className="text-muted" style={{ marginBottom: '1rem' }}>{message}</div>
        {onRetry && (
          <button className="btn btn-primary" onClick={onRetry}>
            Попробовать снова
          </button>
        )}
      </div>
    </div>
  );
}

