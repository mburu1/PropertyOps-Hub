# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /app

# Lockfile-only install first for layer caching.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
# angular.json defaultConfiguration is production, so this emits the optimised bundle
# into dist/PropertyOpsHub.Web/browser.
RUN npm run build

# ---- runtime --------------------------------------------------------------
FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/PropertyOpsHub.Web/browser /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
