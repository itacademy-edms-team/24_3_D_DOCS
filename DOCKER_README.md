# 🐳 Docker Setup

## Запуск проекта

```bash
# Запустить все сервисы
docker compose up -d

# Остановить
docker compose down

# Остановить и удалить volumes (очистка БД)
docker compose down -v

# Пересобрать и запустить
docker compose up -d --build
```

## Сервисы

- **PostgreSQL** - `localhost:5432`
  - Database: `rusal_db`
  - User: `rusal_user`
  - Password: `rusal_password`

- **Redis** - `localhost:6379`

- **Backend API** - `http://localhost:5159`
  - Swagger: `http://localhost:5159/openapi/v1.json`
  - Миграции применяются автоматически при запуске

## Проверка работы

```bash
# Проверка логов
docker logs rusal_backend
docker logs rusal_postgres
docker logs rusal_redis

# Проверка статуса
docker compose ps

# Тест API
curl -X POST http://localhost:5159/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123","name":"Test User"}'
```

## API Endpoints

- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Вход
- `POST /api/auth/refresh` - Обновление токена
- `POST /api/auth/logout` - Выход
- `POST /api/auth/logout-all` - Выход со всех устройств
- `GET /api/auth/me` - Информация о текущем пользователе

