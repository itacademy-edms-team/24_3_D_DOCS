import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import type { TitlePageElementType } from '../../../../shared/src/types';
import { TitlePageCanvas } from '../components/TitlePageCanvas';
import { ElementEditor } from '../components/ElementEditor';
import { VariableEditor } from '../components/VariableEditor';
import { TitlePageEditorHeader } from '../components/TitlePageEditorHeader';
import { useTitlePageEditor } from '../hooks/useTitlePageEditor';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcuts';

export function TitlePageEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const {
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
  } = useTitlePageEditor(id);

  const [currentTool, setCurrentTool] = useState<TitlePageElementType | null>(null);

  useKeyboardShortcuts({
    selectedElementIds,
    currentTool,
    onToolChange: setCurrentTool,
    onElementDelete: handleElementDelete,
    onElementDeselect: () => handleElementSelect([]),
    onElementMove: (deltaX, deltaY) => {
      if (selectedElementIds.length > 0 && titlePage) {
        handleElementsMove(selectedElementIds, deltaX, deltaY);
      }
    },
    onCopy: handleCopy,
    onCut: handleCut,
    onPaste: handlePaste,
    onUndo: handleUndo,
    onRedo: handleRedo,
    titlePage,
  });

  if (loading) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Загрузка...</div>
      </div>
    );
  }

  if (!titlePage) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Титульный лист не найден</div>
      </div>
    );
  }

  const selectedElement = selectedElementIds.length === 1 
    ? titlePage.elements.find((el) => el.id === selectedElementIds[0]) || null
    : null;

  return (
    <div className="page">
      <div className="container">
        {/* Header */}
        <TitlePageEditorHeader
          name={titlePage.name}
          onNameChange={handleNameChange}
          onSave={handleSave}
          onGeneratePdf={handleGeneratePdf}
          onNavigateBack={() => navigate('/')}
          saving={saving}
          generating={generating}
        />

        {/* Main content */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '1.5rem' }}>
          {/* Canvas area */}
          <div>
            <TitlePageCanvas
              elements={titlePage.elements}
              selectedElementIds={selectedElementIds}
              onElementSelect={handleElementSelect}
              onElementToggle={handleElementToggle}
              onElementMove={handleElementMove}
              onElementsMove={handleElementsMove}
              onElementAdd={handleElementAdd}
              onMoveEnd={handleMoveEnd}
              initialTool={currentTool}
              onToolChange={setCurrentTool}
            />
          </div>

          {/* Sidebar */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <ElementEditor
              element={selectedElement}
              onUpdate={handleElementUpdate}
              onDelete={handleElementDelete}
            />
            <VariableEditor
              variables={titlePage.variables}
              onUpdate={handleVariablesUpdate}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
