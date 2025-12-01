import { useState } from 'react';

interface VariableEditorProps {
  variables: Record<string, string>;
  onUpdate: (variables: Record<string, string>) => void;
}

export function VariableEditor({ variables, onUpdate }: VariableEditorProps) {
  const [localVars, setLocalVars] = useState<Array<{ key: string; value: string }>>(() => {
    return Object.entries(variables).map(([key, value]) => ({ key, value }));
  });

  const handleAdd = () => {
    const newVars = [...localVars, { key: '', value: '' }];
    setLocalVars(newVars);
    updateVariables(newVars);
  };

  const handleUpdate = (index: number, updates: Partial<{ key: string; value: string }>) => {
    const newVars = [...localVars];
    newVars[index] = { ...newVars[index], ...updates };
    setLocalVars(newVars);
    updateVariables(newVars);
  };

  const handleDelete = (index: number) => {
    const newVars = localVars.filter((_, i) => i !== index);
    setLocalVars(newVars);
    updateVariables(newVars);
  };

  const updateVariables = (vars: Array<{ key: string; value: string }>) => {
    const result: Record<string, string> = {};
    vars.forEach(({ key, value }) => {
      if (key.trim()) {
        result[key.trim()] = value;
      }
    });
    onUpdate(result);
  };

  return (
    <div style={{ padding: '1rem', border: '1px solid #ddd', borderRadius: '4px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h3 style={{ margin: 0, fontSize: '1rem', fontWeight: 600 }}>Переменные</h3>
        <button
          onClick={handleAdd}
          style={{
            padding: '0.25rem 0.5rem',
            background: '#28a745',
            color: '#fff',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
        >
          + Добавить
        </button>
      </div>

      {localVars.length === 0 ? (
        <p style={{ color: '#666', fontSize: '0.875rem' }}>Нет переменных. Добавьте переменную для использования в элементах.</p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {localVars.map((variable, index) => (
            <div key={index} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
              <input
                type="text"
                value={variable.key}
                onChange={(e) => handleUpdate(index, { key: e.target.value })}
                placeholder="Ключ"
                style={{ flex: 1, padding: '0.25rem' }}
              />
              <input
                type="text"
                value={variable.value}
                onChange={(e) => handleUpdate(index, { value: e.target.value })}
                placeholder="Значение"
                style={{ flex: 2, padding: '0.25rem' }}
              />
              <button
                onClick={() => handleDelete(index)}
                style={{
                  padding: '0.25rem 0.5rem',
                  background: '#dc3545',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '0.875rem',
                }}
              >
                Удалить
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

