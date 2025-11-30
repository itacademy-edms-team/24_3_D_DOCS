import { Router, Request, Response } from 'express';
import multer from 'multer';
import type { DocumentService } from '../../application/services/DocumentService';

// Configure multer for memory storage
const upload = multer({
  storage: multer.memoryStorage(),
  limits: {
    fileSize: 10 * 1024 * 1024, // 10MB limit
  },
  fileFilter: (req, file, cb) => {
    if (file.mimetype.startsWith('image/')) {
      cb(null, true);
    } else {
      cb(new Error('Only image files are allowed'));
    }
  },
});

/**
 * Create Upload routes with injected service
 */
export function createUploadRoutes(documentService: DocumentService): Router {
  const router = Router();

  // POST /api/upload - Upload image to document
  router.post('/', upload.single('image'), async (req: Request, res: Response) => {
    try {
      const documentId = req.query.documentId as string;
      
      if (!documentId) {
        return res.status(400).json({ error: 'documentId is required' });
      }
      
      if (!req.file) {
        return res.status(400).json({ error: 'No image file provided' });
      }

      const url = await documentService.saveImage(
        documentId,
        req.file.originalname,
        req.file.buffer
      );
      
      res.json({ url });
    } catch (error) {
      console.error('Upload error:', error);
      res.status(500).json({ error: 'Failed to upload image' });
    }
  });

  return router;
}

