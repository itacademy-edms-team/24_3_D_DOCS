# Rusal Project - Full Stack Application

JWT Authentication система с React + ASP.NET Core + PostgreSQL + Redis

## 🚀 Быстрый старт

```bash
# Запуск всего проекта
docker compose up --build -d

# Остановка
docker compose down

# Остановка с очисткой данных
docker compose down -v
```

## 📦 Сервисы

| Сервис | URL | Описание |
|--------|-----|----------|
| Frontend | http://localhost:3000 | React приложение |
| Backend API | http://localhost:5159 | ASP.NET Core Web API |
| Swagger | http://localhost:5159/swagger | API документация |
| PostgreSQL | localhost:5432 | База данных |
| Redis | localhost:6379 | Кэш и токены |

## 🔑 Учетные данные

### PostgreSQL
- User: `rusal_user`
- Password: `rusal_password`
- Database: `rusal_db`

### Swagger
1. Открыть http://localhost:5159/swagger
2. Зарегистрироваться через `/api/auth/register`
3. Нажать "Authorize" и ввести токен: `Bearer {accessToken}`

## 📁 Структура

```
.
├── backend/          # ASP.NET Core Web API
│   ├── Controllers/  # API endpoints
│   ├── Services/     # Business logic
│   ├── Models/       # DTOs & Entities
│   └── Provider/     # Database & Redis
├── frontend/         # React + TypeScript
│   └── src/
│       ├── app/      # App configuration
│       ├── pages/    # Page components
│       ├── features/ # Feature modules
│       ├── entities/ # Business entities
│       └── shared/   # Shared utilities
└── docker-compose.yml
```

## 🛠️ Разработка

### Backend (локально)
```bash
cd backend
dotnet restore
dotnet run
```

### Frontend (локально)
```bash
cd frontend
pnpm install
pnpm dev
```

## ✅ Функциональность

- ✅ JWT аутентификация (Access + Refresh токены)
- ✅ Регистрация и логин
- ✅ Protected routes
- ✅ Token rotation
- ✅ Автоматические миграции БД
- ✅ Swagger UI для тестирования
- ✅ Docker Compose для всего стека
- ✅ Glassmorphism UI дизайн
