import express from 'express';
import cors from 'cors';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Infrastructure
import { FileProfileRepository } from './infrastructure/persistence/FileProfileRepository';
import { FileDocumentRepository } from './infrastructure/persistence/FileDocumentRepository';
import { PdfGenerator } from './infrastructure/pdf/PdfGenerator';

// Application Services
import { ProfileService } from './application/services/ProfileService';
import { DocumentService } from './application/services/DocumentService';

// Routes
import { createProfileRoutes, createDocumentRoutes, createUploadRoutes } from './presentation/routes';

// Configuration
const PORT = process.env.PORT || 3001;
const DATA_DIR = path.join(__dirname, '../../data');

// Create Express app
const app = express();

// Middleware
app.use(cors());
app.use(express.json());

// Static files for uploaded images
app.use('/uploads', express.static(path.join(DATA_DIR, 'documents')));

// ==================== Dependency Injection ====================

// Repositories
const profileRepository = new FileProfileRepository(DATA_DIR);
const documentRepository = new FileDocumentRepository(DATA_DIR);

// PDF Generator
const pdfGenerator = new PdfGenerator();

// Application Services
const profileService = new ProfileService(profileRepository);
const documentService = new DocumentService(documentRepository, profileRepository, pdfGenerator);

// ==================== Routes ====================

app.use('/api/profiles', createProfileRoutes(profileService));
app.use('/api/documents', createDocumentRoutes(documentService));
app.use('/api/upload', createUploadRoutes(documentService));

// Health check
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// ==================== Start Server ====================

app.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`);
  console.log(`Data directory: ${DATA_DIR}`);
});

