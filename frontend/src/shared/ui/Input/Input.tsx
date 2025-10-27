import type React from 'react';
import { useState } from 'react';
import styles from './Input.module.css';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
	label?: string;
	error?: string;
	fullWidth?: boolean;
	showPasswordToggle?: boolean;
}

export const Input: React.FC<InputProps> = ({
	label,
	error,
	fullWidth = false,
	showPasswordToggle = false,
	className = '',
	type,
	...props
}) => {
	const [isPasswordVisible, setIsPasswordVisible] = useState(false);
	
	const containerClass = [
		styles.container,
		fullWidth && styles.fullWidth,
		className,
	]
		.filter(Boolean)
		.join(' ');

	const inputType = showPasswordToggle && type === 'password' 
		? (isPasswordVisible ? 'text' : 'password')
		: type;

	const togglePasswordVisibility = () => {
		setIsPasswordVisible(!isPasswordVisible);
	};

	return (
		<div className={containerClass}>
			{label && <label className={styles.label}>{label}</label>}
			<div className={styles.inputWrapper}>
				<input
					className={`${styles.input} ${error ? styles.error : ''}`}
					type={inputType}
					{...props}
				/>
				{showPasswordToggle && type === 'password' && (
					<button
						type="button"
						className={styles.passwordToggle}
						onClick={togglePasswordVisibility}
						tabIndex={-1}
					>
						{isPasswordVisible ? '👁️' : '👁️‍🗨️'}
					</button>
				)}
			</div>
			{error && <span className={styles.errorText}>{error}</span>}
		</div>
	);
};

