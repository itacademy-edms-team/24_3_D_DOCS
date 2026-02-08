import HttpClient from './HttpClient';

interface UploadAssetResponseDTO {
	fileName: string;
	originalFileName: string;
	size: number;
	contentType: string;
	url: string;
	path: string;
}

class UploadAPI extends HttpClient {
	constructor() {
		super();
	}

	/**
	 * Загрузить изображение для документа
	 * @param documentId ID документа
	 * @param file Файл изображения
	 * @returns Информация о загруженном файле
	 */
	async uploadAsset(
		documentId: string,
		file: File
	): Promise<UploadAssetResponseDTO> {
		const formData = new FormData();
		formData.append('file', file);

		return this.instance.post<UploadAssetResponseDTO>(
			`/api/upload/document/${documentId}/asset`,
			formData,
			{
				headers: {
					'Content-Type': 'multipart/form-data',
				},
			}
		).then((response) => response.data);
	}
}

export default new UploadAPI();
