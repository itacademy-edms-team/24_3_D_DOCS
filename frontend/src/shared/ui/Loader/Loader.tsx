import type React from 'react';
import styles from './Loader.module.css';

interface LoaderProps {
	size?: 'small' | 'medium' | 'large';
	fullScreen?: boolean;
}

export const Loader: React.FC<LoaderProps> = ({ size = 'medium', fullScreen = false }) => {
	if (fullScreen) {
		return (
			<div className={styles.fullScreen}>
				<div className={`${styles.loader} ${styles[size]}`} />
			</div>
		);
	}

	return <div className={`${styles.loader} ${styles[size]}`} />;
};

