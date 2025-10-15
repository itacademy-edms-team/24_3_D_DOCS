# RusalProject Backend

ASP.NET Core Web API проект с PostgreSQL и Redis.

## Структура проекта

```
backend/
├── Controllers/
│   └── AuthController.cs          # JWT авторизация (register, login, refresh, logout)
├── Models/
│   ├── Entities/
│   │   ├── User.cs                # Entity модель пользователя
│   │   └── Schema.cs              # Entity модель схемы
│   └── DTOs/
│       ├── Auth/                  # DTOs для авторизации
│       │   ├── LoginRequestDTO.cs
│       │   ├── LoginResponseDTO.cs
│       │   ├── RegisterRequestDTO.cs
│       │   ├── RefreshTokenRequestDTO.cs
│       │   └── TokenResponseDTO.cs
│       ├── UserDTO.cs
│       └── SchemaDTO.cs
├── Services/
│   └── Auth/
│       ├── IPasswordHasher.cs     # Интерфейс хеширования паролей
│       ├── PasswordHasher.cs      # BCrypt хеширование
│       ├── IJwtService.cs         # Интерфейс работы с JWT
│       ├── JwtService.cs          # Генерация и валидация JWT
│       ├── IAuthService.cs        # Интерфейс авторизации
│       └── AuthService.cs         # Бизнес-логика авторизации
├── Provider/
│   ├── Database/
│   │   └── ApplicationDbContext.cs  # EF Core DbContext
│   └── Redis/
│       ├── IRedisService.cs       # Интерфейс Redis
│       └── RedisService.cs        # Redis (токены, blacklist, rate limiting)
├── Migrations/                    # EF Core миграции
├── Program.cs                     # Точка входа, DI, middleware
└── RusalProject.http              # HTTP запросы для тестирования API
```

## Технологии

- **ASP.NET Core 9.0** - Web API framework
- **PostgreSQL** (через Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4)
- **Redis** (через StackExchange.Redis 2.9.32)
- **Entity Framework Core 9.0** - ORM и миграции
- **BCrypt.Net-Next 4.0.3** - хеширование паролей
- **System.IdentityModel.Tokens.Jwt 8.14.0** - работа с JWT
- **Microsoft.AspNetCore.Authentication.JwtBearer 9.0.10** - JWT middleware

## Конфигурация

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=rusal_db;Username=postgres;Password=postgres",
    "Redis": "redis:6379"
  }
}
```

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=rusal_db;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  }
}
```

## База данных

### Entity модели

**User:**
- `Id` - GUID (PK)
- `Email` - string (unique, indexed)
- `PasswordHash` - string
- `Name` - string
- `Role` - string (default: "User")
- `CreatedAt` - DateTime
- `UpdatedAt` - DateTime

**Schema:**
- `Id` - GUID (PK)
- `CreatorId` - GUID (FK → User)
- `SchemaData` - string (text)
- `CreatedAt` - DateTime
- `UpdatedAt` - DateTime

### Миграции EF Core

```bash
# Создать новую миграцию
dotnet ef migrations add <MigrationName>

# Применить миграции к БД
dotnet ef database update

# Откатить миграцию
dotnet ef migrations remove

# Посмотреть список миграций
dotnet ef migrations list
```

## Redis

### Функционал RedisService

**Базовые операции:**
- `GetAsync(key)` - получить значение
- `SetAsync(key, value, expiry)` - установить значение с TTL
- `DeleteAsync(key)` - удалить ключ
- `ExistsAsync(key)` - проверить существование

**Refresh Token операции:**
- `SaveRefreshTokenAsync(userId, token, expiry, metadata)` - сохранить refresh token
- `GetRefreshTokenAsync(userId, token)` - получить refresh token
- `DeleteRefreshTokenAsync(userId, token)` - удалить refresh token
- `DeleteAllUserRefreshTokensAsync(userId)` - удалить все токены пользователя

**Blacklist операции (для Access Token):**
- `AddToBlacklistAsync(jti, expiry)` - добавить токен в blacklist
- `IsBlacklistedAsync(jti)` - проверить, в blacklist ли токен

**Rate Limiting:**
- `IncrementLoginAttemptsAsync(identifier, expiry)` - увеличить счетчик попыток
- `GetLoginAttemptsAsync(identifier)` - получить количество попыток
- `ResetLoginAttemptsAsync(identifier)` - сбросить счетчик

### Redis Keys структура

```
# Refresh tokens
refresh_token:{userId}:{tokenGuid} → metadata (TTL: 30 days)

# Blacklist
blacklist:{jti} → "true" (TTL: до истечения access token)

# Rate limiting
login_attempts:{email} → counter (TTL: 15 minutes)
```

## Запуск проекта

### Локально (Development)

```bash
# Запустить проект
dotnet run

# Или с hot reload
dotnet watch run
```

Проект будет доступен на `http://localhost:5000` (или порт из launchSettings.json)

### Docker

```bash
# Собрать образ
docker build -t rusal-backend .

# Запустить контейнер
docker run -p 8080:8080 -p 8081:8081 rusal-backend
```

## Примечания

- Пароли хешируются с использованием BCrypt (будет добавлено)
- JWT токены с коротким сроком жизни (15-30 минут)
- Refresh токены с длинным сроком жизни (7-30 дней)
- Все токены можно отозвать через Redis

