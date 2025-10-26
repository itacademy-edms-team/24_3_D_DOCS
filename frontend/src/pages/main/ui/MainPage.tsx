import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@entities';
import { Meta } from '@ui';
import { documentLinksAPI, schemaLinksAPI, type DocumentLinkDTO, type SchemaLinkDTO, type ContentType } from '@entities';
import style from './MainPage.module.css';

const MainPage = (): React.JSX.Element => {
  const navigate = useNavigate();
  const { logout, user } = useAuthStore();
  
  // Состояние для переключения между документами и шаблонами
  const [contentType, setContentType] = useState<ContentType>('documents');
  const [documents, setDocuments] = useState<DocumentLinkDTO[]>([]);
  const [templates, setTemplates] = useState<SchemaLinkDTO[]>([]);
  const [selectedDocument, setSelectedDocument] = useState<DocumentLinkDTO | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<SchemaLinkDTO | null>(null);
  const [markdownContent, setMarkdownContent] = useState('# Заголовок документа\n\nНачните печатать ваш документ здесь...');
  const [templateContent, setTemplateContent] = useState('\\documentclass{article}\n\\begin{document}\n\n\\end{document}');
  const [loading, setLoading] = useState(false);

  // Состояние для модальных окон
  const [showSaveModal, setShowSaveModal] = useState(false);
  const [showConvertModal, setShowConvertModal] = useState(false);
  const [showDownloadPdfModal, setShowDownloadPdfModal] = useState(false);
  const [showDeleteDocumentModal, setShowDeleteDocumentModal] = useState(false);
  const [showCreateTemplateModal, setShowCreateTemplateModal] = useState(false);
  const [showDeleteTemplateModal, setShowDeleteTemplateModal] = useState(false);
  const [newDocumentName, setNewDocumentName] = useState('');
  const [newTemplateName, setNewTemplateName] = useState('');
  const [editableTitle, setEditableTitle] = useState('Новый документ');
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [documentToDelete, setDocumentToDelete] = useState<DocumentLinkDTO | null>(null);
  const [templateToDelete, setTemplateToDelete] = useState<SchemaLinkDTO | null>(null);

  // Загрузка документов
  const loadDocuments = async () => {
    try {
      setLoading(true);
      const docs = await documentLinksAPI.getDocuments();
      setDocuments(docs);
    } catch (error) {
      console.error('Failed to load documents:', error);
    } finally {
      setLoading(false);
    }
  };

  // Загрузка шаблонов
  const loadTemplates = async () => {
    try {
      setLoading(true);
      const tmpls = await schemaLinksAPI.getSchemas();
      setTemplates(tmpls);
    } catch (error) {
      console.error('Failed to load templates:', error);
    } finally {
      setLoading(false);
    }
  };

  // Загрузка данных при монтировании компонента
  useEffect(() => {
    loadDocuments();
    loadTemplates();
  }, []);

  // Переключение типа контента
  const handleContentTypeChange = (type: ContentType) => {
    setContentType(type);
    setSelectedDocument(null);
    setSelectedTemplate(null);
  };

  // Выбор документа
  const handleDocumentSelect = async (document: DocumentLinkDTO) => {
    setSelectedDocument(document);
    setSelectedTemplate(null);
    setEditableTitle(document.name || 'Без названия');
    try {
      const blob = await documentLinksAPI.downloadDocument(document.id);
      const text = await blob.text();
      setMarkdownContent(text);
    } catch (error) {
      console.error('Failed to load document content:', error);
    }
  };

  // Выбор шаблона
  const handleTemplateSelect = async (template: SchemaLinkDTO) => {
    setSelectedTemplate(template);
    setSelectedDocument(null);
    setEditableTitle(template.name || 'Без названия');
    try {
      const blob = await schemaLinksAPI.downloadSchema(template.id);
      const text = await blob.text();
      setTemplateContent(text);
    } catch (error) {
      console.error('Failed to load template content:', error);
    }
  };

  // Создание нового документа
  const handleCreateDocument = async () => {
    setNewDocumentName('');
    setShowSaveModal(true);
  };

  // Сохранение нового документа
  const handleSaveNewDocument = async () => {
    if (!newDocumentName.trim()) return;

    try {
      const formData = new FormData();
      formData.append('Name', newDocumentName.trim());
      formData.append('Description', '');
      formData.append('file', new Blob(['# ' + newDocumentName.trim() + '\n\nНачните печатать ваш документ здесь...'], { type: 'text/markdown' }), newDocumentName.trim() + '.md');

      const newDoc = await documentLinksAPI.createDocument(formData);
      setDocuments(prev => [newDoc, ...prev]);
      setSelectedDocument(newDoc);
      setEditableTitle(newDoc.name || 'Без названия');
      setMarkdownContent('# ' + newDoc.name + '\n\nНачните печатать ваш документ здесь...');
      setShowSaveModal(false);
    } catch (error) {
      console.error('Failed to create document:', error);
    }
  };

  // Создание нового шаблона
  const handleCreateTemplate = async () => {
    setNewTemplateName('');
    setShowCreateTemplateModal(true);
  };

  // Сохранение нового шаблона
  const handleSaveNewTemplate = async () => {
    if (!newTemplateName.trim()) return;

    try {
      const formData = new FormData();
      formData.append('Name', newTemplateName.trim());
      formData.append('Description', '');
      formData.append('IsPublic', 'false');
      formData.append('file', new Blob(['\\documentclass{article}\n\\begin{document}\n\n\\title{' + newTemplateName.trim() + '}\n\\author{}\n\\date{\\today}\n\n\\maketitle\n\n\\end{document}'], { type: 'text/plain' }), newTemplateName.trim() + '.tex');

      const newTemplate = await schemaLinksAPI.createSchema(formData);
      setTemplates(prev => [newTemplate, ...prev]);
      setSelectedTemplate(newTemplate);
      setEditableTitle(newTemplate.name || 'Без названия');
      setTemplateContent('\\documentclass{article}\n\\begin{document}\n\n\\title{' + newTemplate.name + '}\n\\author{}\n\\date{\\today}\n\n\\maketitle\n\n\\end{document}');
      setShowCreateTemplateModal(false);
    } catch (error) {
      console.error('Failed to create template:', error);
    }
  };

  // Сохранение документа
  const handleSaveDocument = async () => {
    if (!selectedDocument) {
      // Если нет выбранного документа, показываем модальное окно для создания нового
      setNewDocumentName(editableTitle);
      setShowSaveModal(true);
      return;
    }

    try {
      const formData = new FormData();
      formData.append('Name', editableTitle);
      formData.append('Description', selectedDocument.description || '');
      formData.append('file', new Blob([markdownContent], { type: 'text/markdown' }), editableTitle + '.md');

      // Обновляем существующий документ
      await documentLinksAPI.deleteDocument(selectedDocument.id);
      const updatedDoc = await documentLinksAPI.createDocument(formData);
      
      setDocuments(prev => prev.map(doc => doc.id === selectedDocument.id ? updatedDoc : doc));
      setSelectedDocument(updatedDoc);
    } catch (error) {
      console.error('Failed to save document:', error);
    }
  };

  // Сохранение шаблона
  const handleSaveTemplate = async () => {
    if (!selectedTemplate) return;

    try {
      const formData = new FormData();
      formData.append('Name', selectedTemplate.name || '');
      formData.append('Description', selectedTemplate.description || '');
      formData.append('IsPublic', selectedTemplate.isPublic.toString());
      formData.append('file', new Blob([templateContent], { type: 'text/plain' }), selectedTemplate.name + '.tex');

      // Обновляем существующий шаблон
      await schemaLinksAPI.deleteSchema(selectedTemplate.id);
      const updatedTemplate = await schemaLinksAPI.createSchema(formData);
      
      setTemplates(prev => prev.map(tmpl => tmpl.id === selectedTemplate.id ? updatedTemplate : tmpl));
      setSelectedTemplate(updatedTemplate);
    } catch (error) {
      console.error('Failed to save template:', error);
    }
  };

  // Скачивание файла
  const handleDownloadFile = async () => {
    if (contentType === 'documents' && selectedDocument) {
      try {
        const blob = await documentLinksAPI.downloadDocument(selectedDocument.id);
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = (selectedDocument.name || 'document') + '.md';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      } catch (error) {
        console.error('Failed to download document:', error);
      }
    } else if (contentType === 'templates' && selectedTemplate) {
      try {
        const blob = await schemaLinksAPI.downloadSchema(selectedTemplate.id);
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = (selectedTemplate.name || 'template') + '.tex';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      } catch (error) {
        console.error('Failed to download template:', error);
      }
    }
  };

  // Удаление документа
  const handleDeleteDocument = async (document: DocumentLinkDTO) => {
    setDocumentToDelete(document);
    setShowDeleteDocumentModal(true);
  };

  // Подтверждение удаления документа
  const handleConfirmDeleteDocument = async () => {
    if (!documentToDelete) return;

    try {
      await documentLinksAPI.deleteDocument(documentToDelete.id);
      setDocuments(prev => prev.filter(doc => doc.id !== documentToDelete.id));
      
      if (selectedDocument?.id === documentToDelete.id) {
        setSelectedDocument(null);
        setMarkdownContent('# Заголовок документа\n\nНачните печатать ваш документ здесь...');
        setEditableTitle('Новый документ');
      }
      setShowDeleteDocumentModal(false);
      setDocumentToDelete(null);
    } catch (error) {
      console.error('Failed to delete document:', error);
    }
  };

  // Удаление шаблона
  const handleDeleteTemplate = async (template: SchemaLinkDTO) => {
    setTemplateToDelete(template);
    setShowDeleteTemplateModal(true);
  };

  // Подтверждение удаления шаблона
  const handleConfirmDeleteTemplate = async () => {
    if (!templateToDelete) return;

    try {
      await schemaLinksAPI.deleteSchema(templateToDelete.id);
      setTemplates(prev => prev.filter(tmpl => tmpl.id !== templateToDelete.id));
      
      if (selectedTemplate?.id === templateToDelete.id) {
        setSelectedTemplate(null);
        setTemplateContent('\\documentclass{article}\n\\begin{document}\n\n\\end{document}');
        setEditableTitle('Новый шаблон');
      }
      setShowDeleteTemplateModal(false);
      setTemplateToDelete(null);
    } catch (error) {
      console.error('Failed to delete template:', error);
    }
  };

  // Конвертация документа
  const handleConvertDocument = async () => {
    if (!selectedDocument) {
      // Если нет выбранного документа, сначала нужно сохранить
      setNewDocumentName(editableTitle);
      setShowSaveModal(true);
      return;
    }
    
    setShowConvertModal(true);
  };

  // Скачивание PDF
  const handleDownloadPdf = async () => {
    if (!selectedDocument) return;
    
    setShowDownloadPdfModal(true);
  };

  // Обработка изменения заголовка
  const handleTitleChange = (newTitle: string) => {
    setEditableTitle(newTitle);
  };

  // Обработка сохранения заголовка
  const handleTitleSave = () => {
    setIsEditingTitle(false);
  };

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/');
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <>
      <Meta title="Dashboard | DDOCS" lang="ru" description="Панель управления документами DDOCS" />
      <div className={style.container}>
        {/* Левая панель - Список схем/документов */}
        <aside className={style.sidebar}>
          <div className={style.sidebarHeader}>
            <div className={style.sidebarTop}>
              <h2 className={style.sidebarTitle}>
                {contentType === 'documents' ? 'Документы' : 'Шаблоны'}
              </h2>
              <button 
                className={style.newDocButton} 
                type="button"
                onClick={contentType === 'documents' ? handleCreateDocument : handleCreateTemplate}
              >
                + Создать
              </button>
            </div>
            
            {/* Переключатель Документы/Шаблоны */}
            <div className={style.contentTypeSwitcher}>
              <button
                className={`${style.switcherButton} ${contentType === 'documents' ? style.active : ''}`}
                onClick={() => handleContentTypeChange('documents')}
                type="button"
              >
                📄 Документы
              </button>
              <button
                className={`${style.switcherButton} ${contentType === 'templates' ? style.active : ''}`}
                onClick={() => handleContentTypeChange('templates')}
                type="button"
              >
                📋 Шаблоны
              </button>
            </div>
          </div>
          
          <div className={style.documentList}>
            {loading ? (
              <div className={style.loadingPlaceholder}>Загрузка...</div>
            ) : contentType === 'documents' ? (
              documents.length === 0 ? (
                <div className={style.emptyPlaceholder}>Нет документов</div>
              ) : (
                documents.map((doc) => (
                  <div 
                    key={doc.id}
                    className={`${style.documentItem} ${selectedDocument?.id === doc.id ? style.active : ''}`}
                    onClick={() => handleDocumentSelect(doc)}
                  >
                    <div className={style.documentIcon}>📄</div>
                    <div className={style.documentInfo}>
                      <span className={style.documentName}>{doc.name || 'Без названия'}</span>
                      <span className={style.documentDate}>
                        {new Date(doc.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    <button
                      className={style.deleteButton}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteDocument(doc);
                      }}
                      type="button"
                      title="Удалить документ"
                    >
                      🗑
                    </button>
                  </div>
                ))
              )
            ) : (
              templates.length === 0 ? (
                <div className={style.emptyPlaceholder}>Нет шаблонов</div>
              ) : (
                templates.map((template) => (
                  <div 
                    key={template.id}
                    className={`${style.documentItem} ${selectedTemplate?.id === template.id ? style.active : ''}`}
                    onClick={() => handleTemplateSelect(template)}
                  >
                    <div className={style.documentIcon}>📋</div>
                    <div className={style.documentInfo}>
                      <span className={style.documentName}>{template.name || 'Без названия'}</span>
                      <span className={style.documentDate}>
                        {new Date(template.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    <button
                      className={style.deleteButton}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteTemplate(template);
                      }}
                      type="button"
                      title="Удалить шаблон"
                    >
                      🗑
                    </button>
                  </div>
                ))
              )
            )}
          </div>
          <div className={style.sidebarFooter}>
            <div className={style.userInfo}>
              <div className={style.userAvatar}>👤</div>
              <div className={style.userDetails}>
                <span className={style.userName}>{user?.name || 'Пользователь'}</span>
                <span className={style.userEmail}>{user?.email}</span>
              </div>
            </div>
            <button className={style.logoutButton} onClick={handleLogout} type="button">
              🚪 Выход
            </button>
          </div>
        </aside>

        {/* Центральная часть - Markdown редактор */}
        <main className={style.editor}>
          <div className={style.editorHeader}>
            <div className={style.titleContainer}>
              {isEditingTitle ? (
                <input
                  type="text"
                  value={editableTitle}
                  onChange={(e) => handleTitleChange(e.target.value)}
                  onBlur={handleTitleSave}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleTitleSave();
                    if (e.key === 'Escape') setIsEditingTitle(false);
                  }}
                  className={style.titleInput}
                  autoFocus
                />
              ) : (
                <h3 
                  className={style.editorTitle}
                  onClick={() => setIsEditingTitle(true)}
                  title="Нажмите для редактирования"
                >
                  {editableTitle}
                </h3>
              )}
            </div>
            <div className={style.editorActions}>
              <button 
                className={style.actionButton} 
                type="button"
                onClick={contentType === 'documents' ? handleSaveDocument : handleSaveTemplate}
              >
                💾 Сохранить
              </button>
              {contentType === 'documents' && (
                <>
                  <button 
                    className={style.actionButton} 
                    type="button"
                    onClick={handleConvertDocument}
                  >
                    🔄 Преобразовать
                  </button>
                  <button 
                    className={style.actionButton} 
                    type="button"
                    onClick={handleDownloadPdf}
                    disabled={!selectedDocument}
                  >
                    📄 Скачать PDF
                  </button>
                </>
              )}
              <button 
                className={style.actionButton} 
                type="button"
                onClick={handleDownloadFile}
                disabled={contentType === 'documents' ? !selectedDocument : !selectedTemplate}
              >
                📤 Скачать MD
              </button>
            </div>
          </div>
          <textarea
            className={style.markdownArea}
            value={contentType === 'documents' ? markdownContent : templateContent}
            onChange={(e) => {
              if (contentType === 'documents') {
                setMarkdownContent(e.target.value);
              } else {
                setTemplateContent(e.target.value);
              }
            }}
            placeholder={contentType === 'documents' 
              ? "Начните печатать markdown..." 
              : "Начните печатать LaTeX..."
            }
          />
        </main>

        {/* Правая панель - Превью PDF */}
        <aside className={style.preview}>
          <div className={style.previewHeader}>
            <h3 className={style.previewTitle}>Превью PDF</h3>
          </div>
          <div className={style.previewContent}>
            <div className={style.previewPlaceholder}>
              <div className={style.previewIcon}>📋</div>
              <p>Здесь будет отображаться<br />PDF-превью документа</p>
            </div>
          </div>
        </aside>
      </div>

      {/* Модальное окно сохранения документа */}
      {showSaveModal && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Сохранить документ</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowSaveModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <label htmlFor="documentName">Название документа:</label>
              <input
                id="documentName"
                type="text"
                value={newDocumentName}
                onChange={(e) => setNewDocumentName(e.target.value)}
                placeholder="Введите название документа"
                className={style.modalInput}
                autoFocus
              />
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowSaveModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonPrimary}`}
                onClick={handleSaveNewDocument}
                disabled={!newDocumentName.trim()}
                type="button"
              >
                Сохранить
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Модальное окно конвертации */}
      {showConvertModal && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Преобразовать в PDF</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowConvertModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <label htmlFor="templateSelect">Выберите шаблон:</label>
              <select id="templateSelect" className={style.modalSelect}>
                <option value="">Выберите шаблон...</option>
                {templates.map(template => (
                  <option key={template.id} value={template.id}>
                    {template.name || 'Без названия'}
                  </option>
                ))}
              </select>
              {templates.length === 0 && (
                <p className={style.modalHint}>Нет доступных шаблонов. Создайте шаблон в разделе "Шаблоны".</p>
              )}
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowConvertModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonPrimary}`}
                onClick={() => {
                  // TODO: Реализовать конвертацию через Pandoc
                  alert('Конвертация будет реализована позже с Pandoc');
                  setShowConvertModal(false);
                }}
                type="button"
              >
                Преобразовать
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Модальное окно скачивания PDF */}
      {showDownloadPdfModal && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Скачать PDF</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowDownloadPdfModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <p>Скачать PDF версию документа "{editableTitle}"?</p>
              <p className={style.modalHint}>PDF будет сгенерирован из текущего содержимого документа.</p>
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowDownloadPdfModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonPrimary}`}
                onClick={() => {
                  // TODO: Реализовать скачивание PDF
                  alert('Скачивание PDF будет реализовано позже');
                  setShowDownloadPdfModal(false);
                }}
                type="button"
              >
                Скачать PDF
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Модальное окно удаления документа */}
      {showDeleteDocumentModal && documentToDelete && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Удалить документ</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowDeleteDocumentModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <p>Вы уверены, что хотите удалить документ <strong>"{documentToDelete.name}"</strong>?</p>
              <p className={style.modalHint}>Это действие нельзя отменить. Документ будет удален из хранилища и базы данных.</p>
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowDeleteDocumentModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonDanger}`}
                onClick={handleConfirmDeleteDocument}
                type="button"
              >
                Удалить
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Модальное окно создания шаблона */}
      {showCreateTemplateModal && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Создать шаблон</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowCreateTemplateModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <label htmlFor="templateName">Название шаблона:</label>
              <input
                id="templateName"
                type="text"
                value={newTemplateName}
                onChange={(e) => setNewTemplateName(e.target.value)}
                placeholder="Введите название шаблона"
                className={style.modalInput}
                autoFocus
              />
              <p className={style.modalHint}>Будет создан новый LaTeX шаблон с базовой структурой.</p>
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowCreateTemplateModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonPrimary}`}
                onClick={handleSaveNewTemplate}
                disabled={!newTemplateName.trim()}
                type="button"
              >
                Создать
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Модальное окно удаления шаблона */}
      {showDeleteTemplateModal && templateToDelete && (
        <div className={style.modalOverlay}>
          <div className={style.modal}>
            <div className={style.modalHeader}>
              <h3>Удалить шаблон</h3>
              <button 
                className={style.modalClose}
                onClick={() => setShowDeleteTemplateModal(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <div className={style.modalBody}>
              <p>Вы уверены, что хотите удалить шаблон <strong>"{templateToDelete.name}"</strong>?</p>
              <p className={style.modalHint}>Это действие нельзя отменить. Шаблон будет удален из хранилища и базы данных.</p>
            </div>
            <div className={style.modalFooter}>
              <button 
                className={style.modalButton}
                onClick={() => setShowDeleteTemplateModal(false)}
                type="button"
              >
                Отмена
              </button>
              <button 
                className={`${style.modalButton} ${style.modalButtonDanger}`}
                onClick={handleConfirmDeleteTemplate}
                type="button"
              >
                Удалить
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

export default MainPage;
