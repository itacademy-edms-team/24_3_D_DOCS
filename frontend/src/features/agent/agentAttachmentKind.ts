const IMAGE_EXT = new Set(['png', 'jpg', 'jpeg', 'webp', 'gif']);

export function fileExtension(fileName: string): string {
	const base = fileName.split(/[/\\]/).pop() ?? fileName;
	const i = base.lastIndexOf('.');
	return i >= 0 ? base.slice(i + 1).toLowerCase() : '';
}

export function isImageAttachment(fileName: string, contentType: string): boolean {
	if (contentType.startsWith('image/')) return true;
	return IMAGE_EXT.has(fileExtension(fileName));
}

export function isPdfAttachment(fileName: string, contentType: string): boolean {
	if (contentType.includes('pdf')) return true;
	return fileExtension(fileName) === 'pdf';
}

export function attachmentKindLabel(fileName: string, contentType: string): string {
	if (isPdfAttachment(fileName, contentType)) return 'PDF';
	const ext = fileExtension(fileName);
	return ext ? ext.toUpperCase() : 'FILE';
}
