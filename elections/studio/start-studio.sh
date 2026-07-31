#!/bin/sh
set -eu

export DISPLAY=:99
width="${WIDTH:-1920}"
height="${HEIGHT:-1080}"
fps="${FPS:-30}"
keyframe_interval=$((fps * 2))
studio_url="${STUDIO_URL:-http://127.0.0.1/}"
: "${STREAM_URL:?STREAM_URL is required (for example, an RTMPS YouTube ingestion URL)}"
: "${ELECTION_API_BASE_URL:?ELECTION_API_BASE_URL is required}"
: "${ELECTION_ID:?ELECTION_ID is required}"

case "${POLL_INTERVAL_MS:-5000}" in
        ''|*[!0-9]*) echo "POLL_INTERVAL_MS must be an integer" >&2; exit 1 ;;
esac

json_escape() {
        printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

cat > /usr/share/nginx/html/runtime-config.js <<EOF
window.__STUDIO_CONFIG__ = {
    electionApiBaseUrl: "$(json_escape "$ELECTION_API_BASE_URL")",
    electionId: "$(json_escape "$ELECTION_ID")",
    pollIntervalMs: ${POLL_INTERVAL_MS:-5000},
}
EOF

cleanup() {
    kill "${chromium_pid:-}" "${xvfb_pid:-}" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

Xvfb "$DISPLAY" -screen 0 "${width}x${height}x24" -nolisten tcp -ac &
xvfb_pid=$!
nginx

for attempt in $(seq 1 30); do
    if curl --fail --silent --output /dev/null "$studio_url"; then
        break
    fi
    if [ "$attempt" -eq 30 ]; then
        echo "Studio web server did not become ready" >&2
        exit 1
    fi
    sleep 1
done

rm -rf /tmp/chromium
chromium \
    --no-sandbox \
    --autoplay-policy=no-user-gesture-required \
    --disable-dev-shm-usage \
    --disable-background-timer-throttling \
    --disable-renderer-backgrounding \
    --disable-session-crashed-bubble \
    --hide-scrollbars \
    --no-first-run \
    --user-data-dir=/tmp/chromium \
    --window-position=0,0 \
    --window-size="${width},${height}" \
    --kiosk "$studio_url" &
chromium_pid=$!

sleep 2

duration_options=""
if [ -n "${STREAM_DURATION:-}" ]; then
    duration_options="-t $STREAM_DURATION"
fi

# duration_options is intentionally split into FFmpeg arguments for bounded smoke tests.
# shellcheck disable=SC2086
exec ffmpeg \
    -y \
    -f x11grab -draw_mouse 0 -framerate "$fps" -video_size "${width}x${height}" -i "$DISPLAY.0" \
    -i /usr/share/nginx/html/audio/title-theme.mp3 \
    -i /usr/share/nginx/html/audio/transition-whoosh.mp3 \
    -filter_complex "[1:a]aformat=sample_rates=44100:channel_layouts=stereo,volume=0.38,apad=pad_dur=101.364906,atrim=duration=108,aloop=loop=-1:size=4762800[title];[2:a]aformat=sample_rates=44100:channel_layouts=stereo,volume=0.3,apad=pad_dur=11.112,atrim=duration=18,aloop=loop=-1:size=793800,adelay=18000|18000[whoosh];[title][whoosh]amix=inputs=2:duration=longest:normalize=0[audio]" \
    -map 0:v -map "[audio]" \
    -c:v libx264 -preset veryfast -pix_fmt yuv420p \
    -r "$fps" -g "$keyframe_interval" -keyint_min "$keyframe_interval" -sc_threshold 0 -bf 2 -refs 1 \
    -b:v "${VIDEO_BITRATE:-10M}" -minrate "${VIDEO_BITRATE:-10M}" -maxrate "${VIDEO_BITRATE:-10M}" -bufsize 20M \
    -c:a aac -b:a "${AUDIO_BITRATE:-128k}" -ar 44100 \
    $duration_options -f flv "$STREAM_URL"
