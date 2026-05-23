# Dockerfile DEV: simple, hot reload, apto para iterar en el prototipo.
# Para producción habrá que cambiar a multi-stage:
#   1) FROM node:20-alpine AS builder; npm ci; npm run build
#   2) FROM node:20-alpine AS runner; COPY artefactos; CMD ["npm", "start"]
# y probablemente añadir `output: 'standalone'` en next.config.ts.
FROM node:20-alpine
WORKDIR /app

COPY package*.json ./
RUN npm install --no-audit --no-fund

COPY . .

EXPOSE 3000
CMD ["npm", "run", "dev"]
