import type React from 'react';
import styles from './Button.module.css';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: 'primary' | 'secondary';
	isLoading?: boolean;
	fullWidth?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
	children,
	variant = 'primary',
	isLoading = false,
	fullWidth = false,
	disabled,
	className = '',
	...props
}) => {
	const buttonClass = [
		styles.button,
		styles[variant],
		fullWidth && styles.fullWidth,
		isLoading && styles.loading,
		className,
	]
		.filter(Boolean)
		.join(' ');

	return (
		<button
			type="button"
			className={buttonClass}
			disabled={disabled || isLoading}
			{...props}
		>
			{isLoading ? (
				<span className={styles.spinner} />
			) : (
				children
			)}
		</button>
	);
};

