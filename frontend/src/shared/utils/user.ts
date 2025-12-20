import type { User } from '@/entities/auth/types';

export function getUserInitials(user: User | null): string {
	if (!user?.email) return 'U';
	const parts = user.email.split('@')[0].split('.');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[1][0]).toUpperCase();
	}
	return user.email[0].toUpperCase();
}
