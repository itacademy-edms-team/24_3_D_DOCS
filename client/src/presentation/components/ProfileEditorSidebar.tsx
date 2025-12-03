import type { Profile, EntityType, PageSettings } from '../../../../shared/src/types';
import { ALL_ENTITY_TYPES, ENTITY_LABELS } from '../../../../shared/src/types';
import { PageSettingsEditor } from './PageSettingsEditor';

interface ProfileEditorSidebarProps {
  profile: Profile;
  selectedEntity: EntityType;
  onEntitySelect: (entityType: EntityType) => void;
  onPageSettingChange: <K extends keyof PageSettings>(key: K, value: PageSettings[K]) => void;
  onMarginChange: (side: 'top' | 'right' | 'bottom' | 'left', value: number) => void;
}

export function ProfileEditorSidebar({
  profile,
  selectedEntity,
  onEntitySelect,
  onPageSettingChange,
  onMarginChange,
}: ProfileEditorSidebarProps) {
  return (
    <div>
      {/* Page Settings */}
      <PageSettingsEditor
        pageSettings={profile.page}
        onPageSettingChange={onPageSettingChange}
        onMarginChange={onMarginChange}
      />

      {/* Entity List */}
      <div className="card">
        <h3 className="card-title mb-md">Стили сущностей</h3>
        <ul className="list">
          {ALL_ENTITY_TYPES.map((entityType) => (
            <li
              key={entityType}
              className={`list-item list-item-clickable ${selectedEntity === entityType ? 'selected' : ''}`}
              onClick={() => onEntitySelect(entityType)}
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
  );
}

