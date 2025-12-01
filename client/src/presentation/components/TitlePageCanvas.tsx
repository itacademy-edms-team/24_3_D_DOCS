import { useRef, useEffect, useState, useCallback } from 'react';
import type { TitlePageElement, TitlePageElementType } from '../../../../shared/src/types';

interface TitlePageCanvasProps {
  elements: TitlePageElement[];
  selectedElementId: string | null;
  onElementSelect: (id: string | null) => void;
  onElementMove: (id: string, x: number, y: number) => void;
  onElementAdd: (type: TitlePageElementType, x: number, y: number) => void;
}

// A4 dimensions in mm
const PAGE_WIDTH_MM = 210;
const PAGE_HEIGHT_MM = 297;
// Conversion factor: 1mm ≈ 3.78 pixels at 96dpi (same as DocumentPreview)
const MM_TO_PX = 3.7795275591;

export function TitlePageCanvas({
  elements,
  selectedElementId,
  onElementSelect,
  onElementMove,
  onElementAdd,
}: TitlePageCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState<{ x: number; y: number; elementId: string } | null>(null);
  const [tool, setTool] = useState<TitlePageElementType | null>(null);
  const [zoom, setZoom] = useState(0.5); // Start at 50% to fit on screen
  const [mousePos, setMousePos] = useState<{ x: number; y: number } | null>(null);

  const mmToPx = (mm: number) => mm * MM_TO_PX;
  const pxToMm = (px: number) => px / MM_TO_PX;

  const getCanvasCoords = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };
    const rect = canvas.getBoundingClientRect();
    // Account for zoom scale
    return {
      x: (e.clientX - rect.left) / zoom,
      y: (e.clientY - rect.top) / zoom,
    };
  };

  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = mmToPx(PAGE_WIDTH_MM);
    const height = mmToPx(PAGE_HEIGHT_MM);

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw page background
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, width, height);

    // Draw border
    ctx.strokeStyle = '#cccccc';
    ctx.lineWidth = 1;
    ctx.strokeRect(0, 0, width, height);

    // Draw elements
    elements.forEach((element) => {
      const x = mmToPx(element.x);
      const y = mmToPx(element.y);
      const isSelected = element.id === selectedElementId;

      if (element.type === 'line') {
        const length = mmToPx(element.length || 100);
        const thickness = mmToPx(element.thickness || 1);
        ctx.fillStyle = '#000000';
        ctx.fillRect(x, y, length, thickness);
      } else if (element.type === 'text' || element.type === 'variable') {
        ctx.font = `${element.fontWeight || 'normal'} ${element.fontSize || 14}pt ${element.fontFamily || 'Times New Roman'}`;
        ctx.fillStyle = '#000000';
        ctx.textBaseline = 'top';
        
        const content = element.type === 'variable' 
          ? `{${element.variableKey || ''}}`
          : (element.content || '');
        
        const lines = content.split('\n');
        const textAlign = element.textAlign || 'left';
        const fontSize = element.fontSize || 14;
        const lineHeight = fontSize * 1.2;
        
        lines.forEach((line, i) => {
          // Calculate x position based on alignment
          let textX = x;
          if (textAlign === 'center') {
            const metrics = ctx.measureText(line);
            textX = x - metrics.width / 2;
            ctx.textAlign = 'left';
            ctx.fillText(line, textX, y + i * lineHeight);
          } else if (textAlign === 'right') {
            const metrics = ctx.measureText(line);
            textX = x - metrics.width;
            ctx.textAlign = 'left';
            ctx.fillText(line, textX, y + i * lineHeight);
          } else {
            ctx.textAlign = 'left';
            ctx.fillText(line, x, y + i * lineHeight);
          }
        });
      }

      // Draw selection border
      if (isSelected) {
        ctx.strokeStyle = '#0066ff';
        ctx.lineWidth = 2;
        ctx.setLineDash([5, 5]);
        
        if (element.type === 'line') {
          const length = mmToPx(element.length || 100);
          const thickness = mmToPx(element.thickness || 1);
          ctx.strokeRect(x - 2, y - 2, length + 4, thickness + 4);
        } else {
          // Calculate selection bounds with proper alignment
          const content = element.type === 'variable' 
            ? `{${element.variableKey || ''}}`
            : (element.content || '');
          const lines = content.split('\n');
          const fontSize = element.fontSize || 14;
          const lineHeight = fontSize * 1.2;
          const totalHeight = lines.length * lineHeight;
          
          let maxWidth = 0;
          lines.forEach(line => {
            const metrics = ctx.measureText(line);
            if (metrics.width > maxWidth) maxWidth = metrics.width;
          });
          
          const textAlign = element.textAlign || 'left';
          let textLeft = x;
          if (textAlign === 'center') {
            textLeft = x - maxWidth / 2;
          } else if (textAlign === 'right') {
            textLeft = x - maxWidth;
          }
          
          ctx.strokeRect(textLeft - 2, y - 2, maxWidth + 4, totalHeight + 4);
        }
        
        ctx.setLineDash([]);
      }
    });
  }, [elements, selectedElementId]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const width = mmToPx(PAGE_WIDTH_MM);
    const height = mmToPx(PAGE_HEIGHT_MM);

    canvas.width = width;
    canvas.height = height;
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;

    draw();
  }, [draw]);

  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const coords = getCanvasCoords(e);
    const x = pxToMm(coords.x);
    const y = pxToMm(coords.y);

    // Check if clicking on an element
    let clickedElement: TitlePageElement | null = null;
    for (let i = elements.length - 1; i >= 0; i--) {
      const el = elements[i];
      if (el.type === 'line') {
        const length = el.length || 100;
        const thickness = el.thickness || 1;
        // Expanded hitbox for easier selection - 5mm padding on all sides
        const hitboxPadding = 5;
        const minX = el.x - hitboxPadding;
        const maxX = el.x + length + hitboxPadding;
        const minY = el.y - hitboxPadding;
        const maxY = el.y + thickness + hitboxPadding;
        
        if (x >= minX && x <= maxX && y >= minY && y <= maxY) {
          clickedElement = el;
          break;
        }
      } else {
        // Calculate text bounds with proper alignment
        const canvas = canvasRef.current;
        if (!canvas) continue;
        const ctx = canvas.getContext('2d');
        if (!ctx) continue;
        
        const fontSize = el.fontSize || 14;
        const fontFamily = el.fontFamily || 'Times New Roman';
        const fontWeight = el.fontWeight || 'normal';
        ctx.font = `${fontWeight} ${fontSize}pt ${fontFamily}`;
        
        const content = el.type === 'variable' 
          ? `{${el.variableKey || ''}}`
          : (el.content || '');
        
        const lines = content.split('\n');
        const textAlign = el.textAlign || 'left';
        const lineHeight = fontSize * 1.2;
        const totalHeight = lines.length * lineHeight;
        
        // Find max width and calculate bounds
        let maxWidth = 0;
        lines.forEach(line => {
          const metrics = ctx.measureText(line);
          if (metrics.width > maxWidth) maxWidth = metrics.width;
        });
        
        let textLeft = el.x;
        if (textAlign === 'center') {
          textLeft = el.x - maxWidth / 2;
        } else if (textAlign === 'right') {
          textLeft = el.x - maxWidth;
        }
        
        const textRight = textLeft + maxWidth;
        const textTop = el.y;
        const textBottom = el.y + totalHeight;
        
        if (x >= textLeft && x <= textRight && y >= textTop && y <= textBottom) {
          clickedElement = el;
          break;
        }
      }
    }

    if (tool) {
      // Add new element
      onElementAdd(tool, x, y);
      setTool(null);
    } else if (clickedElement) {
      // Start dragging
      setIsDragging(true);
      setDragStart({ x: coords.x, y: coords.y, elementId: clickedElement.id });
      onElementSelect(clickedElement.id);
    } else {
      // Deselect
      onElementSelect(null);
    }
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const coords = getCanvasCoords(e);
    const mmX = pxToMm(coords.x);
    const mmY = pxToMm(coords.y);
    setMousePos({ x: mmX, y: mmY });

    if (!isDragging || !dragStart) return;

    const deltaX = pxToMm(coords.x - dragStart.x);
    const deltaY = pxToMm(coords.y - dragStart.y);

    const element = elements.find((el) => el.id === dragStart.elementId);
    if (element) {
      onElementMove(dragStart.elementId, element.x + deltaX, element.y + deltaY);
      setDragStart({ x: coords.x, y: coords.y, elementId: dragStart.elementId });
    }
  };

  const handleCanvasMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const rect = canvas.getBoundingClientRect();
    const coords = {
      x: (e.clientX - rect.left) / zoom,
      y: (e.clientY - rect.top) / zoom,
    };
    const mmX = pxToMm(coords.x);
    const mmY = pxToMm(coords.y);
    setMousePos({ x: mmX, y: mmY });
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    setDragStart(null);
  };

  const handleMouseLeave = () => {
    setIsDragging(false);
    setDragStart(null);
    setMousePos(null);
  };

  // Render ruler marks
  const renderRuler = (orientation: 'horizontal' | 'vertical') => {
    const marks: JSX.Element[] = [];
    const length = orientation === 'horizontal' ? PAGE_WIDTH_MM : PAGE_HEIGHT_MM;
    const step = length > 200 ? 10 : length > 100 ? 5 : 1; // Adaptive step size
    
    for (let i = 0; i <= length; i += step) {
      const isMajor = i % (step * 5) === 0;
      const size = isMajor ? 15 : 8;
      const label = isMajor ? i.toString() : '';
      
      if (orientation === 'horizontal') {
        marks.push(
          <div
            key={i}
            style={{
              position: 'absolute',
              left: `${mmToPx(i)}px`,
              top: 0,
              width: '1px',
              height: `${size}px`,
              background: '#666',
            }}
          />
        );
        if (label) {
          marks.push(
            <div
              key={`label-${i}`}
              style={{
                position: 'absolute',
                left: `${mmToPx(i) + 2}px`,
                top: 2,
                fontSize: '10px',
                color: '#666',
                pointerEvents: 'none',
              }}
            >
              {label}
            </div>
          );
        }
      } else {
        marks.push(
          <div
            key={i}
            style={{
              position: 'absolute',
              top: `${mmToPx(i)}px`,
              left: 0,
              height: '1px',
              width: `${size}px`,
              background: '#666',
            }}
          />
        );
        if (label) {
          marks.push(
            <div
              key={`label-${i}`}
              style={{
                position: 'absolute',
                top: `${mmToPx(i) + 2}px`,
                left: 2,
                fontSize: '10px',
                color: '#666',
                pointerEvents: 'none',
                transform: 'rotate(-90deg)',
                transformOrigin: 'top left',
              }}
            >
              {label}
            </div>
          );
        }
      }
    }
    return marks;
  };

  return (
    <div ref={containerRef} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      {/* Toolbar */}
      <div style={{ display: 'flex', gap: '0.5rem', padding: '0.5rem', border: '1px solid #ddd', borderRadius: '4px', alignItems: 'center', flexWrap: 'wrap' }}>
        <button
          onClick={() => setTool(tool === 'text' ? null : 'text')}
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
          onClick={() => setTool(tool === 'variable' ? null : 'variable')}
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
          onClick={() => setTool(tool === 'line' ? null : 'line')}
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
        
        {/* Zoom controls */}
        <div style={{ marginLeft: 'auto', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <button
            onClick={() => setZoom(Math.max(0.25, zoom - 0.1))}
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
            onClick={() => setZoom(Math.min(2, zoom + 0.1))}
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
            onClick={() => setZoom(1)}
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

      {/* Canvas with Rulers */}
      <div style={{ 
        border: '1px solid #ddd', 
        display: 'inline-block', 
        background: '#f5f5f5', 
        padding: '20px',
        overflow: 'auto',
        maxWidth: '100%',
      }}>
        <div style={{
          transform: `scale(${zoom})`,
          transformOrigin: 'top left',
          display: 'inline-block',
          position: 'relative',
        }}>
          {/* Horizontal Ruler */}
          <div
            style={{
              position: 'absolute',
              top: '-20px',
              left: 0,
              width: `${mmToPx(PAGE_WIDTH_MM)}px`,
              height: '20px',
              background: '#e8e8e8',
              borderBottom: '1px solid #ccc',
            }}
          >
            {renderRuler('horizontal')}
            {mousePos && (
              <div
                style={{
                  position: 'absolute',
                  left: `${mmToPx(mousePos.x)}px`,
                  top: 0,
                  width: '1px',
                  height: '20px',
                  background: '#0066ff',
                  pointerEvents: 'none',
                }}
              />
            )}
          </div>

          {/* Vertical Ruler */}
          <div
            style={{
              position: 'absolute',
              top: 0,
              left: '-20px',
              width: '20px',
              height: `${mmToPx(PAGE_HEIGHT_MM)}px`,
              background: '#e8e8e8',
              borderRight: '1px solid #ccc',
            }}
          >
            {renderRuler('vertical')}
            {mousePos && (
              <div
                style={{
                  position: 'absolute',
                  top: `${mmToPx(mousePos.y)}px`,
                  left: 0,
                  width: '20px',
                  height: '1px',
                  background: '#0066ff',
                  pointerEvents: 'none',
                }}
              />
            )}
          </div>

          {/* Corner */}
          <div
            style={{
              position: 'absolute',
              top: '-20px',
              left: '-20px',
              width: '20px',
              height: '20px',
              background: '#d0d0d0',
              borderRight: '1px solid #ccc',
              borderBottom: '1px solid #ccc',
            }}
          />

          {/* Canvas container with mouse tracking */}
          <div 
            style={{ marginTop: '20px', marginLeft: '20px', position: 'relative' }}
            onMouseMove={handleCanvasMouseMove}
            onMouseLeave={handleMouseLeave}
          >
            <canvas
              ref={canvasRef}
              onMouseDown={handleMouseDown}
              onMouseMove={handleMouseMove}
              onMouseUp={handleMouseUp}
              style={{ cursor: tool ? 'crosshair' : isDragging ? 'move' : 'default', display: 'block' }}
            />
          </div>

          {/* Position indicator */}
          {mousePos && (
            <div
              style={{
                position: 'absolute',
                top: '-20px',
                right: 0,
                background: '#0066ff',
                color: '#fff',
                padding: '2px 6px',
                fontSize: '10px',
                borderRadius: '2px',
                pointerEvents: 'none',
                whiteSpace: 'nowrap',
              }}
            >
              X: {mousePos.x.toFixed(1)}mm Y: {mousePos.y.toFixed(1)}mm
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

