# ReportLab Site - Руководство по установке и запуску

Этот проект представляет собой веб-приложение для работы с документами, профилями и титульными страницами. Проект состоит из трех основных частей: клиент (React), сервер (Express) и общий модуль (shared).

## Быстрый старт

1. **Установите зависимости:**
   - Linux/macOS: `./install.sh`
   - Windows: `install.bat`

2. **Настройте Puppeteer** (см. раздел "Настройка Puppeteer")

3. **Запустите проект:**
   - Терминал 1: `cd server && npm run dev`
   - Терминал 2: `cd client && npm run dev`

4. **Откройте браузер:** `http://localhost:5173`

Подробные инструкции см. ниже.

## Требования

Перед установкой убедитесь, что на вашей машине установлены:

- **Node.js** версии 18 или выше (рекомендуется 20+)
- **npm** версии 9 или выше
- **Chromium/Chrome** (для генерации PDF через Puppeteer)

### Проверка версий

```bash
node --version  # Должно быть v18.0.0 или выше
npm --version   # Должно быть 9.0.0 или выше
```

## Установка зависимостей

Проект использует монорепозиторий с тремя пакетами. Необходимо установить зависимости для каждого из них.

### Быстрая установка (рекомендуется)

**Linux/macOS:**
```bash
./install.sh
```

**Windows:**
```cmd
install.bat
```

Скрипт автоматически установит все зависимости и проверит необходимые требования.

### Ручная установка

Если вы предпочитаете установить зависимости вручную:

#### 1. Установка зависимостей для shared модуля

```bash
cd shared
npm install
cd ..
```

#### 2. Установка зависимостей для сервера

```bash
cd server
npm install
cd ..
```

**Важно:** При установке зависимостей сервера, Puppeteer автоматически загрузит Chromium. Если у вас возникнут проблемы, убедитесь, что у вас достаточно места на диске (Puppeteer требует ~300-400 МБ).

#### 3. Установка зависимостей для клиента

```bash
cd client
npm install
cd ..
```

## Настройка Puppeteer (для генерации PDF)

Сервер использует Puppeteer для генерации PDF документов. По умолчанию он настроен на использование Chromium по пути `/usr/bin/chromium`.

### Linux

Если у вас установлен Chromium в другом месте, или вы используете Chrome, вам нужно отредактировать файл:

```
server/src/infrastructure/pdf/common/browserLauncher.ts
```

Измените путь в строке 8:

```typescript
executablePath: '/usr/bin/chromium',  // Замените на ваш путь к Chromium/Chrome
```

**Установка Chromium на Linux:**

- **Ubuntu/Debian:**
  ```bash
  sudo apt-get update
  sudo apt-get install chromium-browser
  ```

- **Arch Linux:**
  ```bash
  sudo pacman -S chromium
  ```

- **Fedora:**
  ```bash
  sudo dnf install chromium
  ```

### macOS

На macOS Puppeteer обычно использует загруженный Chromium автоматически. Если возникают проблемы, можно указать путь к Chrome:

```typescript
executablePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
```

### Windows

На Windows укажите путь к установленному Chrome:

```typescript
executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
```

Или используйте загруженный Puppeteer Chromium (уберите параметр `executablePath`).

## Структура данных

Проект использует файловую систему для хранения данных. Убедитесь, что папка `data/` существует в корне проекта со следующей структурой:

```
data/
├── documents/      # Документы и загруженные изображения
├── profiles/       # Профили пользователей
└── title-pages/    # Титульные страницы
```

Если папка `data/` отсутствует, она будет создана автоматически при первом запуске сервера.

## Запуск проекта

### Режим разработки

Для разработки нужно запустить сервер и клиент в отдельных терминалах.

#### Терминал 1: Запуск сервера

```bash
cd server
npm run dev
```

Сервер запустится на `http://localhost:3001`

#### Терминал 2: Запуск клиента

```bash
cd client
npm run dev
```

Клиент запустится на `http://localhost:5173`

После запуска откройте браузер и перейдите по адресу `http://localhost:5173`

### Production режим

#### 1. Сборка сервера

```bash
cd server
npm run build
```

#### 2. Сборка клиента

```bash
cd client
npm run build
```

#### 3. Запуск сервера

