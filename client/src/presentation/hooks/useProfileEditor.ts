import { useState, useEffect } from 'react';
import type { Profile, EntityType, EntityStyle, PageSettings } from '../../../../shared/src/types';
import { profileApi } from '../../infrastructure/api';

export function useProfileEditor(profileId: string | undefined) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selectedEntity, setSelectedEntity] = useState<EntityType>('paragraph');

  useEffect(() => {
    if (profileId) {
      loadProfile(profileId);
    }
  }, [profileId]);

  async function loadProfile(id: string) {
    try {
      const data = await profileApi.getById(id);
      setProfile(data);
    } catch (error) {
      console.error('Failed to load profile:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!profile || !profileId) return;

    setSaving(true);
    try {
      await profileApi.update(profileId, {
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

  return {
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
  };
}

