# DDOCS - Система конвертации документов

Полнофункциональная система для создания, управления и конвертации документов из Markdown в PDF с использованием современных технологий.

## 🚀 Быстрый старт

```bash
# Запуск всего проекта
docker compose up --build -d

# Остановка
docker compose down

# Остановка с очисткой данных
docker compose down -v
```

## 📦 Архитектура системы

> **Примечание**: Backend представляет собой монолитное ASP.NET Core приложение с модульной структурой, использующее внешние сервисы (PostgreSQL, Redis, MinIO, Pandoc) для хранилища данных, кэширования и конвертации.

| Компонент | URL | Описание |
|--------|-----|----------|
| **Frontend** | http://localhost:3000 | React приложение с современным UI |
| **Backend API** | http://localhost:5159 | ASP.NET Core 9.0 Web API (монолитный) |
| **Swagger UI** | http://localhost:5159/swagger | Интерактивная API документация |
| **PostgreSQL** | `localhost:5432` | Основная база данных |
| **pgAdmin** | http://localhost:5050 | Веб-интерфейс для PostgreSQL |
| **Redis** | `localhost:6379` | Кэширование и управление токенами |
| **Redis Commander** | http://localhost:8081 | Веб-интерфейс для Redis |
| **MinIO** | http://localhost:9000 | S3-совместимое API |
| **Pandoc** | Container | Сервис конвертации Markdown → PDF |

### Технологический стек

#### Backend (ASP.NET Core 9.0)
- **Аутентификация**: JWT с Refresh Token rotation
- **База данных**: PostgreSQL с Entity Framework Core
- **Кэширование**: Redis для токенов и сессий
- **Файловое хранилище**: MinIO (S3-совместимое)
- **Конвертация**: Pandoc + XeLaTeX для PDF генерации
- **Email**: MailKit для отправки кодов верификации
- **Безопасность**: BCrypt для хеширования паролей

#### Frontend (React + TypeScript)
- **Фреймворк**: React 18 с TypeScript
- **Сборщик**: Rsbuild (современная альтернатива Webpack)
- **Роутинг**: React Router v7
- **Состояние**: Zustand для управления состоянием
- **HTTP клиент**: Axios с перехватчиками
- **Валидация**: Zod для схем валидации
- **Стили**: CSS Modules с BEM методологией
- **Линтинг**: Biome для форматирования и проверки кода

#### Инфраструктура
- **Контейнеризация**: Docker Compose
- **Мониторинг**: Health checks для всех компонентов
- **Масштабирование**: Горизонтальное масштабирование готово через Docker Compose

## 🔑 Учетные данные

### PostgreSQL
- **User**: `rusal_user`
- **Password**: `rusal_password`
- **Database**: `rusal_db`

### pgAdmin
- **URL**: http://localhost:5050
- **Email**: `admin@admin.com`
- **Password**: `admin`

**Настройка подключения к PostgreSQL:**
1. Вкладка **General**: Name → `RusalProject DB`
2. Вкладка **Connection**: 
   - Host: `postgres`
   - Port: `5432`
   - Database: `rusal_db`
   - Username: `rusal_user`
   - Password: `rusal_password`
3. Включите **Save password**

### MinIO
- **Access Key**: `minioadmin`
- **Secret Key**: `minioadmin123`
- **Console**: http://localhost:9001

### Redis
- **Connection**: `localhost:6379`
- **No authentication** (локальная разработка)

### Redis Commander
- **URL**: http://localhost:8081
- **No authentication** (локальная разработка)

## 📁 Структура проекта

```
.
├── backend/                             # ASP.NET Core Web API
│   ├── Controllers/                     # API endpoints
│   │   ├── AuthController.cs            # Аутентификация и авторизация
│   │   ├── DocumentLinksController.cs   # Управление документами
│   │   └── SchemaLinksController.cs     # Управление шаблонами
│   ├── Services/                        # Бизнес-логика
│   │   ├── Auth/                        # JWT, хеширование, email
│   │   ├── Email/                       # Отправка email
│   │   ├── Pandoc/                      # Конвертация документов
│   │   └── Storage/                     # MinIO интеграция
│   ├── Models/                          # DTOs и Entities
│   │   ├── DTOs/                        # Data Transfer Objects
│   │   └── Entities/                    # Database entities
│   ├── Provider/                        # Внешние сервисы
│   │   ├── Database/                    # EF Core контекст
│   │   └── Redis/                       # Redis клиент
│   └── Migrations/                      # EF Core миграции
├── frontend/                             # React приложение
│   ├── src/
│   │   ├── app/                         # Конфигурация приложения
│   │   │   ├── layouts/                 # Макеты страниц
│   │   │   ├── providers/               # Провайдеры (Auth, ProtectedRoute)
│   │   │   └── routes.tsx               # Маршрутизация
│   │   ├── pages/                       # Страницы приложения
│   │   │   ├── auth/                    # Страница аутентификации
│   │   │   └── main/                    # Главная страница
│   │   ├── features/                    # Функциональные модули
│   │   │   ├── auth/                    # Логин, регистрация, выход
│   │   │   └── documents/               # Управление документами
│   │   ├── entities/                    # Бизнес-сущности
│   │   │   └── auth/                    # API и модели аутентификации
│   │   ├── shared/                      # Общие компоненты
│   │   │   ├── ui/                      # UI компоненты
│   │   │   ├── api/                     # HTTP клиент
│   │   │   └── lib/                     # Утилиты
│   │   └── widgets/                     # Виджеты
│   ├── rsbuild.config.ts                # Конфигурация сборки
│   └── package.json                     # Зависимости
├── pandoc/                               # Docker образ для Pandoc
│   └── Dockerfile                       # LaTeX + Pandoc + шрифты
├── infra/                                # Инфраструктура
│   └── terraform/                       # Terraform конфигурации
├── docker-compose.yml                    # Оркестрация сервисов
└── README.md                             # Документация
```

