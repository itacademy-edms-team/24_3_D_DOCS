import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { v4 as uuidv4 } from 'uuid';
import type { TitlePage, TitlePageElement, TitlePageElementType } from '../../../../shared/src/types';
import { titlePageApi } from '../../infrastructure/api/titlePageApi';
import { TitlePageCanvas } from '../components/TitlePageCanvas';
import { ElementEditor } from '../components/ElementEditor';
import { VariableEditor } from '../components/VariableEditor';

export function TitlePageEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [titlePage, setTitlePage] = useState<TitlePage | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadTitlePage(id);
    }
  }, [id]);

  async function loadTitlePage(pageId: string) {
    try {
      const data = await titlePageApi.getById(pageId);
      setTitlePage(data);
    } catch (error) {
      console.error('Failed to load title page:', error);
      alert('Ошибка загрузки титульного листа');
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!titlePage || !id) return;

    setSaving(true);
    try {
      const updated = await titlePageApi.update(id, {
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
    if (!id) return;

    setGenerating(true);
    try {
      const blob = await titlePageApi.generatePdf(id);
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

  function handleElementSelect(elementId: string | null) {
    setSelectedElementId(elementId);
  }

  function handleElementMove(elementId: string, x: number, y: number) {
    if (!titlePage) return;

    const updatedElements = titlePage.elements.map((el) =>
      el.id === elementId ? { ...el, x, y } : el
    );

    setTitlePage({ ...titlePage, elements: updatedElements });
  }

  function handleElementAdd(type: TitlePageElementType, x: number, y: number) {
    if (!titlePage) return;

    const newElement: TitlePageElement = {
      id: uuidv4(),
      type,
      x,
      y,
      ...(type === 'text' && { content: 'Новый текст', fontSize: 14, fontFamily: 'Times New Roman', fontWeight: 'normal', textAlign: 'left' }),
      ...(type === 'variable' && { variableKey: '', fontSize: 14, fontFamily: 'Times New Roman', fontWeight: 'normal', textAlign: 'left' }),
      ...(type === 'line' && { length: 100, thickness: 1 }),
    };

    const updatedElements = [...titlePage.elements, newElement];
    setTitlePage({ ...titlePage, elements: updatedElements });
    setSelectedElementId(newElement.id);
  }

  function handleElementUpdate(updatedElement: TitlePageElement) {
    if (!titlePage) return;

    const updatedElements = titlePage.elements.map((el) =>
      el.id === updatedElement.id ? updatedElement : el
    );

    setTitlePage({ ...titlePage, elements: updatedElements });
  }

  function handleElementDelete() {
    if (!titlePage || !selectedElementId) return;

    const updatedElements = titlePage.elements.filter((el) => el.id !== selectedElementId);
    setTitlePage({ ...titlePage, elements: updatedElements });
    setSelectedElementId(null);
  }

  function handleVariablesUpdate(variables: Record<string, string>) {
    if (!titlePage) return;
    setTitlePage({ ...titlePage, variables });
  }

  function handleNameChange(name: string) {
    if (!titlePage) return;
    setTitlePage({ ...titlePage, name });
  }

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

  const selectedElement = titlePage.elements.find((el) => el.id === selectedElementId) || null;

  return (
    <div className="page">
      <div className="container">
        {/* Header */}
        <div className="flex justify-between items-center mb-lg">
          <div>
            <input
              type="text"
              value={titlePage.name}
              onChange={(e) => handleNameChange(e.target.value)}
              style={{
                fontSize: '1.75rem',
                fontWeight: 700,
                border: 'none',
                background: 'transparent',
                padding: '0.25rem',
                width: '100%',
                maxWidth: '500px',
              }}
            />
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              className="btn btn-primary"
              onClick={handleSave}
              disabled={saving}
            >
              {saving ? 'Сохранение...' : 'Сохранить'}
            </button>
            <button
              className="btn btn-primary"
              onClick={handleGeneratePdf}
              disabled={generating}
            >
              {generating ? 'Генерация...' : 'Скачать PDF'}
            </button>
            <button
              className="btn btn-ghost"
              onClick={() => navigate('/')}
            >
              Назад
            </button>
          </div>
        </div>

        {/* Main content */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '1.5rem' }}>
          {/* Canvas area */}
          <div>
            <TitlePageCanvas
              elements={titlePage.elements}
              selectedElementId={selectedElementId}
              onElementSelect={handleElementSelect}
              onElementMove={handleElementMove}
              onElementAdd={handleElementAdd}
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

