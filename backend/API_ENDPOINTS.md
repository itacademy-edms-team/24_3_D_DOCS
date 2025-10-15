# API Endpoints

## Auth Controller

Base URL: `/api/auth`

### 1. Register - Регистрация нового пользователя

**POST** `/api/auth/register`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "name": "John Doe"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "accessTokenExpiration": "2025-10-20T15:00:00Z",
  "refreshTokenExpiration": "2025-11-19T14:30:00Z",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "User",
    "createdAt": "2025-10-20T14:30:00Z"
  }
}
```

**Errors:**
- `400 Bad Request` - Пользователь уже существует или невалидные данные
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

### 2. Login - Вход пользователя

**POST** `/api/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "accessTokenExpiration": "2025-10-20T15:00:00Z",
  "refreshTokenExpiration": "2025-11-19T14:30:00Z",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "User",
    "createdAt": "2025-10-20T14:30:00Z"
  }
}
```

**Errors:**
- `401 Unauthorized` - Неверный email или пароль
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

### 3. Refresh Token - Обновление токенов

**POST** `/api/auth/refresh`

⚠️ **TODO:** Требует доработки архитектуры хранения

**Request:**
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "660e8400-e29b-41d4-a716-446655440001",
  "accessTokenExpiration": "2025-10-20T16:00:00Z",
  "refreshTokenExpiration": "2025-11-19T15:30:00Z"
}
```

**Errors:**
- `401 Unauthorized` - Невалидный refresh token
- `501 Not Implemented` - Метод требует доработки
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

### 4. Logout - Выход пользователя

**POST** `/api/auth/logout`

🔒 **Requires Authentication**

**Headers:**
```
Authorization: Bearer <accessToken>
```

**Request:**
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response (200 OK):**
```json
{
  "message": "Выход выполнен успешно"
}
```

**Errors:**
- `401 Unauthorized` - Невалидный токен или токен не предоставлен
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

### 5. Logout All - Выход со всех устройств

**POST** `/api/auth/logout-all`

🔒 **Requires Authentication**

**Headers:**
```
Authorization: Bearer <accessToken>
```

**Response (200 OK):**
```json
{
  "message": "Выход выполнен со всех устройств"
}
```

**Errors:**
- `401 Unauthorized` - Невалидный токен
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

### 6. Get Current User - Информация о текущем пользователе

**GET** `/api/auth/me`

🔒 **Requires Authentication**

**Headers:**
```
Authorization: Bearer <accessToken>
```

**Response (200 OK):**
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "role": "User",
  "claims": [
    {
      "type": "sub",
      "value": "123e4567-e89b-12d3-a456-426614174000"
    },
    {
      "type": "email",
      "value": "user@example.com"
    },
    {
      "type": "role",
      "value": "User"
    },
    {
      "type": "jti",
      "value": "unique-jwt-id"
    }
  ]
}
```

**Errors:**
- `401 Unauthorized` - Невалидный токен
- `500 Internal Server Error` - Внутренняя ошибка сервера

---

## JWT Token Structure

### Access Token Claims:
- `sub` (Subject) - UserId (GUID)
- `email` - Email пользователя
- `role` - Роль пользователя (User, Admin, etc.)
- `jti` (JWT ID) - Уникальный идентификатор токена (для blacklist)
- `iat` (Issued At) - Время выдачи токена
- `exp` (Expiration) - Время истечения токена
- `iss` (Issuer) - Издатель токена (RusalProject)
- `aud` (Audience) - Аудитория (RusalProject-Client)

### Token Lifetimes:
- **Access Token**: 30 минут (configurable)
- **Refresh Token**: 30 дней (configurable)

---

## Authentication Flow

### 1. Registration / Login:
```
Client → POST /api/auth/register or /api/auth/login
       ← AccessToken + RefreshToken
```

### 2. Authenticated Request:
```
Client → GET /api/resource
       Headers: Authorization: Bearer <accessToken>
       ← Protected Resource
```

### 3. Token Refresh:
```
Client → POST /api/auth/refresh
       Body: { refreshToken }
       ← New AccessToken + New RefreshToken
```

### 4. Logout:
```
Client → POST /api/auth/logout
       Headers: Authorization: Bearer <accessToken>
       Body: { refreshToken }
       ← Success
```

---

## Security Features

✅ **Implemented:**
- BCrypt password hashing (work factor: 12)
- JWT token generation and validation
- Access token blacklist on logout
- Refresh token storage in Redis
- Role-based authorization
- CORS protection
- HTTPS redirection

🔄 **TODO:**
- RefreshToken implementation needs improvement
- Middleware for automatic blacklist checking
- Rate limiting on login endpoint
- Email verification
- Password reset functionality
- Two-factor authentication
- Device tracking and management

---

## Error Responses Format

All error responses follow this structure:

```json
{
  "message": "Error description in Russian"
}
```

**HTTP Status Codes:**
- `200` - Success
- `400` - Bad Request (validation errors, user already exists)
- `401` - Unauthorized (invalid credentials, expired token)
- `500` - Internal Server Error
- `501` - Not Implemented

