import { useAuthStore } from '@entities';
import { LogoutButton } from '@features';
import { Card, Meta } from '@ui';
import styles from './DashboardPage.module.css';

export const DashboardPage = () => {
	const user = useAuthStore((state) => state.user);

	return (
		<>
			<Meta title="Dashboard | Rusal Project" />
			<div className={styles.container}>
				<header className={styles.header}>
					<div className={styles.headerContent}>
						<h1 className={styles.logo}>Rusal Project</h1>
						<div className={styles.userSection}>
							<div className={styles.userInfo}>
								<span className={styles.userName}>{user?.name}</span>
								<span className={styles.userRole}>{user?.role}</span>
							</div>
							<LogoutButton />
						</div>
					</div>
				</header>

				<main className={styles.main}>
					<Card className={styles.welcomeCard}>
						<div className={styles.welcome}>
							<h2 className={styles.welcomeTitle}>
								Добро пожаловать, {user?.name}! 🎉
							</h2>
							<p className={styles.welcomeText}>
								Вы успешно вошли в систему
							</p>
						</div>

						<div className={styles.userDetails}>
							<div className={styles.detail}>
								<span className={styles.detailLabel}>Email:</span>
								<span className={styles.detailValue}>{user?.email}</span>
							</div>
							<div className={styles.detail}>
								<span className={styles.detailLabel}>Роль:</span>
								<span className={styles.detailValue}>{user?.role}</span>
							</div>
							<div className={styles.detail}>
								<span className={styles.detailLabel}>ID:</span>
								<span className={styles.detailValue}>{user?.id}</span>
							</div>
							<div className={styles.detail}>
								<span className={styles.detailLabel}>Дата регистрации:</span>
								<span className={styles.detailValue}>
									{user?.createdAt ? new Date(user.createdAt).toLocaleDateString('ru-RU') : '—'}
								</span>
							</div>
						</div>
					</Card>
				</main>
			</div>
		</>
	);
};

export default DashboardPage;

