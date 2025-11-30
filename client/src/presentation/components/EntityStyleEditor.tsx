import { useState, useEffect } from 'react';
import type { EntityStyle, EntityType } from '../../../../shared/src/types';
import { ENTITY_LABELS, FONT_FAMILIES, DEFAULT_ENTITY_STYLES } from '../../../../shared/src/types';

interface EntityStyleEditorProps {
  entityType: EntityType;
  style: EntityStyle;
  onChange: (style: EntityStyle) => void;
  title?: string;
  showReset?: boolean;
  onReset?: () => void;
}

/**
 * Editor component for entity styles
 * Allows editing all style properties for any entity type
 */
export function EntityStyleEditor({
  entityType,
  style,
  onChange,
  title,
  showReset = false,
  onReset,
}: EntityStyleEditorProps) {
  const [localStyle, setLocalStyle] = useState<EntityStyle>(style);

  useEffect(() => {
    setLocalStyle(style);
  }, [style]);

  const updateStyle = (key: keyof EntityStyle, value: unknown) => {
    const newStyle = { ...localStyle, [key]: value };
    setLocalStyle(newStyle);
    onChange(newStyle);
  };

  const updateNumericStyle = (key: keyof EntityStyle, value: string, allowFloat = false) => {
    if (value === '') {
      const newStyle = { ...localStyle };
      delete newStyle[key];
      setLocalStyle(newStyle);
      onChange(newStyle);
      return;
    }
    const num = allowFloat ? parseFloat(value) : parseInt(value, 10);
    if (!isNaN(num)) {
      updateStyle(key, num);
    }
  };

  const getDefaultValue = (key: keyof EntityStyle) => {
    return DEFAULT_ENTITY_STYLES[entityType]?.[key];
  };

  const displayTitle = title || ENTITY_LABELS[entityType];

  // Determine which sections to show based on entity type
  const showTypography = ['paragraph', 'heading', 'image-caption', 'table-caption', 'formula-caption', 'ordered-list', 'unordered-list', 'table'].includes(entityType);
  const showTextFormat = ['paragraph', 'heading', 'image-caption', 'table-caption', 'formula-caption', 'image', 'formula'].includes(entityType);
  const showIndent = ['paragraph', 'ordered-list', 'unordered-list'].includes(entityType);
  const showBorder = ['table'].includes(entityType);
  const showMaxWidth = ['image'].includes(entityType);

  return (
    <div className="style-editor">
      <div className="style-editor-header">
        <span className="style-editor-title">{displayTitle}</span>
        {showReset && onReset && (
          <button className="btn btn-ghost btn-sm" onClick={onReset}>
            Сбросить
          </button>
        )}
      </div>

      <div className="style-editor-content">
        {/* Typography Section */}
        {showTypography && (
          <div className="style-section">
            <div className="style-section-title">Типографика</div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Размер (pt)</label>
                <input
                  type="number"
                  className="form-input form-input-sm"
                  value={localStyle.fontSize ?? ''}
                  placeholder={String(getDefaultValue('fontSize') ?? '')}
                  onChange={(e) => updateNumericStyle('fontSize', e.target.value)}
                  min={6}
                  max={72}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Шрифт</label>
                <select
                  className="form-select form-input-sm"
                  value={localStyle.fontFamily ?? ''}
                  onChange={(e) => updateStyle('fontFamily', e.target.value || undefined)}
                >
                  {FONT_FAMILIES.map((font) => (
                    <option key={font.value} value={font.value}>
                      {font.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Жирность</label>
                <select
                  className="form-select form-input-sm"
                  value={localStyle.fontWeight ?? ''}
                  onChange={(e) => updateStyle('fontWeight', e.target.value || undefined)}
                >
                  <option value="">По умолч.</option>
                  <option value="normal">Обычный</option>
                  <option value="bold">Жирный</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Начертание</label>
                <select
                  className="form-select form-input-sm"
                  value={localStyle.fontStyle ?? ''}
                  onChange={(e) => updateStyle('fontStyle', e.target.value || undefined)}
                >
                  <option value="">По умолч.</option>
                  <option value="normal">Обычный</option>
                  <option value="italic">Курсив</option>
                </select>
              </div>
            </div>
          </div>
        )}

        {/* Text Formatting Section */}
        {showTextFormat && (
          <div className="style-section">
            <div className="style-section-title">Форматирование</div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Выравнивание</label>
                <select
                  className="form-select form-input-sm"
                  value={localStyle.textAlign ?? ''}
                  onChange={(e) => updateStyle('textAlign', e.target.value || undefined)}
                >
                  <option value="">По умолч.</option>
                  <option value="left">По левому</option>
                  <option value="center">По центру</option>
                  <option value="right">По правому</option>
                  <option value="justify">По ширине</option>
                </select>
              </div>

              {showIndent && (
                <div className="form-group">
                  <label className="form-label">Красная строка (см)</label>
                  <input
                    type="number"
                    className="form-input form-input-sm"
                    value={localStyle.textIndent ?? ''}
                    placeholder={String(getDefaultValue('textIndent') ?? '')}
                    onChange={(e) => updateNumericStyle('textIndent', e.target.value, true)}
                    step={0.25}
                    min={0}
                    max={5}
                  />
                </div>
              )}

              <div className="form-group">
                <label className="form-label">Межстрочный интервал</label>
                <input
                  type="number"
                  className="form-input form-input-sm"
                  value={localStyle.lineHeight ?? ''}
                  placeholder={String(getDefaultValue('lineHeight') ?? '')}
                  onChange={(e) => updateNumericStyle('lineHeight', e.target.value, true)}
                  step={0.1}
                  min={0.5}
                  max={3}
                />
              </div>
            </div>
          </div>
        )}

        {/* Margins Section */}
        <div className="style-section">
          <div className="style-section-title">Внешние отступы (pt)</div>
          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Сверху</label>
              <input
                type="number"
                className="form-input form-input-sm"
                value={localStyle.marginTop ?? ''}
                placeholder={String(getDefaultValue('marginTop') ?? '')}
                onChange={(e) => updateNumericStyle('marginTop', e.target.value)}
                min={0}
                max={200}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Снизу</label>
              <input
                type="number"
                className="form-input form-input-sm"
                value={localStyle.marginBottom ?? ''}
                placeholder={String(getDefaultValue('marginBottom') ?? '')}
                onChange={(e) => updateNumericStyle('marginBottom', e.target.value)}
                min={0}
                max={200}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Слева</label>
              <input
                type="number"
                className="form-input form-input-sm"
                value={localStyle.marginLeft ?? ''}
                placeholder={String(getDefaultValue('marginLeft') ?? '')}
                onChange={(e) => updateNumericStyle('marginLeft', e.target.value)}
                min={0}
                max={200}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Справа</label>
              <input
                type="number"
                className="form-input form-input-sm"
                value={localStyle.marginRight ?? ''}
                placeholder={String(getDefaultValue('marginRight') ?? '')}
                onChange={(e) => updateNumericStyle('marginRight', e.target.value)}
                min={0}
                max={200}
              />
            </div>
          </div>
        </div>

        {/* Border Section (for tables) */}
        {showBorder && (
          <div className="style-section">
            <div className="style-section-title">Рамка</div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Толщина (px)</label>
                <input
                  type="number"
                  className="form-input form-input-sm"
                  value={localStyle.borderWidth ?? ''}
                  placeholder={String(getDefaultValue('borderWidth') ?? '')}
                  onChange={(e) => updateNumericStyle('borderWidth', e.target.value)}
                  min={0}
                  max={10}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Стиль</label>
                <select
                  className="form-select form-input-sm"
                  value={localStyle.borderStyle ?? ''}
                  onChange={(e) => updateStyle('borderStyle', e.target.value || undefined)}
                >
                  <option value="">По умолч.</option>
                  <option value="none">Нет</option>
                  <option value="solid">Сплошная</option>
                  <option value="dashed">Пунктир</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Цвет</label>
                <input
                  type="color"
                  className="form-input form-input-sm"
                  value={localStyle.borderColor ?? '#333333'}
                  onChange={(e) => updateStyle('borderColor', e.target.value)}
                  style={{ padding: 2, height: 32 }}
                />
              </div>
            </div>
          </div>
        )}

        {/* Size Section (for images) */}
        {showMaxWidth && (
          <div className="style-section">
            <div className="style-section-title">Размер</div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Макс. ширина (%)</label>
                <input
                  type="number"
                  className="form-input form-input-sm"
                  value={localStyle.maxWidth ?? ''}
                  placeholder={String(getDefaultValue('maxWidth') ?? '')}
                  onChange={(e) => updateNumericStyle('maxWidth', e.target.value)}
                  min={10}
                  max={100}
                />
              </div>
            </div>
          </div>
        )}

        {/* Colors Section */}
        <div className="style-section">
          <div className="style-section-title">Цвета</div>
          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Цвет текста</label>
              <input
                type="color"
                className="form-input form-input-sm"
                value={localStyle.color ?? '#000000'}
                onChange={(e) => updateStyle('color', e.target.value)}
                style={{ padding: 2, height: 32 }}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Фон</label>
              <input
                type="color"
                className="form-input form-input-sm"
                value={localStyle.backgroundColor ?? '#ffffff'}
                onChange={(e) => updateStyle('backgroundColor', e.target.value)}
                style={{ padding: 2, height: 32 }}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

