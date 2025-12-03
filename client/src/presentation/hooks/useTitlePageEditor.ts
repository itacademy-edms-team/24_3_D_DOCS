import { useState, useEffect, useCallback, useRef } from 'react';
import type { TitlePage, TitlePageElement, TitlePageElementType } from '../../../../shared/src/types';
import { titlePageApi } from '../../infrastructure/api/titlePageApi';
import { v4 as uuidv4 } from 'uuid';

export function useTitlePageEditor(pageId: string | undefined) {
  const [titlePage, setTitlePage] = useState<TitlePage | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [selectedElementIds, setSelectedElementIds] = useState<string[]>([]);
  const clipboardRef = useRef<TitlePageElement[]>([]);
  
  // Undo/Redo history
  const historyRef = useRef<TitlePage[]>([]);
  const historyIndexRef = useRef<number>(-1);
  const MAX_HISTORY = 50;

  const loadTitlePage = useCallback(async (id: string) => {
    try {
      setLoading(true);
      const data = await titlePageApi.getById(id);
      setTitlePage(data);
    } catch (error) {
      console.error('Failed to load title page:', error);
      alert('Ошибка загрузки титульного листа');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (pageId) {
      loadTitlePage(pageId);
    }
  }, [pageId, loadTitlePage]);

  // Initialize history when titlePage is loaded
  useEffect(() => {
    if (titlePage) {
      historyRef.current = [JSON.parse(JSON.stringify(titlePage))]; // Deep copy
      historyIndexRef.current = 0;
    }
  }, [titlePage?.id]); // Only when page ID changes (new page loaded)

  const saveToHistory = useCallback((newState: TitlePage) => {
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    // Remove any history after current index (when we're not at the end)
    const newHistory = history.slice(0, index + 1);
    
    // Add new state
    newHistory.push(JSON.parse(JSON.stringify(newState))); // Deep copy
    
    // Limit history size
    if (newHistory.length > MAX_HISTORY) {
      newHistory.shift();
    } else {
      historyIndexRef.current = newHistory.length - 1;
    }
    
    historyRef.current = newHistory;
  }, []);

  const moveStartStateRef = useRef<TitlePage | null>(null);
  const isMovingRef = useRef(false);

  const handleElementMove = useCallback((elementId: string, x: number, y: number) => {
    if (!titlePage) return;

    // Save initial state when move starts
    if (!isMovingRef.current) {
      moveStartStateRef.current = JSON.parse(JSON.stringify(titlePage));
      isMovingRef.current = true;
    }

    const updatedElements = titlePage.elements.map((el) =>
      el.id === elementId ? { ...el, x, y } : el
    );

    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    // Don't save to history during move - will save on mouse up
  }, [titlePage]);

  const handleElementsMove = useCallback((elementIds: string[], deltaX: number, deltaY: number) => {
    if (!titlePage) return;

    // Save initial state when move starts
    if (!isMovingRef.current) {
      moveStartStateRef.current = JSON.parse(JSON.stringify(titlePage));
      isMovingRef.current = true;
    }

    // Use the initial state to calculate new positions, not current state
    // This prevents accumulation of deltas
    const baseState = moveStartStateRef.current || titlePage;
    const updatedElements = baseState.elements.map((el) => {
      if (elementIds.includes(el.id)) {
        // Find original position from the start state
        const originalEl = baseState.elements.find(e => e.id === el.id);
        if (originalEl) {
          return { ...el, x: originalEl.x + deltaX, y: originalEl.y + deltaY };
        }
      }
      return el;
    });

    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    // Don't save to history during move - will save on mouse up
  }, [titlePage]);

  const handleMoveEnd = useCallback(() => {
    if (isMovingRef.current && titlePage && moveStartStateRef.current) {
      // Check if state actually changed
      const currentStateStr = JSON.stringify(titlePage);
      const startStateStr = JSON.stringify(moveStartStateRef.current);
      
      if (currentStateStr !== startStateStr) {
        // State changed, save to history
        saveToHistory(titlePage);
      }
      
      isMovingRef.current = false;
      moveStartStateRef.current = null;
    }
  }, [titlePage, saveToHistory]);

  const handleElementDelete = useCallback(() => {
    if (!titlePage || selectedElementIds.length === 0) return;

    const updatedElements = titlePage.elements.filter((el) => !selectedElementIds.includes(el.id));
    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    setSelectedElementIds([]);
    saveToHistory(newState);
  }, [titlePage, selectedElementIds, saveToHistory]);

  async function handleSave() {
    if (!titlePage || !pageId) return;

    setSaving(true);
    try {
      const updated = await titlePageApi.update(pageId, {
        name: titlePage.name,
        elements: titlePage.elements,
        variables: titlePage.variables,
      });
      setTitlePage(updated);
    } catch (error) {
      console.error('Save failed:', error);
      alert('Ошибка сохранения');
    } finally {
      setSaving(false);
    }
  }

  async function handleGeneratePdf() {
    if (!pageId) return;

    setGenerating(true);
    try {
      const blob = await titlePageApi.generatePdf(pageId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${titlePage?.name || 'title-page'}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('PDF generation failed:', error);
      alert('Ошибка генерации PDF');
    } finally {
      setGenerating(false);
    }
  }

  function handleElementSelect(elementIds: string[]) {
    setSelectedElementIds(elementIds);
  }

  function handleElementToggle(elementId: string) {
    setSelectedElementIds((prev) => {
      if (prev.includes(elementId)) {
        return prev.filter((id) => id !== elementId);
      } else {
        return [...prev, elementId];
      }
    });
  }

  function handleElementAdd(type: TitlePageElementType, x: number, y: number) {
    if (!titlePage) return;

    const newElement: TitlePageElement = {
      id: uuidv4(),
      type,
      x,
      y,
      ...(type === 'text' && { content: 'Новый текст', fontSize: 14, fontFamily: 'Times New Roman', fontWeight: 'normal', fontStyle: 'normal', lineHeight: 1.2, textAlign: 'left' }),
      ...(type === 'variable' && { variableKey: '', fontSize: 14, fontFamily: 'Times New Roman', fontWeight: 'normal', fontStyle: 'normal', lineHeight: 1.2, textAlign: 'left' }),
      ...(type === 'line' && { length: 100, thickness: 1 }),
    };

    const updatedElements = [...titlePage.elements, newElement];
    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    setSelectedElementIds([newElement.id]);
    saveToHistory(newState);
  }

  function handleElementUpdate(updatedElement: TitlePageElement) {
    if (!titlePage) return;

    const updatedElements = titlePage.elements.map((el) =>
      el.id === updatedElement.id ? updatedElement : el
    );

    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    saveToHistory(newState);
  }

  function handleVariablesUpdate(variables: Record<string, string>) {
    if (!titlePage) return;
    setTitlePage({ ...titlePage, variables });
  }

  function handleNameChange(name: string) {
    if (!titlePage) return;
    setTitlePage({ ...titlePage, name });
  }

  function handleCopy() {
    if (!titlePage || selectedElementIds.length === 0) return;
    
    const elementsToCopy = titlePage.elements.filter(el => selectedElementIds.includes(el.id));
    clipboardRef.current = elementsToCopy.map(el => ({ ...el })); // Deep copy
  }

  function handleCut() {
    if (!titlePage || selectedElementIds.length === 0) return;
    
    handleCopy();
    const updatedElements = titlePage.elements.filter((el) => !selectedElementIds.includes(el.id));
    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    setSelectedElementIds([]);
    saveToHistory(newState);
  }

  function handlePaste() {
    if (!titlePage || clipboardRef.current.length === 0) return;
    
    // Create new elements with new IDs and offset position
    const offsetX = 10; // 10mm offset
    const offsetY = 10; // 10mm offset
    const newElements = clipboardRef.current.map(el => ({
      ...el,
      id: uuidv4(),
      x: el.x + offsetX,
      y: el.y + offsetY,
    }));
    
    const updatedElements = [...titlePage.elements, ...newElements];
    const newState = { ...titlePage, elements: updatedElements };
    setTitlePage(newState);
    setSelectedElementIds(newElements.map(el => el.id));
    saveToHistory(newState);
  }

  function handleUndo() {
    if (!titlePage) return;
    
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    if (index > 0) {
      historyIndexRef.current = index - 1;
      const previousState = history[historyIndexRef.current];
      setTitlePage(JSON.parse(JSON.stringify(previousState))); // Deep copy
    }
  }

  function handleRedo() {
    if (!titlePage) return;
    
    const history = historyRef.current;
    const index = historyIndexRef.current;
    
    if (index < history.length - 1) {
      historyIndexRef.current = index + 1;
      const nextState = history[historyIndexRef.current];
      setTitlePage(JSON.parse(JSON.stringify(nextState))); // Deep copy
    }
  }

  return {
    titlePage,
    loading,
    saving,
    generating,
    selectedElementIds,
    handleSave,
    handleGeneratePdf,
    handleElementSelect,
    handleElementToggle,
    handleElementMove,
    handleElementsMove,
    handleElementAdd,
    handleElementUpdate,
    handleElementDelete,
    handleVariablesUpdate,
    handleNameChange,
    handleCopy,
    handleCut,
    handlePaste,
    handleUndo,
    handleRedo,
    handleMoveEnd,
  };
}

