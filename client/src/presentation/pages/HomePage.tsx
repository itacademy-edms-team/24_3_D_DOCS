import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Profile, DocumentMeta } from '../../../../shared/src/types';
import { profileApi, documentApi } from '../../infrastructure/api';

export function HomePage() {
  const navigate = useNavigate();
  const [profiles, setProfiles] = useState<Profile[]>([]);
  const [documents, setDocuments] = useState<DocumentMeta[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'documents' | 'profiles'>('documents');

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    try {
      const [profilesData, documentsData] = await Promise.all([
        profileApi.getAll(),
        documentApi.getAll(),
      ]);
      setProfiles(profilesData);
      setDocuments(documentsData);
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreateProfile() {
    try {
      const profile = await profileApi.create({ name: 'Новый профиль' });
      navigate(`/profile/${profile.id}`);
    } catch (error) {
      console.error('Failed to create profile:', error);
    }
  }

  async function handleCreateDocument() {
    try {
      const document = await documentApi.create({
        name: 'Новый документ',
        profileId: profiles[0]?.id || '',
      });
      navigate(`/document/${document.id}/edit`);
    } catch (error) {
      console.error('Failed to create document:', error);
    }
  }

  async function handleDeleteProfile(id: string) {
    if (!confirm('Удалить профиль?')) return;
    try {
      await profileApi.delete(id);
      setProfiles(profiles.filter((p) => p.id !== id));
    } catch (error) {
      console.error('Failed to delete profile:', error);
    }
  }

  async function handleDeleteDocument(id: string) {
    if (!confirm('Удалить документ?')) return;
    try {
      await documentApi.delete(id);
      setDocuments(documents.filter((d) => d.id !== id));
    } catch (error) {
      console.error('Failed to delete document:', error);
    }
  }

  function getProfileName(profileId: string): string {
    return profiles.find((p) => p.id === profileId)?.name || 'Без профиля';
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  if (loading) {
    return (
      <div className="page flex items-center justify-center">
        <div className="text-muted">Загрузка...</div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="container">
        {/* Header */}
        <div className="flex justify-between items-center mb-lg">
          <div>
            <h1 style={{ fontSize: '1.75rem', fontWeight: 700, marginBottom: 4 }}>
              MD → PDF Converter
            </h1>
            <p className="text-muted">
              Конвертируйте Markdown документы в PDF с настраиваемыми стилями
            </p>
          </div>
        </div>

        {/* Tabs */}
        <div className="tabs mb-lg">
          <button
            className={`tab ${activeTab === 'documents' ? 'active' : ''}`}
            onClick={() => setActiveTab('documents')}
          >
            Документы ({documents.length})
          </button>
          <button
            className={`tab ${activeTab === 'profiles' ? 'active' : ''}`}
            onClick={() => setActiveTab('profiles')}
          >
            Профили ({profiles.length})
          </button>
        </div>

        {/* Documents Tab */}
        {activeTab === 'documents' && (
          <div>
            <div className="flex justify-between items-center mb-md">
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600 }}>Документы</h2>
              <button className="btn btn-primary" onClick={handleCreateDocument}>
                + Создать документ
              </button>
            </div>

            {documents.length === 0 ? (
              <div className="card text-center" style={{ padding: '3rem' }}>
                <p className="text-muted mb-md">У вас пока нет документов</p>
                <button className="btn btn-primary" onClick={handleCreateDocument}>
                  Создать первый документ
                </button>
              </div>
            ) : (
              <div className="grid grid-3">
                {documents.map((doc) => (
                  <div
                    key={doc.id}
                    className="card card-hover"
                    onClick={() => navigate(`/document/${doc.id}/edit`)}
                  >
                    <div className="card-header">
                      <h3 className="card-title">{doc.name}</h3>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteDocument(doc.id);
                        }}
                      >
                        🗑️
                      </button>
                    </div>
                    <p className="card-subtitle">
                      Профиль: {getProfileName(doc.profileId)}
                    </p>
                    <p className="card-subtitle">
                      Изменён: {formatDate(doc.updatedAt)}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Profiles Tab */}
        {activeTab === 'profiles' && (
          <div>
            <div className="flex justify-between items-center mb-md">
              <h2 style={{ fontSize: '1.25rem', fontWeight: 600 }}>Профили стилей</h2>
              <button className="btn btn-primary" onClick={handleCreateProfile}>
                + Создать профиль
              </button>
            </div>

            {profiles.length === 0 ? (
              <div className="card text-center" style={{ padding: '3rem' }}>
                <p className="text-muted mb-md">У вас пока нет профилей</p>
                <button className="btn btn-primary" onClick={handleCreateProfile}>
                  Создать первый профиль
                </button>
              </div>
            ) : (
              <div className="grid grid-3">
                {profiles.map((profile) => (
                  <div
                    key={profile.id}
                    className="card card-hover"
                    onClick={() => navigate(`/profile/${profile.id}`)}
                  >
                    <div className="card-header">
                      <h3 className="card-title">{profile.name}</h3>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteProfile(profile.id);
                        }}
                      >
                        🗑️
                      </button>
                    </div>
                    <p className="card-subtitle">
                      Страница: {profile.page.size}, {profile.page.orientation === 'portrait' ? 'Книжная' : 'Альбомная'}
                    </p>
                    <p className="card-subtitle">
                      Изменён: {formatDate(profile.updatedAt)}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

