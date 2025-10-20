import { useState } from 'react';
import { Card } from '@ui';
import { LoginForm, RegisterForm } from '@features';
import { Meta } from '@ui';
import styles from './AuthPage.module.css';

type Tab = 'login' | 'register';

export const AuthPage = () => {
	const [activeTab, setActiveTab] = useState<Tab>('login');
	const [isAnimating, setIsAnimating] = useState(false);

	const handleTabChange = (tab: Tab) => {
		if (tab === activeTab) return;
		setIsAnimating(true);
		setTimeout(() => {
			setActiveTab(tab);
			setTimeout(() => setIsAnimating(false), 50);
		}, 150);
	};

	return (
		<>
			<Meta title="Авторизация | DDOCS" />
			<div className={styles.container}>
				<div className={styles.background}>
					<div className={styles.gridPattern} />
					<div className={styles.glow1} />
					<div className={styles.glow2} />
				</div>

				<div className={styles.logo}>
					<div className={styles.logoIcon}>📄</div>
					<h1 className={styles.logoText}>DDOCS</h1>
				</div>

				<Card className={styles.card}>
					<div className={styles.tabs}>
						<button
							type="button"
							className={`${styles.tab} ${activeTab === 'login' ? styles.active : ''}`}
							onClick={() => handleTabChange('login')}
						>
							Вход
						</button>
						<button
							type="button"
							className={`${styles.tab} ${activeTab === 'register' ? styles.active : ''}`}
							onClick={() => handleTabChange('register')}
						>
							Регистрация
						</button>
						<div 
							className={styles.tabIndicator} 
							style={{ 
								transform: `translateX(${activeTab === 'login' ? '0' : '100'}%)`
							}}
						/>
					</div>

					<div className={`${styles.formContainer} ${isAnimating ? styles.animating : ''}`}>
						{activeTab === 'login' ? <LoginForm /> : <RegisterForm />}
					</div>
				</Card>
			</div>
		</>
	);
};

export default AuthPage;

