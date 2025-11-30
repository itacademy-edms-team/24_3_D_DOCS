import { Router, Request, Response } from 'express';
import type { DocumentService } from '../../application/services/DocumentService';

/**
 * Create Document routes with injected service
 */
export function createDocumentRoutes(documentService: DocumentService): Router {
  const router = Router();

  // GET /api/documents - Get all documents
  router.get('/', async (req: Request, res: Response) => {
    try {
      const documents = await documentService.getAllDocuments();
      res.json(documents);
    } catch (error) {
      console.error('Failed to get documents:', error);
      res.status(500).json({ error: 'Failed to get documents' });
    }
  });

  // GET /api/documents/:id - Get document by ID
  router.get('/:id', async (req: Request, res: Response) => {
    try {
      const document = await documentService.getDocumentById(req.params.id);
      
      if (!document) {
        return res.status(404).json({ error: 'Document not found' });
      }
      
      res.json(document);
    } catch (error) {
      console.error('Failed to get document:', error);
      res.status(500).json({ error: 'Failed to get document' });
    }
  });

  // POST /api/documents - Create new document
  router.post('/', async (req: Request, res: Response) => {
    try {
      const { name, profileId, content } = req.body;
      const document = await documentService.createDocument({ name, profileId, content });
      res.status(201).json(document);
    } catch (error) {
      console.error('Failed to create document:', error);
      res.status(500).json({ error: 'Failed to create document' });
    }
  });

  // PUT /api/documents/:id - Update document
  router.put('/:id', async (req: Request, res: Response) => {
    try {
      const { name, profileId, content, overrides } = req.body;
      const document = await documentService.updateDocument(req.params.id, { 
        name, 
        profileId, 
        content, 
        overrides 
      });
      
      if (!document) {
        return res.status(404).json({ error: 'Document not found' });
      }
      
      res.json(document);
    } catch (error) {
      console.error('Failed to update document:', error);
      res.status(500).json({ error: 'Failed to update document' });
    }
  });

  // DELETE /api/documents/:id - Delete document
  router.delete('/:id', async (req: Request, res: Response) => {
    try {
      const deleted = await documentService.deleteDocument(req.params.id);
      
      if (!deleted) {
        return res.status(404).json({ error: 'Document not found' });
      }
      
      res.json({ success: true });
    } catch (error) {
      console.error('Failed to delete document:', error);
      res.status(500).json({ error: 'Failed to delete document' });
    }
  });

  // POST /api/documents/:id/pdf - Generate PDF
  router.post('/:id/pdf', async (req: Request, res: Response) => {
    try {
      console.log('PDF generation started for:', req.params.id);
      
      const document = await documentService.getDocumentById(req.params.id);
      
      if (!document) {
        console.log('Document not found:', req.params.id);
        return res.status(404).json({ error: 'Document not found' });
      }
      
      console.log('Generating PDF...');
      const pdfBuffer = await documentService.generatePdf(req.params.id);
      console.log('PDF generated, size:', pdfBuffer.length);
      
      res.setHeader('Content-Type', 'application/pdf');
      const filename = encodeURIComponent(document.name) + '.pdf';
      res.setHeader('Content-Disposition', `attachment; filename*=UTF-8''${filename}`);
      res.send(pdfBuffer);
    } catch (error) {
      console.error('PDF generation error:', error);
      res.status(500).json({ error: 'Failed to generate PDF', details: String(error) });
    }
  });

  return router;
}

