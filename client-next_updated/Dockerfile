FROM node:22-alpine AS deps
WORKDIR /app
COPY apps/client-next/package*.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY apps/client-next/ ./
RUN npm run build

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
COPY apps/client-next/docker-entrypoint.sh /usr/local/bin/medsocial-entrypoint
RUN chmod +x /usr/local/bin/medsocial-entrypoint
EXPOSE 3000
ENTRYPOINT ["/usr/local/bin/medsocial-entrypoint"]
CMD ["node", "server.js"]
