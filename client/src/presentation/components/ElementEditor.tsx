import { useState, useEffect } from 'react';
import type { TitlePageElement } from '../../../../shared/src/types';
import { FONT_FAMILIES } from '../../../../shared/src/types';

interface ElementEditorProps {
  element: TitlePageElement | null;
  onUpdate: (element: TitlePageElement) => void;
  onDelete: () => void;
}

export function ElementEditor({ element, onUpdate, onDelete }: ElementEditorProps) {
  const [localElement, setLocalElement] = useState<TitlePageElement | null>(element);

  useEffect(() => {
    setLocalElement(element);
  }, [element]);

  if (!localElement) {
    return (
      <div style={{ padding: '1rem', border: '1px solid #ddd', borderRadius: '4px' }}>
        <p style={{ color: '#666' }}>Выберите элемент для редактирования</p>
      </div>
    );
  }

  const handleChange = (updates: Partial<TitlePageElement>) => {
    const updated = { ...localElement, ...updates };
    setLocalElement(updated);
    onUpdate(updated);
  };

  return (
    <div style={{ padding: '1rem', border: '1px solid #ddd', borderRadius: '4px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h3 style={{ margin: 0, fontSize: '1rem', fontWeight: 600 }}>
          {localElement.type === 'text' && 'Текст'}
          {localElement.type === 'variable' && 'Переменная'}
          {localElement.type === 'line' && 'Линия'}
        </h3>
        <button
          onClick={onDelete}
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

      {/* Position */}
      <div style={{ marginBottom: '1rem' }}>
        <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Позиция (мм)</label>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <input
            type="number"
            value={localElement.x}
            onChange={(e) => handleChange({ x: parseFloat(e.target.value) || 0 })}
            style={{ width: '100px', padding: '0.25rem' }}
            placeholder="X"
          />
          <input
            type="number"
            value={localElement.y}
            onChange={(e) => handleChange({ y: parseFloat(e.target.value) || 0 })}
            style={{ width: '100px', padding: '0.25rem' }}
            placeholder="Y"
          />
        </div>
      </div>

      {/* Text/Variable specific */}
      {(localElement.type === 'text' || localElement.type === 'variable') && (
        <>
          {localElement.type === 'text' && (
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Текст</label>
              <textarea
                value={localElement.content || ''}
                onChange={(e) => handleChange({ content: e.target.value })}
                style={{ width: '100%', padding: '0.25rem', minHeight: '60px' }}
                placeholder="Введите текст"
              />
            </div>
          )}

          {localElement.type === 'variable' && (
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Ключ переменной</label>
              <input
                type="text"
                value={localElement.variableKey || ''}
                onChange={(e) => handleChange({ variableKey: e.target.value })}
                style={{ width: '100%', padding: '0.25rem' }}
                placeholder="например: university"
              />
            </div>
          )}

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Размер шрифта (pt)</label>
            <input
              type="number"
              value={localElement.fontSize || 14}
              onChange={(e) => handleChange({ fontSize: parseFloat(e.target.value) || 14 })}
              style={{ width: '100%', padding: '0.25rem' }}
              min="8"
              max="72"
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Шрифт</label>
            <select
              value={localElement.fontFamily || ''}
              onChange={(e) => handleChange({ fontFamily: e.target.value })}
              style={{ width: '100%', padding: '0.25rem' }}
            >
              {FONT_FAMILIES.map((font) => (
                <option key={font.value} value={font.value}>
                  {font.label}
                </option>
              ))}
            </select>
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Начертание</label>
            <select
              value={localElement.fontWeight || 'normal'}
              onChange={(e) => handleChange({ fontWeight: e.target.value as 'normal' | 'bold' })}
              style={{ width: '100%', padding: '0.25rem' }}
            >
              <option value="normal">Обычный</option>
              <option value="bold">Жирный</option>
            </select>
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Выравнивание</label>
            <select
              value={localElement.textAlign || 'left'}
              onChange={(e) => handleChange({ textAlign: e.target.value as 'left' | 'center' | 'right' })}
              style={{ width: '100%', padding: '0.25rem' }}
            >
              <option value="left">Слева</option>
              <option value="center">По центру</option>
              <option value="right">Справа</option>
            </select>
          </div>
        </>
      )}

      {/* Line specific */}
      {localElement.type === 'line' && (
        <>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Длина (мм)</label>
            <input
              type="number"
              value={localElement.length || 100}
              onChange={(e) => handleChange({ length: parseFloat(e.target.value) || 100 })}
              style={{ width: '100%', padding: '0.25rem' }}
              min="1"
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Толщина (мм)</label>
            <input
              type="number"
              value={localElement.thickness || 1}
              onChange={(e) => handleChange({ thickness: parseFloat(e.target.value) || 1 })}
              style={{ width: '100%', padding: '0.25rem' }}
              min="0.1"
              step="0.1"
            />
          </div>
        </>
      )}
    </div>
  );
}

