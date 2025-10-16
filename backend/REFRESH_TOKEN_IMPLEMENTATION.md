# Refresh Token Implementation

## Обзор реализации

Реализован полноценный функционал Refresh Token с использованием **Token Rotation** для повышенной безопасности.

## Архитектура хранения в Redis

### 1. Основное хранилище refresh токенов
```
Key: refresh_token:{userId}:{refreshToken}
Value: JSON metadata
TTL: RefreshTokenExpirationDays (30 дней по умолчанию)
```

**Пример:**
```
refresh_token:123e4567-e89b-12d3-a456-426614174000:550e8400-e29b-41d4-a716-446655440000
→ {"userId":"123e4567...","createdAt":"2025-10-20T14:30:00Z","rotated":false}
```

### 2. Mapping для быстрого поиска
```
Key: refresh_token_mapping:{refreshToken}
Value: userId (GUID as string)
TTL: RefreshTokenExpirationDays (30 дней по умолчанию)
```

**Пример:**
```
refresh_token_mapping:550e8400-e29b-41d4-a716-446655440000
→ 123e4567-e89b-12d3-a456-426614174000
```

## Поток работы

### 1. Генерация токенов (Register / Login)
```csharp
1. Генерируется Access Token (JWT)
2. Генерируется Refresh Token (GUID)
3. Сохраняется в Redis:
   - refresh_token:{userId}:{token} → metadata
   - refresh_token_mapping:{token} → userId
4. Возвращается клиенту
```

### 2. Обновление токенов (Refresh)
```csharp
1. Получаем refresh token от клиента
2. Ищем userId через mapping: refresh_token_mapping:{token}
3. Проверяем существование в основном хранилище: refresh_token:{userId}:{token}
4. Если валидный:
   a. Удаляем старый refresh token из обоих хранилищ
   b. Генерируем новый Access Token
   c. Генерируем новый Refresh Token
   d. Сохраняем новый refresh token в обоих хранилищах
   e. Возвращаем новые токены клиенту
```

### 3. Logout (выход с устройства)
```csharp
1. Получаем Access Token и Refresh Token
2. Добавляем Access Token в blacklist (по JTI)
3. Удаляем Refresh Token из обоих хранилищ
```

### 4. Logout All (выход со всех устройств)
```csharp
1. Получаем все refresh токены пользователя (scan по паттерну)
2. Удаляем все refresh токены из основного хранилища
3. Удаляем все mapping'и для этих токенов
```

## Преимущества реализации

### ✅ Безопасность
1. **Token Rotation**: каждый refresh обновляет оба токена
2. **Двойное хранилище**: mapping позволяет быстро найти токен без scan операций
3. **TTL автоматически очищает** истекшие токены
4. **Возможность отзыва**: logout удаляет токены из Redis

### ✅ Производительность
1. **O(1) операции**: поиск userId по токену через mapping
2. **Нет scan операций** при refresh (только при logout all)
3. **Атомарные операции** Redis

### ✅ Масштабируемость
1. **Stateless backend**: все данные в Redis
2. **Multi-device support**: пользователь может иметь несколько refresh токенов
3. **Horizontal scaling**: Redis можно масштабировать независимо

## Добавленные методы

### IRedisService / RedisService

**Новые методы:**
```csharp
// Mapping operations
Task<bool> SaveRefreshTokenMappingAsync(string refreshToken, Guid userId, TimeSpan expiry);
Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken);
Task<bool> DeleteRefreshTokenMappingAsync(string refreshToken);
Task<List<string>> GetAllRefreshTokensByUserIdAsync(Guid userId);
```

### AuthService

**Обновленные методы:**
```csharp
// Теперь полностью реализован
Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request);

// Обновлен для работы с mapping'ами
Task LogoutAsync(string accessToken, string refreshToken);
Task LogoutAllAsync(Guid userId);

// Обновлен для сохранения mapping
private async Task<LoginResponseDTO> GenerateTokensForUser(User user);
```

## Тестирование

### Сценарий 1: Полный цикл авторизации
```http
# 1. Register
POST /api/auth/register
{
  "email": "test@example.com",
  "password": "password123",
  "name": "Test User"
}
→ Получаем accessToken и refreshToken

# 2. Использование защищенного endpoint
GET /api/auth/me
Authorization: Bearer {accessToken}
→ Успешный ответ

# 3. Обновление токенов (когда access token истек)
POST /api/auth/refresh
{
  "refreshToken": "{refreshToken}"
}
→ Получаем новые accessToken и refreshToken

# 4. Logout
POST /api/auth/logout
Authorization: Bearer {accessToken}
{
  "refreshToken": "{refreshToken}"
}
→ Токены удалены
```

### Сценарий 2: Multi-device
```http
# Пользователь входит с разных устройств
# Device 1
POST /api/auth/login → refreshToken1

# Device 2  
POST /api/auth/login → refreshToken2

# В Redis:
# refresh_token:{userId}:refreshToken1 ✓
# refresh_token:{userId}:refreshToken2 ✓
# refresh_token_mapping:refreshToken1 → userId
# refresh_token_mapping:refreshToken2 → userId

# Logout from Device 1
POST /api/auth/logout
{ "refreshToken": "refreshToken1" }
→ Удален только refreshToken1

# Device 2 продолжает работать с refreshToken2
```

## Безопасность

### Защита от атак

**1. Token Replay Attack**
- ✅ Защита: Token Rotation (старый токен удаляется)

**2. Token Theft**
- ✅ Защита: Короткий срок жизни Access Token (30 мин)
- ✅ Защита: Возможность отзыва через logout

**3. Brute Force**
- 🔄 TODO: Rate Limiting на refresh endpoint

### Рекомендации для Production

1. **HTTPS Only**: Обязательно использовать HTTPS
2. **Secure Storage**: Хранить refresh token в HttpOnly cookies или secure storage
3. **SecretKey**: Использовать сильный случайный ключ (минимум 32 байта)
4. **Environment Variables**: Хранить JWT SecretKey в переменных окружения
5. **Monitoring**: Логировать все операции с токенами
6. **Rate Limiting**: Ограничить количество refresh операций

## Конфигурация

В `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-min-32-characters",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 30
  }
}
```

## Метрики Redis

После работы системы в Redis будут следующие ключи:

```
# Для пользователя с 2 устройствами:
refresh_token:{userId}:{token1}           # Device 1
refresh_token:{userId}:{token2}           # Device 2
refresh_token_mapping:{token1}            # Device 1 mapping
refresh_token_mapping:{token2}            # Device 2 mapping

# После logout на Device 1:
refresh_token:{userId}:{token2}           # Device 2
refresh_token_mapping:{token2}            # Device 2 mapping

# Token в blacklist (после logout):
blacklist:{jti}                          # До истечения access token
```

## Следующие шаги

Рекомендуемые улучшения:

1. ✅ **RefreshToken реализован**
2. 🔄 **Middleware для проверки Blacklist**
3. 🔄 **Rate Limiting на refresh endpoint**
4. 🔄 **Device tracking** (сохранять User-Agent, IP)
5. 🔄 **Suspicious activity detection** (refresh с другого IP)
6. 🔄 **Admin panel** для управления сессиями пользователей

