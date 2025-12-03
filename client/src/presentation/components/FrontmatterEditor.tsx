import { useState, useEffect, useMemo } from 'react';
import type { TitlePage } from '../../../../shared/src/types';
import { VariableEditor } from './VariableEditor';
import { parseFrontmatter, generateFrontmatter, getTitlePageVariables, validateFrontmatter } from '../../utils/frontmatterUtils';
import { titlePageApi } from '../../infrastructure/api/titlePageApi';
import { getDefaultTitlePageId } from '../../utils/storageUtils';

interface FrontmatterEditorProps {
  markdown: string;
  onUpdate: (markdown: string) => void;
}

export function FrontmatterEditor({ markdown, onUpdate }: FrontmatterEditorProps) {
  const [titlePage, setTitlePage] = useState<TitlePage | null>(null);
  const [loading, setLoading] = useState(true);

  // Extract variables from markdown
  const { variables, content } = useMemo(() => {
    return parseFrontmatter(markdown);
  }, [markdown]);

  // Sync VariableEditor when variables change externally
  const [localVariables, setLocalVariables] = useState(variables);
  
  useEffect(() => {
    setLocalVariables(variables);
  }, [variables]);

  // Get variables used in title page
  const titlePageVariableKeys = useMemo(() => {
    if (!titlePage) return [];
    return getTitlePageVariables(titlePage);
  }, [titlePage]);

  // Load title page
  useEffect(() => {
    async function loadTitlePage() {
      setLoading(true);
      try {
        const titlePageId = getDefaultTitlePageId();
        if (titlePageId) {
          const page = await titlePageApi.getById(titlePageId);
          setTitlePage(page);
        } else {
          setTitlePage(null);
        }
      } catch (error) {
        console.error('Failed to load title page:', error);
        setTitlePage(null);
      } finally {
        setLoading(false);
      }
    }
    
    loadTitlePage();
  }, []);

  // Handle variables update in graphical mode
  function handleVariablesUpdate(newVariables: Record<string, string>) {
    const newMarkdown = generateFrontmatter(newVariables, markdown);
    onUpdate(newMarkdown);
  }

  // Insert all variables from title page template
  function handleInsertAllFromTemplate() {
    if (!titlePage || titlePageVariableKeys.length === 0) return;
    
    const newVariables: Record<string, string> = { ...localVariables };
    
    // Add all variables from title page with empty values
    titlePageVariableKeys.forEach(key => {
      if (!newVariables[key]) {
        newVariables[key] = '';
      }
    });
    
    handleVariablesUpdate(newVariables);
  }

  // Check which variables are used/unused
  const variableStatus = useMemo(() => {
    const status: Record<string, { used: boolean; defined: boolean }> = {};
    
    // Mark all title page variables
    titlePageVariableKeys.forEach(key => {
      status[key] = { used: false, defined: false };
    });
    
    // Mark defined variables
    Object.keys(variables).forEach(key => {
      if (!status[key]) {
        status[key] = { used: false, defined: true };
      } else {
        status[key].defined = true;
      }
    });
    
    // Mark used variables
    titlePageVariableKeys.forEach(key => {
      if (variables[key] !== undefined) {
        status[key].used = true;
      }
    });
    
    return status;
  }, [variables, titlePageVariableKeys]);

  return (
    <div style={{ 
      padding: '1.5rem', 
      border: '1px solid #ddd', 
      borderRadius: '4px',
      background: '#f8f9fa',
      width: '100%',
      maxWidth: '800px',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 600 }}>Переменные Frontmatter</h3>
        {titlePageVariableKeys.length > 0 && (
          <button
            onClick={handleInsertAllFromTemplate}
            style={{
              padding: '0.5rem 1rem',
              background: '#28a745',
              color: '#fff',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '0.875rem',
              fontWeight: 500,
            }}
            title="Вставить все переменные из титульного листа"
          >
            + Вставить из шаблона
          </button>
        )}
      </div>

        {loading ? (
          <div style={{ color: '#666', fontSize: '0.875rem' }}>Загрузка...</div>
        ) : (
          <>
            {titlePageVariableKeys.length > 0 && (
              <div style={{ 
                marginBottom: '1rem', 
                padding: '0.5rem', 
                background: '#e7f3ff', 
                borderRadius: '4px',
                fontSize: '0.875rem',
              }}>
                <strong>Переменные из титульного листа:</strong>
                <ul style={{ margin: '0.5rem 0 0 0', paddingLeft: '1.5rem' }}>
                  {titlePageVariableKeys.map(key => {
                    const status = variableStatus[key];
                    const isDefined = status?.defined || false;
                    return (
                      <li key={key} style={{ color: isDefined ? '#28a745' : '#dc3545' }}>
                        {key} {isDefined ? '✓' : '(не определена)'}
                      </li>
                    );
                  })}
                </ul>
              </div>
            )}

            <VariableEditor variables={localVariables} onUpdate={handleVariablesUpdate} />

            {Object.keys(variables).length > 0 && (
              <div style={{ marginTop: '1rem', fontSize: '0.875rem', color: '#666' }}>
                {Object.entries(variableStatus).map(([key, status]) => {
                  if (!status.defined) return null;
                  if (status.used) return null;
                  return (
                    <div key={key} style={{ color: '#ffc107', marginTop: '0.25rem' }}>
                      ⚠ {key} определена, но не используется в титульном листе
                    </div>
                  );
                })}
              </div>
            )}
          </>
        )}
    </div>
  );
}

