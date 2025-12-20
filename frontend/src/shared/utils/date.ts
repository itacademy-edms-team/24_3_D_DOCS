export function formatDate(dateString: string): string {
	const date = new Date(dateString);
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffMins = Math.floor(diffMs / 60000);
	const diffHours = Math.floor(diffMs / 3600000);
	const diffDays = Math.floor(diffMs / 86400000);
	const diffMonths = Math.floor(diffDays / 30);

	if (diffMins < 1) return 'только что';
	if (diffMins < 60) return `${diffMins} мин. назад`;
	if (diffHours < 24) return `${diffHours} ч. назад`;
	if (diffDays < 30) return `${diffDays} дн. назад`;
	if (diffMonths < 12) return `${diffMonths} мес. назад`;
	return date.toLocaleDateString('ru-RU');
}
