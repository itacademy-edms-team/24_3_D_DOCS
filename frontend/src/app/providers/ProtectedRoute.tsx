import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@entities';

interface ProtectedRouteProps {
	children: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
	const isAuth = useAuthStore((state) => state.isAuth);

	if (!isAuth) {
		return <Navigate to="/auth" replace />;
	}

	return <>{children}</>;
};

