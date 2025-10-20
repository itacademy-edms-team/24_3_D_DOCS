import { useState } from 'react';
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
	const login = useAuthStore((state) => state.login);
	const isLoading = useAuthStore((state) => state.isLoading);

	const [formData, setFormData] = useState({ email: '', password: '' });
	const [errors, setErrors] = useState<Record<string, string>>({});
	const [apiError, setApiError] = useState('');

	const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const { name, value } = e.target;
		setFormData((prev) => ({ ...prev, [name]: value }));
		// Clear error when user starts typing
		if (errors[name]) {
			setErrors((prev) => ({ ...prev, [name]: '' }));
		}
		setApiError('');
	};

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setErrors({});
		setApiError('');

		// Validate form data
		const result = loginSchema.safeParse(formData);
		if (!result.success) {
			const fieldErrors: Record<string, string> = {};
			for (const error of result.error.errors) {
				fieldErrors[error.path[0] as string] = error.message;
			}
			setErrors(fieldErrors);
			return;
		}

		try {
			await login(formData);
			navigate('/dashboard');
		} catch (error: any) {
			setApiError(error.message || 'Ошибка входа');
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
			/>

			{apiError && <div className={styles.apiError}>{apiError}</div>}

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