## 🛠️ Разработка

### Локальная разработка Backend

```bash
cd backend

# Восстановление зависимостей
dotnet restore

# Запуск в режиме разработки
dotnet run

# Применение миграций
dotnet ef database update
```

### Локальная разработка Frontend

```bash
cd frontend

# Установка зависимостей
pnpm install

# Запуск в режиме разработки
pnpm dev

# Сборка для продакшена
pnpm build

# Линтинг и форматирование
pnpm biome:check
pnpm biome:format
```

### Docker разработка

```bash
# Сборка только backend
docker compose build backend

# Перезапуск конкретного сервиса
docker compose restart backend

# Просмотр логов
docker compose logs -f backend

# Выполнение команд в контейнере
docker compose exec backend dotnet ef migrations add NewMigration
```

## ✅ Функциональность

### 🔐 Аутентификация и авторизация
- ✅ **JWT аутентификация** с Access и Refresh токенами
- ✅ **Email верификация** при регистрации
- ✅ **Token rotation** для безопасности
- ✅ **Выход со всех устройств**
- ✅ **Защищенные маршруты** на фронтенде
- ✅ **Автоматическое обновление токенов**

### 📄 Управление документами
- ✅ **Загрузка Markdown файлов** в MinIO
- ✅ **Создание и редактирование** документов
- ✅ **Скачивание исходных файлов** (MD)
- ✅ **Управление метаданными** (название, описание)
- ✅ **Статусы документов** (draft, processing, completed, failed)

### 🎨 Система шаблонов
- ✅ **Создание LaTeX шаблонов** для PDF
- ✅ **Публичные и приватные** шаблоны
- ✅ **Валидация шаблонов** перед использованием
- ✅ **Управление шаблонами** через API

### 🔄 Конвертация документов
- ✅ **Markdown → PDF** через Pandoc + XeLaTeX
- ✅ **Русская локализация** (texlive-lang-cyrillic)
- ✅ **Асинхронная обработка** с отслеживанием статуса
- ✅ **Логирование ошибок** конвертации

### 🗄️ Хранение и кэширование
- ✅ **MinIO S3-совместимое** хранилище
- ✅ **Redis кэширование** токенов
- ✅ **Автоматическая очистка** временных файлов
- ✅ **Health checks** для всех сервисов

### 🎨 Пользовательский интерфейс
- ✅ **Современный Glassmorphism** дизайн
- ✅ **Адаптивная верстка** для всех устройств
- ✅ **Темная тема** по умолчанию
- ✅ **Интуитивная навигация**
- ✅ **Обработка ошибок** с понятными сообщениями

## 🔧 API Endpoints

### Аутентификация (`/api/auth`)
- `POST /send-verification` - Отправка кода верификации
- `POST /register` - Регистрация с верификацией email
- `POST /login` - Вход в систему
- `POST /refresh` - Обновление токенов
- `POST /logout` - Выход из системы
- `POST /logout-all` - Выход со всех устройств
- `GET /me` - Информация о текущем пользователе

### Документы (`/api/documentlinks`)
- `GET /` - Список документов пользователя
- `GET /{id}` - Получение документа по ID
- `POST /` - Создание нового документа
- `POST /{id}/convert` - Конвертация в PDF
- `GET /{id}/download` - Скачивание Markdown файла
- `GET /{id}/pdf` - Скачивание PDF файла
- `DELETE /{id}` - Удаление документа
- `POST /test-pandoc` - Тестирование Pandoc (для разработки)

### Шаблоны (`/api/schemalinks`)
- `GET /` - Список доступных шаблонов
- `GET /{id}` - Получение шаблона по ID
- `POST /` - Создание нового шаблона
- `PUT /{id}` - Обновление шаблона
- `DELETE /{id}` - Удаление шаблона
- `GET /{id}/download` - Скачивание LaTeX файла
