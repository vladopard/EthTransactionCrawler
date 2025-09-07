# ===== build =====
FROM node:20 AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci

COPY . .
ARG VITE_API_URL=http://backend:8080
ENV VITE_API_URL=${VITE_API_URL}

RUN npm run build

# ===== runtime =====
FROM nginx:stable-alpine
WORKDIR /usr/share/nginx/html
COPY --from=builder /app/dist ./
EXPOSE 80
