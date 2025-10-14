FROM node:20-alpine AS build

RUN npm install -g pnpm

WORKDIR /app

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

RUN pnpm run build

FROM nginx:stable-alpine

COPY --from=build /app/dist /usr/share/nginx/html

COPY ../nginx.conf /etc/nginx/conf.d/default.conf
# COPY nginx/chain.pem /etc/nginx/conf.d/chain.pem
# COPY nginx/privkey.pem /etc/nginx/conf.d/privkey.pem

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
