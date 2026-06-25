# syntax=docker/dockerfile:1.7

# ============================================
# Stage 1: Dependencies (Bun - быстрая установка)
# ============================================
FROM oven/bun:1.2 AS deps
WORKDIR /app

ENV BUN_INSTALL_CACHE_DIR=/root/.bun/install/cache

COPY package.json bun.lock* package-lock.json* yarn.lock* pnpm-lock.yaml* ./

# Устанавливаем ВСЕ зависимости (включая dev для build)
RUN --mount=type=cache,id=bun-cache-v2,target=/root/.bun/install/cache \
    bun install --frozen-lockfile

# ============================================
# Stage 2: Build (Node.js - надёжная сборка)
# ============================================
FROM node:22-alpine AS builder
WORKDIR /app

ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_SHARP_PATH=/app/node_modules/sharp

COPY --from=deps /app/node_modules ./node_modules
COPY . .

RUN mkdir -p ./messages

# 🎯 Обычная сборка (без standalone)
RUN --mount=type=cache,id=nextjs-build,target=/app/.next/cache \
    npm run build

# ============================================
# Stage 3: Production (Node.js Alpine)
# ============================================
FROM node:22-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Timezone
RUN apk add --no-cache tzdata \
    && cp /usr/share/zoneinfo/Europe/Moscow /etc/localtime \
    && echo "Europe/Moscow" > /etc/timezone \
    && rm -rf /var/cache/apk/*

# Непривилегированный пользователь
RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

# 🎯 Копируем node_modules из deps (нужны для `next start`)
COPY --from=deps --chown=nextjs:nodejs /app/node_modules ./node_modules

# Public файлы
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# 🎯 Копируем ВЕСЬ .next (без standalone структура другая)
COPY --from=builder --chown=nextjs:nodejs /app/.next ./.next

# package.json нужен для `next start`
COPY --from=builder --chown=nextjs:nodejs /app/package.json ./package.json

# next-intl messages (если есть)
COPY --from=builder --chown=nextjs:nodejs /app/messages ./messages

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"
ENV NEXT_TRUST_FORWARD_HEADERS=1

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

# 🎯 Запуск через next start (НЕ node server.js!)
CMD ["npx", "next", "start"]