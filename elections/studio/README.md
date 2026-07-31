# Election Studio

A fixed-format animated election-night broadcast that renders in Chromium and can be captured by FFmpeg. The prototype uses synthetic data matching the Election Service `TabularResultsView` shape.

## Preview

```sh
cd elections/studio
npm install
npm run dev
```

Open `http://localhost:5173`. The studio rotates through six 18-second scenes:

- `intro`
- `overview`
- `parties`
- `candidates`
- `results`
- `turnout`

Pin a scene for review or screenshots with `?scene=candidates`. By default, the studio runs the complete six-scene program in English, then Czech, then Nissiian, and repeats. Use `?lang=en`, `?lang=cs`, or `?lang=mis` to pin one language; parameters can be combined. The Nissiian catalog currently contains English fallback copy pending authoritative translations.

The election opens on 1 August 2026 at 10:00 and closes on 2 August 2026 at 14:00 in `Europe/Prague`. The header clock, election phase, and countdown update every second.

The title theme plays when the intro scene enters. The transition whoosh plays as each scene wipe begins. Browsers may require one click on the preview before allowing sound. The capture container mixes both cues directly in FFmpeg on the deterministic 18-second scene cycle, so broadcast audio does not depend on browser autoplay.

## Build

```sh
cd elections/studio
npm run build
npm run lint
```

## Container streaming

Build from the repository root because the Dockerfile uses a root-relative context:

```sh
docker build -f elections/studio/Dockerfile -t election-studio:sample .
docker run --rm \
  -e STREAM_URL='rtmps://a.rtmps.youtube.com/live2/YOUR_STREAM_KEY' \
  election-studio:sample
```

`STREAM_URL` is required; the container has no local recording mode. Do not commit or log the ingestion URL. The default encoder output is H.264 at 1080p30, 10 Mbps CBR, with two-second keyframes and AAC stereo audio.

| Variable | Default | Purpose |
| --- | --- | --- |
| `STREAM_URL` | required | Full RTMPS destination |
| `STREAM_DURATION` | unset | Optional bounded duration in seconds for smoke tests |
| `STUDIO_URL` | `http://127.0.0.1/` | Browser source, including optional query string |
| `WIDTH` | `1920` | Capture width |
| `HEIGHT` | `1080` | Capture height |
| `FPS` | `30` | Capture and output frame rate |
| `VIDEO_BITRATE` | `10M` | H.264 CBR target |
| `AUDIO_BITRATE` | `128k` | AAC bitrate |

Example Czech pinned-scene stream:

```sh
docker run --rm \
  -e STREAM_URL='rtmps://a.rtmps.youtube.com/live2/YOUR_STREAM_KEY' \
  -e STUDIO_URL='http://127.0.0.1/?scene=parties&lang=cs' \
  election-studio:sample
```

## One-off Kubernetes deployment

The sample manifest expects the image `ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest`. Build and push that tag, create the stream Secret directly in the target cluster, then apply the Deployment:

```sh
docker build -f elections/studio/Dockerfile \
  -t ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest .
docker push ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest
kubectl create secret generic election-studio-youtube \
  --from-literal=stream-url='rtmps://a.rtmps.youtube.com/live2/YOUR_STREAM_KEY'
kubectl apply -f elections/studio/election-studio.sample.yaml
```

The Deployment uses the `Recreate` strategy so two pods cannot publish concurrently with the same stream key.

## Data integration

Replace the `snapshot` constant in `src/App.tsx` with a polling source for:

```text
GET /public/elections/{electionId}/results/tabular
```

Party palettes are keyed by selection ID in `src/config/party-colors.json`. Interface strings are in `src/config/translations.json`. Scene animations are independent of polling and should retain the last valid snapshot during API failures.

The candidate scene reads `partyGroups[].candidates[]` from the same tabular results response. Candidate order comes from `position`; `isWinner` and `isWithdrawn` control the optional status treatment.
