import { useEffect, useRef } from 'react';

interface UseAutoSaveOptions {
  onSave: () => Promise<void> | void;
  delay?: number; // milliseconds
  enabled?: boolean;
}

export function useAutoSave({ onSave, delay = 2000, enabled = true }: UseAutoSaveOptions) {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  const scheduleSave = () => {
    if (!enabled) return;

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      onSave();
    }, delay);
  };

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return { scheduleSave };
}

