import { useState } from 'react';
import { Card } from '@ui';
import { LoginForm, RegisterForm } from '@features';
import { Meta } from '@ui';
import styles from './AuthPage.module.css';

type Tab = 'login' | 'register';

export const AuthPage = () => {
	const [activeTab, setActiveTab] = useState<Tab>('login');

	return (
		<>
			<Meta title="Авторизация | Rusal Project" />
			<div className={styles.container}>
				<div className={styles.background}>
					<div className={styles.gradient1} />
					<div className={styles.gradient2} />
					<div className={styles.gradient3} />
				</div>

				<Card className={styles.card}>
					<div className={styles.tabs}>
						<button
							type="button"
							className={`${styles.tab} ${activeTab === 'login' ? styles.active : ''}`}
							onClick={() => setActiveTab('login')}
						>
							Вход
						</button>
						<button
							type="button"
							className={`${styles.tab} ${activeTab === 'register' ? styles.active : ''}`}
							onClick={() => setActiveTab('register')}
						>
							Регистрация
						</button>
					</div>

					<div className={styles.formContainer}>
						{activeTab === 'login' ? <LoginForm /> : <RegisterForm />}
					</div>
				</Card>
			</div>
		</>
	);
};

export default AuthPage;

