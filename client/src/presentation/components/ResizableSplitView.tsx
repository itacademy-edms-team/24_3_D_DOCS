import { useState, useRef, useEffect, ReactNode } from 'react';

interface ResizableSplitViewProps {
  left: ReactNode;
  right: ReactNode;
  initialLeftWidth?: number; // Percentage (0-100)
}

export function ResizableSplitView({ left, right, initialLeftWidth = 50 }: ResizableSplitViewProps) {
  const [leftWidth, setLeftWidth] = useState(initialLeftWidth);
  const [isDragging, setIsDragging] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const resizerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleMouseMove(e: MouseEvent) {
      if (!isDragging || !containerRef.current) return;

      const containerRect = containerRef.current.getBoundingClientRect();
      const newLeftWidth = ((e.clientX - containerRect.left) / containerRect.width) * 100;
      
      // Clamp between 20% and 80%
      const clampedWidth = Math.max(20, Math.min(80, newLeftWidth));
      setLeftWidth(clampedWidth);
    }

    function handleMouseUp() {
      setIsDragging(false);
    }

    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
    }

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
  }, [isDragging]);

  function handleMouseDown(e: React.MouseEvent) {
    e.preventDefault();
    setIsDragging(true);
  }

  return (
    <div
      ref={containerRef}
      className="split-view"
      style={{
        display: 'flex',
        flex: 1,
        overflow: 'hidden',
        position: 'relative',
      }}
    >
      {/* Left pane */}
      <div
        className="split-pane split-pane-left"
        style={{
          width: `${leftWidth}%`,
          overflow: 'auto',
          minWidth: 0,
        }}
      >
        {left}
      </div>

      {/* Resizer */}
      <div
        ref={resizerRef}
        onMouseDown={handleMouseDown}
        style={{
          width: '4px',
          background: isDragging ? 'var(--accent-primary, #7aa2f7)' : 'var(--border-color, #ddd)',
          cursor: 'col-resize',
          flexShrink: 0,
          position: 'relative',
          zIndex: 10,
        }}
      />

      {/* Right pane */}
      <div
        className="split-pane"
        style={{
          width: `${100 - leftWidth}%`,
          overflow: 'auto',
          minWidth: 0,
        }}
      >
        {right}
      </div>
    </div>
  );
}



