import { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import 'katex/dist/katex.min.css';
import type { Document, Profile, EntityStyle, EntityType } from '../../../../shared/src/types';
import { ENTITY_LABELS } from '../../../../shared/src/types';
import { documentApi, profileApi } from '../../infrastructure/api';
import { renderDocument } from '../../application/services/documentRenderer';
import { getBaseStyle, computeStyleDelta, isDeltaEmpty } from '../../../../shared/src/utils';
import { parseFrontmatter } from '../../utils/frontmatterUtils';
import { EntityStyleEditor, DocumentPreview } from '../components';

export function DocumentCustomizerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [document, setDocument] = useState<Document | null>(null);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null);
  const [selectedElementType, setSelectedElementType] = useState<EntityType | null>(null);

  useEffect(() => {
    if (id) {
      loadData(id);
    }
  }, [id]);

  async function loadData(docId: string) {
    try {
      const docData = await documentApi.getById(docId);
      setDocument(docData);

      if (docData.profileId) {
        const profileData = await profileApi.getById(docData.profileId);
        setProfile(profileData);
      }
    } catch (error) {
      console.error('Failed to load:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!document || !id) return;

    setSaving(true);
    try {
      await documentApi.update(id, {
        overrides: document.overrides,
      });
    } catch (error) {
      console.error('Save failed:', error);
    } finally {
      setSaving(false);
    }
  }

  function handleElementClick(e: React.MouseEvent) {
    const target = e.target as HTMLElement;
    const selectableElement = target.closest('.element-selectable') as HTMLElement;

    if (selectableElement) {
      const elementId = selectableElement.id;
      const elementType = selectableElement.getAttribute('data-type') as EntityType;

      setSelectedElementId(elementId);
      setSelectedElementType(elementType);

      // Update selection visual
      window.document.querySelectorAll('.element-selectable.selected').forEach((el) => {
        el.classList.remove('selected');
      });
      selectableElement.classList.add('selected');
    }
  }

  function handleStyleChange(style: EntityStyle) {
    if (!document || !selectedElementId || !selectedElementType) return;

    // Compute delta from base style
    const baseStyle = getBaseStyle(selectedElementType, profile);
    const delta = computeStyleDelta(style, baseStyle);

    const newOverrides = { ...document.overrides };

    if (isDeltaEmpty(delta)) {
      // Remove override if style matches base
      delete newOverrides[selectedElementId];
    } else {
      newOverrides[selectedElementId] = delta;
    }

    setDocument({ ...document, overrides: newOverrides });
  }

  function handleResetStyle() {
    if (!document || !selectedElementId) return;

    const newOverrides = { ...document.overrides };
    delete newOverrides[selectedElementId];
    setDocument({ ...document, overrides: newOverrides });
  }

  // Extract frontmatter and variables
  const { variables: documentVariables, content: markdownContent } = useMemo(() => {
    if (!document) return { variables: {}, content: '' };
    return parseFrontmatter(document.content);
  }, [document?.content]);

  const renderedHtml = useMemo(() => {
    if (!document) return '';

    return renderDocument({
      markdown: markdownContent,
      profile,
      overrides: document.overrides || {},
      selectable: true,
    });
  }, [markdownContent, document?.overrides, profile]);

  const currentStyle = useMemo(() => {
    if (!selectedElementId || !selectedElementType) return null;

    const baseStyle = getBaseStyle(selectedElementType, profile);
    const override = document?.overrides[selectedElementId] || {};
    return { ...baseStyle, ...override };
  }, [selectedElementId, selectedElementType, profile, document?.overrides]);

  if (loading) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Загрузка...</div>
      </div>
    );
  }

  if (!document) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Документ не найден</div>
      </div>
    );
  }

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Toolbar */}
      <div className="toolbar">
        <div className="toolbar-group">
          <button className="btn btn-ghost btn-sm" onClick={() => navigate(`/document/${id}/edit`)}>
            ← К редактору
          </button>
          <span style={{ color: 'var(--text-muted)' }}>|</span>
          <h3 style={{ margin: 0, fontSize: '1rem' }}>{document.name}</h3>
          <span className="text-muted" style={{ fontSize: '0.875rem' }}>
            — Кастомизация стилей
          </span>
        </div>

        <div className="toolbar-group">
          <button className="btn btn-secondary btn-sm" onClick={handleSave} disabled={saving}>
            {saving ? '💾 Сохранение...' : '💾 Сохранить'}
          </button>
        </div>
      </div>

      {/* Split view */}
      <div className="split-view">
        {/* Document Preview */}
        <div className="split-pane split-pane-left">
          <DocumentPreview 
            html={renderedHtml} 
            profile={profile} 
            onClick={handleElementClick}
            documentVariables={documentVariables}
          />
        </div>

        {/* Style Editor Panel */}
        <div className="split-pane" style={{ maxWidth: 400, overflow: 'auto' }}>
          <div style={{ padding: 'var(--spacing-md)' }}>
            {selectedElementId && selectedElementType && currentStyle ? (
              <>
                <div className="mb-md">
                  <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: 4 }}>
                    {ENTITY_LABELS[selectedElementType]}
                  </h3>
                  <p className="text-muted" style={{ fontSize: '0.75rem', fontFamily: 'var(--font-mono)' }}>
                    ID: {selectedElementId}
                  </p>
                  {document.overrides[selectedElementId] && (
                    <span
                      style={{
                        display: 'inline-block',
                        fontSize: '0.75rem',
                        padding: '2px 8px',
                        background: 'var(--accent-warning)',
                        color: 'var(--text-inverse)',
                        borderRadius: 'var(--radius-sm)',
                        marginTop: 4,
                      }}
                    >
                      Переопределён
                    </span>
                  )}
                </div>

                <EntityStyleEditor
                  entityType={selectedElementType}
                  style={currentStyle}
                  onChange={handleStyleChange}
                  showReset={!!document.overrides[selectedElementId]}
                  onReset={handleResetStyle}
                />
              </>
            ) : (
              <div className="text-center" style={{ padding: '3rem 1rem' }}>
                <p className="text-muted">
                  Кликните на элемент документа для редактирования его стилей
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

