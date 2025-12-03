import { useState, useEffect, useRef, useCallback } from 'react';
import type { TitlePageElement, TitlePageElementType } from '../../../../shared/src/types';
import { getCanvasCoords, pxToMm, getDistanceToElement, mmToPx, findAlignmentGuides, applySnap, calculateElementDistances, type AlignmentGuide } from '../../../application/services/canvasUtils';
import { findElementAtPoint, calculateLineHitbox, calculateTextHitbox } from '../../../application/services/canvasHitDetection';
import type { ElementDistances } from '../../../application/services/canvasRenderer';

interface UseTitlePageCanvasProps {
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

export function useTitlePageCanvas({
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
}: UseTitlePageCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState<{ 
    x: number; 
    y: number; 
    elementIds: string[];
    elementPositions: Map<string, { x: number; y: number }>;
  } | null>(null);
  const [isSelecting, setIsSelecting] = useState(false);
  const [selectionStart, setSelectionStart] = useState<{ x: number; y: number } | null>(null);
  const [selectionEnd, setSelectionEnd] = useState<{ x: number; y: number } | null>(null);
  const [tool, setTool] = useState<TitlePageElementType | null>(initialTool || null);
  const [zoom, setZoom] = useState(1.0); // Start at 100%
  const [mousePos, setMousePos] = useState<{ x: number; y: number } | null>(null);
  const [hoveredElementId, setHoveredElementId] = useState<string | null>(null);
  const [gridSize] = useState(5); // Grid size in mm
  const [showGrid, setShowGrid] = useState(false);
  const [alignmentGuides, setAlignmentGuides] = useState<AlignmentGuide[]>([]);
  const [isAltPressed, setIsAltPressed] = useState(false);
  const [elementDistances, setElementDistances] = useState<ElementDistances | null>(null);

  // Sync tool with prop
  useEffect(() => {
    if (initialTool !== undefined) {
      setTool(initialTool);
    }
  }, [initialTool]);

  // Keyboard shortcut for grid toggle and Alt key tracking
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'g' || e.key === 'G') {
        const target = e.target as HTMLElement;
        if (target.tagName !== 'INPUT' && target.tagName !== 'TEXTAREA' && !target.isContentEditable) {
          e.preventDefault();
          setShowGrid(!showGrid);
          onGridToggle?.();
        }
      }
      if (e.key === 'Alt') {
        setIsAltPressed(true);
      }
    };

    const handleKeyUp = (e: KeyboardEvent) => {
      if (e.key === 'Alt') {
        setIsAltPressed(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, [showGrid, onGridToggle]);

  const handleToolChange = useCallback((newTool: TitlePageElementType | null) => {
    setTool(newTool);
    onToolChange?.(newTool);
  }, [onToolChange]);

  const handleGridToggle = useCallback(() => {
    setShowGrid(!showGrid);
    onGridToggle?.();
  }, [showGrid, onGridToggle]);

  const handleZoomChange = useCallback((newZoom: number) => {
    setZoom(newZoom);
  }, []);

  const handleMouseDown = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const coords = getCanvasCoords(e, canvas, zoom);
    const x = pxToMm(coords.x);
    const y = pxToMm(coords.y);

    const clickedElement = findElementAtPoint(x, y, elements, ctx, getDistanceToElement);

    if (tool) {
      // Add new element
      onElementAdd(tool, x, y);
      setTool(null);
      handleToolChange(null);
    } else if (clickedElement) {
      const isCtrlPressed = e.ctrlKey || e.metaKey;
      
      if (isCtrlPressed) {
        // Toggle selection with Ctrl
        onElementToggle(clickedElement.id);
        // If element is now selected, prepare for dragging
        if (!selectedElementIds.includes(clickedElement.id)) {
          const newSelection = [...selectedElementIds, clickedElement.id];
          const elementPositions = new Map<string, { x: number; y: number }>();
          newSelection.forEach(id => {
            const el = elements.find(e => e.id === id);
            if (el) elementPositions.set(id, { x: el.x, y: el.y });
          });
          setIsDragging(true);
          setDragStart({ x: coords.x, y: coords.y, elementIds: newSelection, elementPositions });
        } else {
          // Element was deselected, don't start dragging
          setIsDragging(false);
          setDragStart(null);
        }
      } else {
        // Normal click - select element(s)
        if (selectedElementIds.includes(clickedElement.id)) {
          // Already selected, prepare for dragging all selected
          const elementPositions = new Map<string, { x: number; y: number }>();
          selectedElementIds.forEach(id => {
            const el = elements.find(e => e.id === id);
            if (el) elementPositions.set(id, { x: el.x, y: el.y });
          });
          setIsDragging(true);
          setDragStart({ x: coords.x, y: coords.y, elementIds: selectedElementIds, elementPositions });
        } else {
          // Select only this element
          onElementSelect([clickedElement.id]);
          const elementPositions = new Map<string, { x: number; y: number }>();
          const el = elements.find(e => e.id === clickedElement.id);
          if (el) elementPositions.set(clickedElement.id, { x: el.x, y: el.y });
          setIsDragging(true);
          setDragStart({ x: coords.x, y: coords.y, elementIds: [clickedElement.id], elementPositions });
        }
      }
    } else {
      // Click on empty space - start selection box or deselect
      if (e.ctrlKey || e.metaKey) {
        // Ctrl+click on empty space - keep selection, start selection box
        setIsSelecting(true);
        setSelectionStart({ x: coords.x, y: coords.y });
        setSelectionEnd({ x: coords.x, y: coords.y });
      } else {
        // Normal click on empty space - deselect and start selection box
        onElementSelect([]);
        setIsSelecting(true);
        setSelectionStart({ x: coords.x, y: coords.y });
        setSelectionEnd({ x: coords.x, y: coords.y });
      }
    }
  }, [tool, zoom, elements, selectedElementIds, onElementAdd, onElementSelect, onElementToggle, handleToolChange]);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const coords = getCanvasCoords(e, canvas, zoom);
    const mmX = pxToMm(coords.x);
    const mmY = pxToMm(coords.y);
    setMousePos({ x: mmX, y: mmY });

    // Handle selection box
    if (isSelecting && selectionStart) {
      setSelectionEnd({ x: coords.x, y: coords.y });
    }

    // Update hover state if not dragging and not selecting
    if (!isDragging && !isSelecting) {
      const hoveredElement = findElementAtPoint(mmX, mmY, elements, ctx, getDistanceToElement);
      setHoveredElementId(hoveredElement?.id || null);
    }

    // Handle dragging
    if (isDragging && dragStart) {
      const deltaX = pxToMm(coords.x - dragStart.x);
      const deltaY = pxToMm(coords.y - dragStart.y);

      // Move all selected elements
      if (dragStart.elementIds.length === 1) {
        const element = elements.find((el) => el.id === dragStart.elementIds[0]);
        if (element) {
          const originalPos = dragStart.elementPositions.get(element.id);
          if (originalPos) {
            let newX = originalPos.x + deltaX;
            let newY = originalPos.y + deltaY;
            
            // Apply snap if Alt is not pressed
            if (!isAltPressed) {
              const tempElement = { ...element, x: newX, y: newY };
              // Exclude the moving element itself from alignment check
              const guides = findAlignmentGuides(tempElement, elements, ctx, true, [element.id]);
              setAlignmentGuides(guides);
              
              if (guides.length > 0) {
                const snapped = applySnap(element, newX, newY, guides, ctx);
                newX = snapped.x;
                newY = snapped.y;
              }
            } else {
              setAlignmentGuides([]);
            }
            
            // Calculate and set distances for the element at new position
            const tempElementForDistances = { ...element, x: newX, y: newY };
            const distances = calculateElementDistances(tempElementForDistances, ctx);
            setElementDistances(distances);
            
            onElementMove(element.id, newX, newY);
          }
        }
      } else {
        // Multiple elements - move all relative to their original positions
        // For multiple elements, we can apply snap to the first element
        // and move others relative to it
        const firstElement = elements.find((el) => el.id === dragStart.elementIds[0]);
        if (firstElement && !isAltPressed) {
          const originalPos = dragStart.elementPositions.get(firstElement.id);
          if (originalPos) {
            let newX = originalPos.x + deltaX;
            let newY = originalPos.y + deltaY;
            
            const tempElement = { ...firstElement, x: newX, y: newY };
            // Exclude all elements in the moving group from alignment check to prevent jitter
            const guides = findAlignmentGuides(tempElement, elements, ctx, true, dragStart.elementIds);
            setAlignmentGuides(guides);
            
            if (guides.length > 0) {
              const snapped = applySnap(firstElement, newX, newY, guides, ctx);
              const adjustedDeltaX = snapped.x - originalPos.x;
              const adjustedDeltaY = snapped.y - originalPos.y;
              onElementsMove(dragStart.elementIds, adjustedDeltaX, adjustedDeltaY);
            } else {
              onElementsMove(dragStart.elementIds, deltaX, deltaY);
            }
          } else {
            onElementsMove(dragStart.elementIds, deltaX, deltaY);
          }
        } else {
          setAlignmentGuides([]);
          onElementsMove(dragStart.elementIds, deltaX, deltaY);
        }
      }
    }
  }, [zoom, isDragging, isSelecting, dragStart, selectionStart, elements, onElementMove, onElementsMove]);

  const handleCanvasMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
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
  }, [zoom]);

  const handleMouseUp = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    setAlignmentGuides([]);
    setElementDistances(null);
    
    // Notify that move ended
    if (isDragging) {
      onMoveEnd?.();
    }
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Handle selection box completion
    if (isSelecting && selectionStart && selectionEnd) {
      const minX = Math.min(selectionStart.x, selectionEnd.x);
      const maxX = Math.max(selectionStart.x, selectionEnd.x);
      const minY = Math.min(selectionStart.y, selectionEnd.y);
      const maxY = Math.max(selectionStart.y, selectionEnd.y);
      
      // Convert selection box to mm for comparison
      const minXmm = pxToMm(minX);
      const maxXmm = pxToMm(maxX);
      const minYmm = pxToMm(minY);
      const maxYmm = pxToMm(maxY);
      
      const selectedIds: string[] = [];
      elements.forEach(el => {
        // Check if element bounding box intersects with selection box
        let hitbox;
        if (el.type === 'line') {
          hitbox = calculateLineHitbox(el, 0);
        } else {
          hitbox = calculateTextHitbox(el, ctx, 0);
        }
        
        // Check if element bounding box intersects with selection box
        const intersects = !(
          hitbox.maxX < minXmm || 
          hitbox.minX > maxXmm || 
          hitbox.maxY < minYmm || 
          hitbox.minY > maxYmm
        );
        
        if (intersects) {
          selectedIds.push(el.id);
        }
      });
      
      if (selectedIds.length > 0) {
        onElementSelect(selectedIds);
      } else if (!e.ctrlKey && !e.metaKey) {
        // If no elements selected and not Ctrl, deselect all
        onElementSelect([]);
      }
    }
    
    setIsDragging(false);
    setDragStart(null);
    setIsSelecting(false);
    setSelectionStart(null);
    setSelectionEnd(null);
  }, [isSelecting, selectionStart, selectionEnd, elements, onElementSelect]);

  const handleMouseLeave = useCallback(() => {
    setIsDragging(false);
    setDragStart(null);
    setIsSelecting(false);
    setSelectionStart(null);
    setSelectionEnd(null);
    setAlignmentGuides([]);
    setElementDistances(null);
    setMousePos(null);
    setHoveredElementId(null);
  }, []);

  return {
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
  };
}

