import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@entities';
import { Button } from '@ui';

interface LogoutButtonProps {
	variant?: 'primary' | 'secondary';
	className?: string;
}

export const LogoutButton: React.FC<LogoutButtonProps> = ({ 
	variant = 'secondary',
	className = '',
}) => {
	const navigate = useNavigate();
	const logout = useAuthStore((state) => state.logout);
	const isLoading = useAuthStore((state) => state.isLoading);

	const handleLogout = async () => {
		await logout();
		navigate('/auth');
	};

	return (
		<Button
			variant={variant}
			onClick={handleLogout}
			isLoading={isLoading}
			className={className}
		>
			Выйти
		</Button>
	);
};

