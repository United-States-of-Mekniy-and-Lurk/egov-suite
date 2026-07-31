FROM node:22-bookworm-slim AS build
WORKDIR /src

COPY elections/studio/package.json elections/studio/package-lock.json ./
RUN npm ci

COPY elections/studio/ ./
RUN npm run build

FROM debian:bookworm-slim AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        chromium \
        curl \
        ffmpeg \
        nginx \
        tini \
        xvfb \
    && sed -i 's#root /var/www/html;#root /usr/share/nginx/html;#' /etc/nginx/sites-available/default \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /src/dist/ /usr/share/nginx/html/
COPY elections/studio/start-studio.sh /usr/local/bin/start-studio

RUN chmod +x /usr/local/bin/start-studio \
    && mkdir -p /run/nginx

ENV WIDTH=1920 \
    HEIGHT=1080 \
    FPS=30 \
    VIDEO_BITRATE=10M \
    AUDIO_BITRATE=128k

EXPOSE 80

ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/start-studio"]
