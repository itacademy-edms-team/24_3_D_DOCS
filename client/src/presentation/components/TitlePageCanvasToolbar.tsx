import type { TitlePageElementType } from '../../../../shared/src/types';

interface TitlePageCanvasToolbarProps {
  tool: TitlePageElementType | null;
  onToolChange: (tool: TitlePageElementType | null) => void;
  showGrid: boolean;
  onGridToggle: () => void;
  zoom: number;
  onZoomChange: (zoom: number) => void;
}

export function TitlePageCanvasToolbar({
  tool,
  onToolChange,
  showGrid,
  onGridToggle,
  zoom,
  onZoomChange,
}: TitlePageCanvasToolbarProps) {
  const handleToolClick = (toolType: TitlePageElementType) => {
    const newTool = tool === toolType ? null : toolType;
    onToolChange(newTool);
  };

  return (
    <div style={{ display: 'flex', gap: '0.5rem', padding: '0.5rem', border: '1px solid #ddd', borderRadius: '4px', alignItems: 'center', flexWrap: 'wrap' }}>
      <button
        onClick={() => handleToolClick('text')}
        style={{
          padding: '0.5rem 1rem',
          background: tool === 'text' ? '#0066ff' : '#f0f0f0',
          color: tool === 'text' ? '#fff' : '#000',
          border: '1px solid #ddd',
          borderRadius: '4px',
          cursor: 'pointer',
        }}
      >
        Текст
      </button>
      <button
        onClick={() => handleToolClick('variable')}
        style={{
          padding: '0.5rem 1rem',
          background: tool === 'variable' ? '#0066ff' : '#f0f0f0',
          color: tool === 'variable' ? '#fff' : '#000',
          border: '1px solid #ddd',
          borderRadius: '4px',
          cursor: 'pointer',
        }}
      >
        Переменная
      </button>
      <button
        onClick={() => handleToolClick('line')}
        style={{
          padding: '0.5rem 1rem',
          background: tool === 'line' ? '#0066ff' : '#f0f0f0',
          color: tool === 'line' ? '#fff' : '#000',
          border: '1px solid #ddd',
          borderRadius: '4px',
          cursor: 'pointer',
        }}
      >
        Линия
      </button>
      <button
        onClick={onGridToggle}
        style={{
          padding: '0.5rem 1rem',
          background: showGrid ? '#0066ff' : '#f0f0f0',
          color: showGrid ? '#fff' : '#000',
          border: '1px solid #ddd',
          borderRadius: '4px',
          cursor: 'pointer',
          marginLeft: '0.5rem',
        }}
        title="Toggle grid (G)"
      >
        Сетка
      </button>
      
      {/* Zoom controls */}
      <div style={{ marginLeft: 'auto', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <button
          onClick={() => onZoomChange(Math.max(0.25, zoom - 0.1))}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#f0f0f0',
            border: '1px solid #ddd',
            borderRadius: '4px',
            cursor: 'pointer',
          }}
        >
          −
        </button>
        <span style={{ minWidth: '60px', textAlign: 'center', fontSize: '0.875rem' }}>
          {Math.round(zoom * 100)}%
        </span>
        <button
          onClick={() => onZoomChange(Math.min(2, zoom + 0.1))}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#f0f0f0',
            border: '1px solid #ddd',
            borderRadius: '4px',
            cursor: 'pointer',
          }}
        >
          +
        </button>
        <button
          onClick={() => onZoomChange(1)}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#f0f0f0',
            border: '1px solid #ddd',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.75rem',
          }}
        >
          Сброс
        </button>
      </div>
    </div>
  );
}

