# 📧 Настройка Email отправки

## ⚠️ Первый запуск

**ВАЖНО:** Файл `appsettings.Development.json` не должен попадать в Git!

1. **Создать локальный конфиг** (если еще не существует):
   ```bash
   cp backend/appsettings.Development.json.example backend/appsettings.Development.json
   ```

2. **Настроить email** (см. инструкции ниже)

## Для Gmail (рекомендуется)

### 1. Создание App Password

Gmail не позволяет использовать обычный пароль для SMTP. Нужно создать "App Password":

1. **Включить двухфакторную аутентификацию** (если еще не включена):
   - Перейти: https://myaccount.google.com/security
   - Найти "2-Step Verification" и включить

2. **Создать App Password**:
   - Перейти: https://myaccount.google.com/apppasswords
   - Выбрать "Mail" и "Other" (напиши "Rusal Project")
   - Скопировать сгенерированный 16-значный пароль

3. **Настроить `appsettings.Development.json`**:
   ```json
   "Email": {
     "EnableEmailSending": true,
     "FromName": "Rusal Project",
     "FromAddress": "твой-email@gmail.com",
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": 587,
     "SmtpUsername": "твой-email@gmail.com",
     "SmtpPassword": "твой-app-password-16-символов"
   }
   ```

4. **Перезапустить Docker**:
   ```bash
   docker compose down
   docker compose up --build -d
   ```

## Для Yandex Mail

Если хочешь использовать Yandex.ru:

1. **Настроить `appsettings.Development.json`**:
   ```json
   "Email": {
     "EnableEmailSending": true,
     "FromName": "Rusal Project",
     "FromAddress": "твой-email@yandex.ru",
     "SmtpHost": "smtp.yandex.ru",
     "SmtpPort": 587,
     "SmtpUsername": "твой-email@yandex.ru",
     "SmtpPassword": "твой-пароль-от-яндекса"
   }
   ```

2. **Включить SMTP в настройках Yandex**:
   - Перейти: https://mail.yandex.ru/#setup/client
   - Включить "С почтовых программ"

## Для Mail.ru

1. **Настроить `appsettings.Development.json`**:
   ```json
   "Email": {
     "EnableEmailSending": true,
     "FromName": "Rusal Project",
     "FromAddress": "твой-email@mail.ru",
     "SmtpHost": "smtp.mail.ru",
     "SmtpPort": 587,
     "SmtpUsername": "твой-email@mail.ru",
     "SmtpPassword": "твой-пароль"
   }
   ```

2. **Включить SMTP в настройках Mail.ru**:
   - Перейти в настройки почты
   - Найти раздел "Почтовые клиенты"
   - Включить доступ по SMTP

## Режим разработки (без Email)

Если не хочешь настраивать email, просто оставь `EnableEmailSending: false`. 
В этом случае код будет логироваться в консоль:

```bash
# Смотреть логи с кодом верификации:
docker compose logs backend --tail 20

# Искать коды в логах:
docker compose logs backend | grep "Code:"
```

## Проверка работы

После настройки:

1. Открыть http://localhost:3000
2. Перейти на вкладку "Регистрация"
3. Ввести данные и нажать "Отправить код"
4. Проверить почту - письмо должно прийти в течение 1-2 секунд
5. Ввести полученный код и завершить регистрацию

## Возможные проблемы

### "Failed to send email"
- Проверь правильность email/пароля
- Убедись что App Password создан (для Gmail)
- Убедись что SMTP доступ включен (для Yandex/Mail.ru)
- Проверь интернет-соединение

### Письмо не приходит
- Проверь папку "Спам"
- Проверь логи: `docker compose logs backend --tail 50`
- Убедись что `EnableEmailSending: true`

### "Authentication failed"
- Для Gmail: используй App Password, не обычный пароль!
- Для Yandex/Mail.ru: проверь что SMTP доступ включен

