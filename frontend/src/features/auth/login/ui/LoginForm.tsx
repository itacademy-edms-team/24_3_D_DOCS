import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuthStore } from '@entities';
import { Button, Input } from '@ui';
import styles from './LoginForm.module.css';

const loginSchema = z.object({
	email: z.string().email('Неверный формат email'),
	password: z.string().min(6, 'Пароль должен быть минимум 6 символов'),
});

export const LoginForm = () => {
	const navigate = useNavigate();
	const { login, isLoading, error, clearError } = useAuthStore();

	const [formData, setFormData] = useState({ email: '', password: '' });
	const [errors, setErrors] = useState<Record<string, string>>({});

	// Очищаем ошибку при монтировании компонента
	useEffect(() => {
		clearError();
	}, [clearError]);

	const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const { name, value } = e.target;
		setFormData((prev) => ({ ...prev, [name]: value }));
		// Clear error when user starts typing
		if (errors[name]) {
			setErrors((prev) => ({ ...prev, [name]: '' }));
		}
		if (error) {
			clearError();
		}
	};

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setErrors({});
		clearError();

		// Validate form data
		const result = loginSchema.safeParse(formData);

		if (!result.success) {
			const fieldErrors: Record<string, string> = {};
			// Zod v4 использует .issues вместо .errors
			const issues = result.error?.issues || [];
			for (const issue of issues) {
				if (issue.path && issue.path.length > 0) {
					fieldErrors[issue.path[0] as string] = issue.message;
				}
			}

			setErrors(fieldErrors);
			return;
		}

		try {
			clearError();
			await login(formData);
			navigate('/dashboard');
		} catch (error: any) {
			console.error('Login error:', error);
			// Ошибка уже установлена в store через login функцию
		}
	};

	return (
		<form className={styles.form} onSubmit={handleSubmit}>
			<h2 className={styles.title}>Вход</h2>

			<Input
				name="email"
				type="email"
				label="Email"
				placeholder="your@email.com"
				value={formData.email}
				onChange={handleChange}
				error={errors.email}
				fullWidth
				autoComplete="email"
			/>

			<Input
				name="password"
				type="password"
				label="Пароль"
				placeholder="••••••••"
				value={formData.password}
				onChange={handleChange}
				error={errors.password}
				fullWidth
				autoComplete="current-password"
				showPasswordToggle
			/>

			{error && <div className={styles.apiError}>{error}</div>}

			<Button
				type="submit"
				variant="primary"
				fullWidth
				isLoading={isLoading}
			>
				Войти
			</Button>
		</form>
	);
};

