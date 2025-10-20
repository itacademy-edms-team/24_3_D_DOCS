import { useState, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@entities';
import { Button, Input } from '@ui';
import styles from './RegisterForm.module.css';

export const RegisterForm = () => {
	const navigate = useNavigate();
	const { sendVerification, register, isLoading, error, clearError } = useAuthStore();
	const [step, setStep] = useState<'email' | 'code'>('email');
	
	// Шаг 1: email, password, name
	const [email, setEmail] = useState('');
	const [password, setPassword] = useState('');
	const [name, setName] = useState('');
	const [errors, setErrors] = useState<{ email?: string; password?: string; name?: string }>({});

	// Шаг 2: code
	const [code, setCode] = useState('');
	const [codeError, setCodeError] = useState('');

	const validateEmail = (value: string): boolean => {
		if (!value) {
			setErrors(prev => ({ ...prev, email: 'Email обязателен' }));
			return false;
		}
		if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
			setErrors(prev => ({ ...prev, email: 'Введите корректный email' }));
			return false;
		}
		setErrors(prev => ({ ...prev, email: undefined }));
		return true;
	};

	const validatePassword = (value: string): boolean => {
		if (!value) {
			setErrors(prev => ({ ...prev, password: 'Пароль обязателен' }));
			return false;
		}
		if (value.length < 6) {
			setErrors(prev => ({ ...prev, password: 'Пароль должен быть минимум 6 символов' }));
			return false;
		}
		setErrors(prev => ({ ...prev, password: undefined }));
		return true;
	};

	const validateName = (value: string): boolean => {
		if (!value) {
			setErrors(prev => ({ ...prev, name: 'Имя обязательно' }));
			return false;
		}
		if (value.length < 2) {
			setErrors(prev => ({ ...prev, name: 'Имя должно быть минимум 2 символа' }));
			return false;
		}
		setErrors(prev => ({ ...prev, name: undefined }));
		return true;
	};

	const validateCode = (value: string): boolean => {
		if (!value) {
			setCodeError('Код обязателен');
			return false;
		}
		if (value.length !== 6) {
			setCodeError('Код должен содержать 6 цифр');
			return false;
		}
		setCodeError('');
		return true;
	};

	const handleSendVerification = async (e: FormEvent) => {
		e.preventDefault();
		
		const emailValid = validateEmail(email);
		const passwordValid = validatePassword(password);
		const nameValid = validateName(name);

		if (!emailValid || !passwordValid || !nameValid) {
			return;
		}

		try {
			clearError();
			await sendVerification({ email, password, name });
			setStep('code');
		} catch (err) {
			console.error('Ошибка отправки кода:', err);
		}
	};

	const handleVerifyCode = async (e: FormEvent) => {
		e.preventDefault();
		
		if (!validateCode(code)) {
			return;
		}

		try {
			clearError();
			await register({ email, code });
			navigate('/dashboard');
		} catch (err) {
			console.error('Ошибка регистрации:', err);
		}
	};

	const handleBackToEmail = () => {
		setStep('email');
		setCode('');
		setCodeError('');
		clearError();
	};

	return (
		<div className={styles.form}>
			<h2 className={styles.title}>
				{step === 'email' ? 'Создать аккаунт' : 'Введите код'}
			</h2>

			{step === 'email' ? (
				<form onSubmit={handleSendVerification}>
					<Input
						type="email"
						placeholder="Email"
						value={email}
						onChange={(e) => {
							setEmail(e.target.value);
							if (errors.email) validateEmail(e.target.value);
						}}
						onBlur={(e) => validateEmail(e.target.value)}
						error={errors.email}
					/>
					<Input
						type="password"
						placeholder="Пароль"
						value={password}
						onChange={(e) => {
							setPassword(e.target.value);
							if (errors.password) validatePassword(e.target.value);
						}}
						onBlur={(e) => validatePassword(e.target.value)}
						error={errors.password}
					/>
					<Input
						type="text"
						placeholder="Имя"
						value={name}
						onChange={(e) => {
							setName(e.target.value);
							if (errors.name) validateName(e.target.value);
						}}
						onBlur={(e) => validateName(e.target.value)}
						error={errors.name}
					/>

					{error && <p className={styles.error}>{error}</p>}

					<Button type="submit" loading={isLoading} fullWidth>
						Отправить код
					</Button>
				</form>
			) : (
				<>
					<p className={styles.codeHint}>
						Мы отправили код подтверждения на <strong>{email}</strong>
					</p>
					<form onSubmit={handleVerifyCode}>
						<Input
							type="text"
							placeholder="Код из email (6 цифр)"
							value={code}
							onChange={(e) => {
								const value = e.target.value.replace(/\D/g, '').slice(0, 6);
								setCode(value);
								if (codeError && value) validateCode(value);
							}}
							onBlur={(e) => validateCode(e.target.value)}
							error={codeError}
							maxLength={6}
							autoFocus
						/>

						{error && <p className={styles.error}>{error}</p>}

						<Button type="submit" loading={isLoading} fullWidth disabled={code.length !== 6}>
							Подтвердить
						</Button>
					</form>
					<button
						type="button"
						className={styles.backButton}
						onClick={handleBackToEmail}
					>
						← Назад
					</button>
				</>
			)}
		</div>
	);
};

export default RegisterForm;
