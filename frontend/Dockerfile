# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# npm встроен в node; альтернатива — pnpm через corepack
RUN corepack enable && corepack prepare pnpm@10.17.1 --activate || true

# Копируем файлы зависимостей
COPY package.json package-lock.json ./

# Устанавливаем зависимости (npm встроен в node)
RUN npm ci

# Копируем исходный код
COPY . .

# Принимаем BASE_URI как build arg (по умолчанию для dev)
ARG BASE_URI=http://localhost:5159
ENV BASE_URI=${BASE_URI}
ENV NODE_OPTIONS=--max-old-space-size=4096

# Билдим приложение
RUN npm run build

# Stage 2: Production
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Удаляем дефолтный контент nginx
RUN rm -rf ./*

# Копируем билд из первого stage
COPY --from=build /app/dist .

# Копируем конфигурацию nginx для SPA
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

