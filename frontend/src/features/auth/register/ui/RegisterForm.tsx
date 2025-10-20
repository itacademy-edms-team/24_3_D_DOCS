import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuthStore } from '@entities';
import { Button, Input } from '@ui';
import styles from './RegisterForm.module.css';

const registerSchema = z.object({
	name: z.string().min(2, 'Имя должно быть минимум 2 символа'),
	email: z.string().email('Неверный формат email'),
	password: z.string().min(6, 'Пароль должен быть минимум 6 символов'),
	confirmPassword: z.string(),
}).refine((data) => data.password === data.confirmPassword, {
	message: 'Пароли не совпадают',
	path: ['confirmPassword'],
});

export const RegisterForm = () => {
	const navigate = useNavigate();
	const register = useAuthStore((state) => state.register);
	const isLoading = useAuthStore((state) => state.isLoading);

	const [formData, setFormData] = useState({
		name: '',
		email: '',
		password: '',
		confirmPassword: '',
	});
	const [errors, setErrors] = useState<Record<string, string>>({});
	const [apiError, setApiError] = useState('');

	const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const { name, value } = e.target;
		setFormData((prev) => ({ ...prev, [name]: value }));
		if (errors[name]) {
			setErrors((prev) => ({ ...prev, [name]: '' }));
		}
		setApiError('');
	};

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setErrors({});
		setApiError('');

		const result = registerSchema.safeParse(formData);
		if (!result.success) {
			const fieldErrors: Record<string, string> = {};
			for (const error of result.error.errors) {
				fieldErrors[error.path[0] as string] = error.message;
			}
			setErrors(fieldErrors);
			return;
		}

		try {
			await register({
				name: formData.name,
				email: formData.email,
				password: formData.password,
			});
			navigate('/dashboard');
		} catch (error: any) {
			setApiError(error.message || 'Ошибка регистрации');
		}
	};

	return (
		<form className={styles.form} onSubmit={handleSubmit}>
			<h2 className={styles.title}>Регистрация</h2>

			<Input
				name="name"
				type="text"
				label="Имя"
				placeholder="Иван Петров"
				value={formData.name}
				onChange={handleChange}
				error={errors.name}
				fullWidth
				autoComplete="name"
			/>

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
				autoComplete="new-password"
			/>

			<Input
				name="confirmPassword"
				type="password"
				label="Подтвердите пароль"
				placeholder="••••••••"
				value={formData.confirmPassword}
				onChange={handleChange}
				error={errors.confirmPassword}
				fullWidth
				autoComplete="new-password"
			/>

			{apiError && <div className={styles.apiError}>{apiError}</div>}

			<Button
				type="submit"
				variant="primary"
				fullWidth
				isLoading={isLoading}
			>
				Зарегистрироваться
			</Button>
		</form>
	);
};

