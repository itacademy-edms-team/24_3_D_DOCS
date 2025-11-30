import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import type { Profile, EntityType, EntityStyle, PageSettings } from '../../../../shared/src/types';
import { ALL_ENTITY_TYPES, ENTITY_LABELS, DEFAULT_PAGE_SETTINGS } from '../../../../shared/src/types';
import { profileApi } from '../../infrastructure/api';
import { EntityStyleEditor } from '../components/EntityStyleEditor';

export function ProfileEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selectedEntity, setSelectedEntity] = useState<EntityType>('paragraph');

  useEffect(() => {
    if (id) {
      loadProfile(id);
    }
  }, [id]);

  async function loadProfile(profileId: string) {
    try {
      const data = await profileApi.getById(profileId);
      setProfile(data);
    } catch (error) {
      console.error('Failed to load profile:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!profile || !id) return;

    setSaving(true);
    try {
      await profileApi.update(id, {
        name: profile.name,
        page: profile.page,
        entities: profile.entities,
      });
    } catch (error) {
      console.error('Failed to save profile:', error);
    } finally {
      setSaving(false);
    }
  }

  function handleNameChange(name: string) {
    if (!profile) return;
    setProfile({ ...profile, name });
  }

  function handlePageSettingChange<K extends keyof PageSettings>(key: K, value: PageSettings[K]) {
    if (!profile) return;
    setProfile({
      ...profile,
      page: { ...profile.page, [key]: value },
    });
  }

  function handleMarginChange(side: 'top' | 'right' | 'bottom' | 'left', value: number) {
    if (!profile) return;
    setProfile({
      ...profile,
      page: {
        ...profile.page,
        margins: { ...profile.page.margins, [side]: value },
      },
    });
  }

  function handleEntityStyleChange(entityType: EntityType, style: EntityStyle) {
    if (!profile) return;
    setProfile({
      ...profile,
      entities: { ...profile.entities, [entityType]: style },
    });
  }

  function handleResetEntityStyle(entityType: EntityType) {
    if (!profile) return;
    const newEntities = { ...profile.entities };
    delete newEntities[entityType];
    setProfile({ ...profile, entities: newEntities });
  }

  if (loading) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Загрузка...</div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Профиль не найден</div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="container">
        {/* Header */}
        <div className="flex justify-between items-center mb-lg">
          <div className="flex items-center gap-md">
            <button className="btn btn-ghost" onClick={() => navigate('/')}>
              ← Назад
            </button>
            <input
              type="text"
              className="form-input"
              value={profile.name}
              onChange={(e) => handleNameChange(e.target.value)}
              style={{ fontSize: '1.25rem', fontWeight: 600, width: 300 }}
            />
          </div>
          <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
            {saving ? 'Сохранение...' : 'Сохранить'}
          </button>
        </div>

        <div className="grid" style={{ gridTemplateColumns: '300px 1fr', gap: 'var(--spacing-lg)' }}>
          {/* Sidebar */}
          <div>
            {/* Page Settings */}
            <div className="card mb-lg">
              <h3 className="card-title mb-md">Настройки страницы</h3>

              <div className="form-group">
                <label className="form-label">Размер</label>
                <select
                  className="form-select"
                  value={profile.page.size}
                  onChange={(e) => handlePageSettingChange('size', e.target.value as 'A4' | 'A5' | 'Letter')}
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
                  value={profile.page.orientation}
                  onChange={(e) => handlePageSettingChange('orientation', e.target.value as 'portrait' | 'landscape')}
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
                      value={profile.page.margins.top}
                      onChange={(e) => handleMarginChange('top', parseInt(e.target.value) || 0)}
                      min={0}
                      max={100}
                    />
                  </div>
                  <div>
                    <label className="form-label" style={{ fontSize: '0.75rem' }}>Снизу</label>
                    <input
                      type="number"
                      className="form-input form-input-sm"
                      value={profile.page.margins.bottom}
                      onChange={(e) => handleMarginChange('bottom', parseInt(e.target.value) || 0)}
                      min={0}
                      max={100}
                    />
                  </div>
                  <div>
                    <label className="form-label" style={{ fontSize: '0.75rem' }}>Слева</label>
                    <input
                      type="number"
                      className="form-input form-input-sm"
                      value={profile.page.margins.left}
                      onChange={(e) => handleMarginChange('left', parseInt(e.target.value) || 0)}
                      min={0}
                      max={100}
                    />
                  </div>
                  <div>
                    <label className="form-label" style={{ fontSize: '0.75rem' }}>Справа</label>
                    <input
                      type="number"
                      className="form-input form-input-sm"
                      value={profile.page.margins.right}
                      onChange={(e) => handleMarginChange('right', parseInt(e.target.value) || 0)}
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
                    checked={profile.page.pageNumbers?.enabled || false}
                    onChange={(e) => handlePageSettingChange('pageNumbers', {
                      ...(profile.page.pageNumbers || DEFAULT_PAGE_SETTINGS.pageNumbers!),
                      enabled: e.target.checked,
                    })}
                  />
                  Нумерация страниц
                </label>
              </div>

              {profile.page.pageNumbers?.enabled && (
                <>
                  <div className="form-group">
                    <label className="form-label">Позиция</label>
                    <select
                      className="form-select form-input-sm"
                      value={profile.page.pageNumbers?.position || 'bottom'}
                      onChange={(e) => handlePageSettingChange('pageNumbers', {
                        ...profile.page.pageNumbers!,
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
                      value={profile.page.pageNumbers?.align || 'center'}
                      onChange={(e) => handlePageSettingChange('pageNumbers', {
                        ...profile.page.pageNumbers!,
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
                      value={profile.page.pageNumbers?.format || '{n}'}
                      onChange={(e) => handlePageSettingChange('pageNumbers', {
                        ...profile.page.pageNumbers!,
                        format: e.target.value,
                      })}
                      placeholder="{n}, Страница {n}, {n} из {total}"
                    />
                  </div>
                </>
              )}
            </div>

            {/* Entity List */}
            <div className="card">
              <h3 className="card-title mb-md">Стили сущностей</h3>
              <ul className="list">
                {ALL_ENTITY_TYPES.map((entityType) => (
                  <li
                    key={entityType}
                    className={`list-item list-item-clickable ${selectedEntity === entityType ? 'selected' : ''}`}
                    onClick={() => setSelectedEntity(entityType)}
                    style={{
                      background: selectedEntity === entityType ? 'var(--bg-hover)' : undefined,
                      borderLeft: selectedEntity === entityType ? '3px solid var(--accent-primary)' : '3px solid transparent',
                    }}
                  >
                    <span>{ENTITY_LABELS[entityType]}</span>
                    {profile.entities[entityType] && (
                      <span style={{ color: 'var(--accent-success)', fontSize: '0.75rem' }}>●</span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Main Content - Entity Style Editor */}
          <div>
            <EntityStyleEditor
              entityType={selectedEntity}
              style={profile.entities[selectedEntity] || {}}
              onChange={(style) => handleEntityStyleChange(selectedEntity, style)}
              showReset={!!profile.entities[selectedEntity]}
              onReset={() => handleResetEntityStyle(selectedEntity)}
            />
          </div>
        </div>
      </div>
    </div>
  );
}

