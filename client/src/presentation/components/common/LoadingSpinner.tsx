interface LoadingSpinnerProps {
  message?: string;
}

export function LoadingSpinner({ message = 'Загрузка...' }: LoadingSpinnerProps) {
  return (
    <div className="page flex items-center justify-center">
      <div className="text-muted">{message}</div>
    </div>
  );
}

