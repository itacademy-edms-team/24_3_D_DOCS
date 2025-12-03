import { useParams, useNavigate } from 'react-router-dom';
import type { EntityType } from '../../../../shared/src/types';
import { EntityStyleEditor } from '../components/EntityStyleEditor';
import { ProfileEditorSidebar } from '../components/ProfileEditorSidebar';
import { useProfileEditor } from '../hooks/useProfileEditor';

export function ProfileEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const {
    profile,
    loading,
    saving,
    selectedEntity,
    setSelectedEntity,
    handleSave,
    handleNameChange,
    handlePageSettingChange,
    handleMarginChange,
    handleEntityStyleChange,
    handleResetEntityStyle,
  } = useProfileEditor(id);

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
          <ProfileEditorSidebar
            profile={profile}
            selectedEntity={selectedEntity}
            onEntitySelect={setSelectedEntity}
            onPageSettingChange={handlePageSettingChange}
            onMarginChange={handleMarginChange}
          />

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
