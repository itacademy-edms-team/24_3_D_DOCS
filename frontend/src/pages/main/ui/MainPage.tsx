import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@entities';
import { Meta } from '@ui';
import style from './MainPage.module.css';

const MainPage = (): React.JSX.Element => {
  const navigate = useNavigate();
  const { logout, user } = useAuthStore();

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
      <Meta title="Dashboard | DDOCS" lang="ru" description="Панель управления DDOCS" />
      <div className={style.container}>
        <div style={{ 
          display: 'flex', 
          flexDirection: 'column', 
          alignItems: 'center', 
          justifyContent: 'center', 
          height: '100vh',
          gap: '2rem'
        }}>
          <h1 style={{ fontSize: '2rem', fontWeight: 700 }}>Добро пожаловать в DDOCS</h1>
          <div style={{ 
            display: 'flex', 
            flexDirection: 'column', 
            alignItems: 'center', 
            gap: '1rem',
            padding: '2rem',
            background: 'rgba(255, 255, 255, 0.1)',
            borderRadius: '12px',
            backdropFilter: 'blur(10px)'
          }}>
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
        </div>
      </div>
    </>
  );
};

export default MainPage;
