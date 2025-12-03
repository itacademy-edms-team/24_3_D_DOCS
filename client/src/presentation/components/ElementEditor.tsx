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
  // String values for numeric inputs to allow clearing
  const [stringValues, setStringValues] = useState<Record<string, string>>({});

  useEffect(() => {
    setLocalElement(element);
    // Reset string values when element changes
    if (element) {
      setStringValues({
        x: element.x != null ? element.x.toFixed(1) : '',
        y: element.y != null ? element.y.toFixed(1) : '',
        fontSize: element.fontSize?.toString() ?? '',
        lineHeight: element.lineHeight?.toString() ?? '',
        length: element.length?.toString() ?? '',
        thickness: element.thickness?.toString() ?? '',
      });
    }
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

  const handleNumericChange = (field: string, value: string, defaultValue: number) => {
    setStringValues({ ...stringValues, [field]: value });
    // Only update if value is valid number
    const numValue = parseFloat(value);
    if (value === '' || !isNaN(numValue)) {
      handleChange({ [field]: value === '' ? undefined : numValue } as Partial<TitlePageElement>);
    }
  };

  const handleNumericBlur = (field: string, defaultValue: number) => {
    const stringValue = stringValues[field] ?? '';
    if (stringValue === '') {
      // Apply default value on blur if empty
      const numValue = defaultValue;
      setStringValues({ ...stringValues, [field]: numValue.toString() });
      handleChange({ [field]: numValue } as Partial<TitlePageElement>);
    } else {
      const numValue = parseFloat(stringValue);
      if (isNaN(numValue)) {
        // Invalid value, restore default
        setStringValues({ ...stringValues, [field]: defaultValue.toString() });
        handleChange({ [field]: defaultValue } as Partial<TitlePageElement>);
      } else {
        // Ensure string value matches parsed value
        setStringValues({ ...stringValues, [field]: numValue.toString() });
      }
    }
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
            value={stringValues.x ?? (localElement.x != null ? localElement.x.toFixed(1) : '')}
            onChange={(e) => handleNumericChange('x', e.target.value, 0)}
            onBlur={() => {
              const numValue = parseFloat(stringValues.x ?? '');
              if (!isNaN(numValue)) {
                // Round to 1 decimal place on blur
                const rounded = parseFloat(numValue.toFixed(1));
                setStringValues({ ...stringValues, x: rounded.toString() });
                handleChange({ x: rounded });
              } else {
                handleNumericBlur('x', 0);
              }
            }}
            style={{ width: '100px', padding: '0.25rem' }}
            placeholder="X"
            step="0.1"
          />
          <input
            type="number"
            value={stringValues.y ?? (localElement.y != null ? localElement.y.toFixed(1) : '')}
            onChange={(e) => handleNumericChange('y', e.target.value, 0)}
            onBlur={() => {
              const numValue = parseFloat(stringValues.y ?? '');
              if (!isNaN(numValue)) {
                // Round to 1 decimal place on blur
                const rounded = parseFloat(numValue.toFixed(1));
                setStringValues({ ...stringValues, y: rounded.toString() });
                handleChange({ y: rounded });
              } else {
                handleNumericBlur('y', 0);
              }
            }}
            style={{ width: '100px', padding: '0.25rem' }}
            placeholder="Y"
            step="0.1"
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
              value={stringValues.fontSize ?? localElement.fontSize?.toString() ?? ''}
              onChange={(e) => handleNumericChange('fontSize', e.target.value, 14)}
              onBlur={() => handleNumericBlur('fontSize', 14)}
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
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Стиль</label>
            <select
              value={localElement.fontStyle || 'normal'}
              onChange={(e) => handleChange({ fontStyle: e.target.value as 'normal' | 'italic' })}
              style={{ width: '100%', padding: '0.25rem' }}
            >
              <option value="normal">Обычный</option>
              <option value="italic">Курсив</option>
            </select>
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Междустрочный интервал</label>
            <input
              type="number"
              value={stringValues.lineHeight ?? localElement.lineHeight?.toString() ?? ''}
              onChange={(e) => handleNumericChange('lineHeight', e.target.value, 1.2)}
              onBlur={() => handleNumericBlur('lineHeight', 1.2)}
              style={{ width: '100%', padding: '0.25rem' }}
              min="0.5"
              max="3"
              step="0.1"
            />
            <small style={{ color: '#666', fontSize: '0.75rem' }}>Множитель (например, 1.2 = 120%)</small>
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
              value={stringValues.length ?? localElement.length?.toString() ?? ''}
              onChange={(e) => handleNumericChange('length', e.target.value, 100)}
              onBlur={() => handleNumericBlur('length', 100)}
              style={{ width: '100%', padding: '0.25rem' }}
              min="1"
            />
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem' }}>Толщина (мм)</label>
            <input
              type="number"
              value={stringValues.thickness ?? localElement.thickness?.toString() ?? ''}
              onChange={(e) => handleNumericChange('thickness', e.target.value, 1)}
              onBlur={() => handleNumericBlur('thickness', 1)}
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

