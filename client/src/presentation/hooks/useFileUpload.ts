import { useState } from 'react';

interface UseFileUploadOptions {
  onUpload: (file: File) => Promise<void>;
  accept?: string;
}

export function useFileUpload({ onUpload, accept }: UseFileUploadOptions) {
  const [uploading, setUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);

  const handleUpload = async (file: File) => {
    setUploading(true);
    try {
      await onUpload(file);
    } finally {
      setUploading(false);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      const file = files[0];
      if (!accept || file.type.match(accept)) {
        handleUpload(file);
      }
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    const items = e.clipboardData.items;
    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.kind === 'file') {
        const file = item.getAsFile();
        if (file && (!accept || file.type.match(accept))) {
          e.preventDefault();
          handleUpload(file);
          break;
        }
      }
    }
  };

  return {
    uploading,
    isDragging,
    handleDragOver,
    handleDragLeave,
    handleDrop,
    handlePaste,
  };
}

