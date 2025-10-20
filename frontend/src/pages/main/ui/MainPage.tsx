import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@entities';
import { Meta } from '@ui';
import style from './MainPage.module.css';

const MainPage = (): React.JSX.Element => {
  const navigate = useNavigate();
  const { logout, user } = useAuthStore();
  const [markdownContent, setMarkdownContent] = useState('# Заголовок документа\n\nНачните печатать ваш документ здесь...');

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/');
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <>
      <Meta title="Dashboard | DDOCS" />
      <div className={style.container}>
        {/* Левая панель - Список схем/документов */}
        <aside className={style.sidebar}>
          <div className={style.sidebarHeader}>
            <div className={style.sidebarTop}>
              <h2 className={style.sidebarTitle}>Документы</h2>
              <button className={style.newDocButton} type="button">+ Создать</button>
            </div>
          </div>
          <div className={style.documentList}>
            <div className={`${style.documentItem} ${style.active}`}>
              <div className={style.documentIcon}>📄</div>
              <div className={style.documentInfo}>
                <span className={style.documentName}>Документ 1</span>
                <span className={style.documentDate}>Сегодня</span>
              </div>
            </div>
            <div className={style.documentItem}>
              <div className={style.documentIcon}>📄</div>
              <div className={style.documentInfo}>
                <span className={style.documentName}>Документ 2</span>
                <span className={style.documentDate}>Вчера</span>
              </div>
            </div>
            <div className={style.documentItem}>
              <div className={style.documentIcon}>📄</div>
              <div className={style.documentInfo}>
                <span className={style.documentName}>Документ 3</span>
                <span className={style.documentDate}>5 дней назад</span>
              </div>
            </div>
          </div>
          <div className={style.sidebarFooter}>
            <div className={style.userInfo}>
              <div className={style.userAvatar}>👤</div>
              <div className={style.userDetails}>
                <span className={style.userName}>{user?.name || 'Пользователь'}</span>
                <span className={style.userEmail}>{user?.email}</span>
              </div>
            </div>
            <button className={style.logoutButton} onClick={handleLogout} type="button">
              🚪 Выход
            </button>
          </div>
        </aside>

        {/* Центральная часть - Markdown редактор */}
        <main className={style.editor}>
          <div className={style.editorHeader}>
            <h3 className={style.editorTitle}>Документ 1</h3>
            <div className={style.editorActions}>
              <button className={style.actionButton} type="button">💾 Сохранить</button>
              <button className={style.actionButton} type="button">📤 Экспорт</button>
            </div>
          </div>
          <textarea
            className={style.markdownArea}
            value={markdownContent}
            onChange={(e) => setMarkdownContent(e.target.value)}
            placeholder="Начните печатать markdown..."
          />
        </main>

        {/* Правая панель - Превью PDF */}
        <aside className={style.preview}>
          <div className={style.previewHeader}>
            <h3 className={style.previewTitle}>Превью PDF</h3>
          </div>
          <div className={style.previewContent}>
            <div className={style.previewPlaceholder}>
              <div className={style.previewIcon}>📋</div>
              <p>Здесь будет отображаться<br />PDF-превью документа</p>
            </div>
          </div>
        </aside>
      </div>
    </>
  );
}

export default MainPage;
