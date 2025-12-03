import type { PageSettings } from '../../../../shared/src/types';
import { DEFAULT_PAGE_SETTINGS } from '../../../../shared/src/types';

interface PageSettingsEditorProps {
  pageSettings: PageSettings;
  onPageSettingChange: <K extends keyof PageSettings>(key: K, value: PageSettings[K]) => void;
  onMarginChange: (side: 'top' | 'right' | 'bottom' | 'left', value: number) => void;
}

export function PageSettingsEditor({
  pageSettings,
  onPageSettingChange,
  onMarginChange,
}: PageSettingsEditorProps) {
  return (
    <div className="card mb-lg">
      <h3 className="card-title mb-md">Настройки страницы</h3>

      <div className="form-group">
        <label className="form-label">Размер</label>
        <select
          className="form-select"
          value={pageSettings.size}
          onChange={(e) => onPageSettingChange('size', e.target.value as 'A4' | 'A5' | 'Letter')}
        >
          <option value="A4">A4</option>
          <option value="A5">A5</option>
          <option value="Letter">Letter</option>
        </select>
      </div>

      <div className="form-group">
        <label className="form-label">Ориентация</label>
        <select
          className="form-select"
          value={pageSettings.orientation}
          onChange={(e) => onPageSettingChange('orientation', e.target.value as 'portrait' | 'landscape')}
        >
          <option value="portrait">Книжная</option>
          <option value="landscape">Альбомная</option>
        </select>
      </div>

      <div className="form-group">
        <label className="form-label">Поля (мм)</label>
        <div className="form-row">
          <div>
            <label className="form-label" style={{ fontSize: '0.75rem' }}>Сверху</label>
            <input
              type="number"
              className="form-input form-input-sm"
              value={pageSettings.margins.top}
              onChange={(e) => onMarginChange('top', parseInt(e.target.value) || 0)}
              min={0}
              max={100}
            />
          </div>
          <div>
            <label className="form-label" style={{ fontSize: '0.75rem' }}>Снизу</label>
            <input
              type="number"
              className="form-input form-input-sm"
              value={pageSettings.margins.bottom}
              onChange={(e) => onMarginChange('bottom', parseInt(e.target.value) || 0)}
              min={0}
              max={100}
            />
          </div>
          <div>
            <label className="form-label" style={{ fontSize: '0.75rem' }}>Слева</label>
            <input
              type="number"
              className="form-input form-input-sm"
              value={pageSettings.margins.left}
              onChange={(e) => onMarginChange('left', parseInt(e.target.value) || 0)}
              min={0}
              max={100}
            />
          </div>
          <div>
            <label className="form-label" style={{ fontSize: '0.75rem' }}>Справа</label>
            <input
              type="number"
              className="form-input form-input-sm"
              value={pageSettings.margins.right}
              onChange={(e) => onMarginChange('right', parseInt(e.target.value) || 0)}
              min={0}
              max={100}
            />
          </div>
        </div>
      </div>

      {/* Page Numbers */}
      <div className="form-group">
        <label className="form-label flex items-center gap-sm">
          <input
            type="checkbox"
            checked={pageSettings.pageNumbers?.enabled || false}
            onChange={(e) => onPageSettingChange('pageNumbers', {
              ...(pageSettings.pageNumbers || DEFAULT_PAGE_SETTINGS.pageNumbers!),
              enabled: e.target.checked,
            })}
          />
          Нумерация страниц
        </label>
      </div>

      {pageSettings.pageNumbers?.enabled && (
        <>
          <div className="form-group">
            <label className="form-label">Позиция</label>
            <select
              className="form-select form-input-sm"
              value={pageSettings.pageNumbers?.position || 'bottom'}
              onChange={(e) => onPageSettingChange('pageNumbers', {
                ...pageSettings.pageNumbers!,
                position: e.target.value as 'top' | 'bottom',
              })}
            >
              <option value="top">Сверху</option>
              <option value="bottom">Снизу</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Выравнивание</label>
            <select
              className="form-select form-input-sm"
              value={pageSettings.pageNumbers?.align || 'center'}
              onChange={(e) => onPageSettingChange('pageNumbers', {
                ...pageSettings.pageNumbers!,
                align: e.target.value as 'left' | 'center' | 'right',
              })}
            >
              <option value="left">По левому краю</option>
              <option value="center">По центру</option>
              <option value="right">По правому краю</option>
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Формат</label>
            <input
              type="text"
              className="form-input form-input-sm"
              value={pageSettings.pageNumbers?.format || '{n}'}
              onChange={(e) => onPageSettingChange('pageNumbers', {
                ...pageSettings.pageNumbers!,
                format: e.target.value,
              })}
              placeholder="{n}, Страница {n}, {n} из {total}"
            />
          </div>
        </>
      )}
    </div>
  );
}