```bash
cd server
npm start
```

**Примечание:** В production режиме вам нужно настроить веб-сервер (например, Nginx) для раздачи статических файлов из `client/dist/` или настроить Express для их раздачи.

## Переменные окружения

Сервер поддерживает следующие переменные окружения:

- `PORT` - Порт для запуска сервера (по умолчанию: 3001)

Пример использования:

```bash
PORT=8080 npm run dev
```

## Проверка работоспособности

После запуска сервера и клиента:

1. Откройте `http://localhost:5173` в браузере
2. Проверьте, что сервер отвечает: `http://localhost:3001/api/health`
3. Должен вернуться JSON: `{"status":"ok","timestamp":"..."}`

## Решение проблем

### Проблема: Puppeteer не может найти браузер

**Решение:**
1. Убедитесь, что Chromium/Chrome установлен
2. Проверьте путь в `server/src/infrastructure/pdf/common/browserLauncher.ts`
3. На Linux убедитесь, что установлены необходимые зависимости:
   ```bash
   # Ubuntu/Debian
   sudo apt-get install -y \
     ca-certificates \
     fonts-liberation \
     libappindicator3-1 \
     libasound2 \
     libatk-bridge2.0-0 \
     libatk1.0-0 \
     libc6 \
     libcairo2 \
     libcups2 \
     libdbus-1-3 \
     libexpat1 \
     libfontconfig1 \
     libgbm1 \
     libgcc1 \
     libglib2.0-0 \
     libgtk-3-0 \
     libnspr4 \
     libnss3 \
     libpango-1.0-0 \
     libpangocairo-1.0-0 \
     libstdc++6 \
     libx11-6 \
     libx11-xcb1 \
     libxcb1 \
     libxcomposite1 \
     libxcursor1 \
     libxdamage1 \
     libxext6 \
     libxfixes3 \
     libxi6 \
     libxrandr2 \
     libxrender1 \
     libxss1 \
     libxtst6 \
     lsb-release \
     wget \
     xdg-utils
   ```

### Проблема: Ошибки при установке зависимостей

**Решение:**
1. Убедитесь, что используете правильную версию Node.js
2. Очистите кэш npm: `npm cache clean --force`
3. Удалите `node_modules` и `package-lock.json`, затем установите заново:
   ```bash
   rm -rf node_modules package-lock.json
   npm install
   ```

### Проблема: Порт уже занят

**Решение:**
1. Измените порт сервера через переменную окружения: `PORT=3002 npm run dev`
2. Или измените порт клиента в `client/vite.config.ts` (строка 7)

### Проблема: Ошибки TypeScript

**Решение:**
1. Убедитесь, что все зависимости установлены
2. Запустите проверку типов:
   ```bash
   cd shared && npm run typecheck
   cd ../server && npm run typecheck
   cd ../client && npm run typecheck
   ```

## Структура проекта

```
ReportLab site/
├── client/          # React клиентское приложение
│   ├── src/
│   │   ├── application/    # Бизнес-логика клиента
│   │   ├── domain/         # Доменные модели
│   │   ├── infrastructure/ # API клиенты
│   │   ├── presentation/   # React компоненты и страницы
│   │   └── utils/          # Утилиты
│   └── package.json
├── server/          # Express сервер
│   ├── src/
│   │   ├── application/    # Сервисы приложения
│   │   ├── domain/         # Доменные модели и репозитории
│   │   ├── infrastructure/ # PDF генерация, персистентность
│   │   └── presentation/   # API роуты
│   └── package.json
├── shared/          # Общие типы и утилиты
│   ├── src/
│   │   ├── types/          # Общие TypeScript типы
│   │   └── utils/          # Общие утилиты
│   └── package.json
└── data/            # Данные приложения (документы, профили, титульные страницы)
```

## Дополнительная информация

- Клиент использует Vite для разработки и сборки
- Сервер использует Express с TypeScript
- Для генерации PDF используется Puppeteer
- Проект использует ES Modules (type: "module" в package.json)

## Поддержка

При возникновении проблем проверьте:
1. Версии Node.js и npm
2. Установлен ли Chromium/Chrome
3. Правильность путей в конфигурации Puppeteer
4. Доступность портов 3001 и 5173
5. Логи сервера и клиента в консоли

