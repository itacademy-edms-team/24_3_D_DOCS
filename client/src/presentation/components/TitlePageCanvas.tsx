import { useEffect, useCallback } from 'react';
import type { TitlePageElement, TitlePageElementType } from '../../../../shared/src/types';
import { mmToPx, PAGE_WIDTH_MM, PAGE_HEIGHT_MM } from '../../application/services/canvasUtils';
import { drawPageBackground, drawGrid, drawElement, drawHover, drawSelection, drawAlignmentGuides, drawDistanceLines } from '../../application/services/canvasRenderer';
import { useTitlePageCanvas } from './hooks/useTitlePageCanvas';
import { TitlePageCanvasToolbar } from './TitlePageCanvasToolbar';
import { TitlePageCanvasRuler } from './TitlePageCanvasRuler';

interface TitlePageCanvasProps {
  elements: TitlePageElement[];
  selectedElementIds: string[];
  onElementSelect: (ids: string[]) => void;
  onElementToggle: (id: string) => void;
  onElementMove: (id: string, x: number, y: number) => void;
  onElementsMove: (ids: string[], deltaX: number, deltaY: number) => void;
  onElementAdd: (type: TitlePageElementType, x: number, y: number) => void;
  onMoveEnd?: () => void;
  onToolChange?: (tool: TitlePageElementType | null) => void;
  initialTool?: TitlePageElementType | null;
  onGridToggle?: () => void;
}

export function TitlePageCanvas({
  elements,
  selectedElementIds,
  onElementSelect,
  onElementToggle,
  onElementMove,
  onElementsMove,
  onElementAdd,
  onMoveEnd,
  onToolChange,
  initialTool,
  onGridToggle,
}: TitlePageCanvasProps) {
  const {
    canvasRef,
    tool,
    zoom,
    showGrid,
    mousePos,
    hoveredElementId,
    gridSize,
    isDragging,
    isSelecting,
    selectionStart,
    selectionEnd,
    alignmentGuides,
    elementDistances,
    handleToolChange,
    handleGridToggle,
    handleZoomChange,
    handleMouseDown,
    handleMouseMove,
    handleCanvasMouseMove,
    handleMouseUp,
    handleMouseLeave,
  } = useTitlePageCanvas({
    elements,
    selectedElementIds,
    onElementSelect,
    onElementToggle,
    onElementMove,
    onElementsMove,
    onElementAdd,
    onMoveEnd,
    onToolChange,
    initialTool,
    onGridToggle,
  });

  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw page background and border
    drawPageBackground(ctx);

    // Draw grid if enabled
    if (showGrid) {
      drawGrid(ctx, gridSize);
    }

    // Draw elements
    elements.forEach((element) => {
      drawElement(element, ctx);

      // Draw hover border (lighter than selection)
      const isHovered = element.id === hoveredElementId && !selectedElementIds.includes(element.id);
      if (isHovered) {
        drawHover(element, ctx);
      }

      // Draw selection border
      if (selectedElementIds.includes(element.id)) {
        drawSelection(element, ctx);
      }
    });

    // Draw alignment guides (on top of everything)
    if (alignmentGuides.length > 0) {
      drawAlignmentGuides(alignmentGuides, ctx);
    }

    // Draw distance lines if dragging a single element
    if (isDragging && elementDistances && selectedElementIds.length === 1) {
      const draggedElement = elements.find(el => el.id === selectedElementIds[0]);
      if (draggedElement) {
        drawDistanceLines(draggedElement, elementDistances, ctx);
      }
    }
  }, [elements, selectedElementIds, hoveredElementId, showGrid, gridSize, alignmentGuides, isDragging, elementDistances]);

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

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      {/* Toolbar */}
      <TitlePageCanvasToolbar
        tool={tool}
        onToolChange={handleToolChange}
        showGrid={showGrid}
        onGridToggle={handleGridToggle}
        zoom={zoom}
        onZoomChange={handleZoomChange}
      />

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
          <TitlePageCanvasRuler orientation="horizontal" mousePos={mousePos} />

          {/* Vertical Ruler */}
          <TitlePageCanvasRuler orientation="vertical" mousePos={mousePos} />

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
            {/* Selection box overlay */}
            {isSelecting && selectionStart && selectionEnd && (
              <div
                style={{
                  position: 'absolute',
                  left: `${Math.min(selectionStart.x, selectionEnd.x) * zoom}px`,
                  top: `${Math.min(selectionStart.y, selectionEnd.y) * zoom}px`,
                  width: `${Math.abs(selectionEnd.x - selectionStart.x) * zoom}px`,
                  height: `${Math.abs(selectionEnd.y - selectionStart.y) * zoom}px`,
                  border: '1px dashed #0066ff',
                  background: 'rgba(0, 102, 255, 0.1)',
                  pointerEvents: 'none',
                }}
              />
            )}
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
