import { BrowserRouter, Routes, Route } from 'react-router-dom';
import {
  HomePage,
  ProfileEditorPage,
  DocumentEditorPage,
  DocumentCustomizerPage,
  TitlePageEditorPage,
} from './presentation/pages';

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/profile/:id" element={<ProfileEditorPage />} />
        <Route path="/document/:id/edit" element={<DocumentEditorPage />} />
        <Route path="/document/:id/customize" element={<DocumentCustomizerPage />} />
        <Route path="/title-page/:id" element={<TitlePageEditorPage />} />
      </Routes>
    </BrowserRouter>
  );
}

