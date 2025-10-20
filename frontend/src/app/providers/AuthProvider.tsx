import { useEffect, useState } from 'react';
import { useAuthStore } from '@entities';
import { Loader } from '@ui';

interface AuthProviderProps {
	children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
	const checkAuth = useAuthStore((state) => state.checkAuth);
	const [isInitialized, setIsInitialized] = useState(false);

	useEffect(() => {
		const initAuth = async () => {
			await checkAuth();
			setIsInitialized(true);
		};

		initAuth();
	}, [checkAuth]);

	if (!isInitialized) {
		return <div style={{ 
			display: 'flex', 
			justifyContent: 'center', 
			alignItems: 'center', 
			height: '100vh',
			background: 'linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%)'
		}}>
			<Loader />
		</div>;
	}

	return <>{children}</>;
};

