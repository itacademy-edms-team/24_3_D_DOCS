import { Router, Request, Response } from 'express';
import type { TitlePageService } from '../../application/services/TitlePageService';

/**
 * Create TitlePage routes with injected service
 */
export function createTitlePageRoutes(titlePageService: TitlePageService): Router {
  const router = Router();

  // GET /api/title-pages - Get all title pages
  router.get('/', async (req: Request, res: Response) => {
    try {
      const titlePages = await titlePageService.getAllTitlePages();
      res.json(titlePages);
    } catch (error) {
      console.error('Failed to get title pages:', error);
      res.status(500).json({ error: 'Failed to get title pages' });
    }
  });

  // GET /api/title-pages/:id - Get title page by ID
  router.get('/:id', async (req: Request, res: Response) => {
    try {
      const titlePage = await titlePageService.getTitlePageById(req.params.id);
      
      if (!titlePage) {
        return res.status(404).json({ error: 'Title page not found' });
      }
      
      res.json(titlePage);
    } catch (error) {
      console.error('Failed to get title page:', error);
      res.status(500).json({ error: 'Failed to get title page' });
    }
  });

  // POST /api/title-pages - Create new title page
  router.post('/', async (req: Request, res: Response) => {
    try {
      const { name } = req.body;
      const titlePage = await titlePageService.createTitlePage({ name });
      res.status(201).json(titlePage);
    } catch (error) {
      console.error('Failed to create title page:', error);
      res.status(500).json({ error: 'Failed to create title page' });
    }
  });

  // PUT /api/title-pages/:id - Update title page
  router.put('/:id', async (req: Request, res: Response) => {
    try {
      const { name, elements, variables } = req.body;
      const titlePage = await titlePageService.updateTitlePage(req.params.id, { 
        name, 
        elements, 
        variables 
      });
      
      if (!titlePage) {
        return res.status(404).json({ error: 'Title page not found' });
      }
      
      res.json(titlePage);
    } catch (error) {
      console.error('Failed to update title page:', error);
      res.status(500).json({ error: 'Failed to update title page' });
    }
  });

  // DELETE /api/title-pages/:id - Delete title page
  router.delete('/:id', async (req: Request, res: Response) => {
    try {
      const deleted = await titlePageService.deleteTitlePage(req.params.id);
      
      if (!deleted) {
        return res.status(404).json({ error: 'Title page not found' });
      }
      
      res.json({ success: true });
    } catch (error) {
      console.error('Failed to delete title page:', error);
      res.status(500).json({ error: 'Failed to delete title page' });
    }
  });

  // POST /api/title-pages/:id/pdf - Generate PDF
  router.post('/:id/pdf', async (req: Request, res: Response) => {
    try {
      console.log('PDF generation started for title page:', req.params.id);
      
      const titlePage = await titlePageService.getTitlePageById(req.params.id);
      
      if (!titlePage) {
        console.log('Title page not found:', req.params.id);
        return res.status(404).json({ error: 'Title page not found' });
      }
      
      console.log('Generating PDF...');
      const pdfBuffer = await titlePageService.generatePdf(req.params.id);
      console.log('PDF generated, size:', pdfBuffer.length);
      
      res.setHeader('Content-Type', 'application/pdf');
      const filename = encodeURIComponent(titlePage.name) + '.pdf';
      res.setHeader('Content-Disposition', `attachment; filename*=UTF-8''${filename}`);
      res.send(pdfBuffer);
    } catch (error) {
      console.error('PDF generation error:', error);
      res.status(500).json({ error: 'Failed to generate PDF', details: String(error) });
    }
  });

  return router;
}

