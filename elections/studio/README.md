# Election Studio

A fixed-format animated election-night broadcast that polls Election Service and renders in Chromium for capture by FFmpeg.

## Preview

```sh
cd elections/studio
npm install
npm run dev
```

Open `http://localhost:5173`. The studio rotates through seven 18-second scenes:

- `intro`
- `overview`
- `parties`
- `candidates`
- `results`
- `seats`
- `turnout`

Pin a scene for review or screenshots with `?scene=seats`. By default, the studio runs the complete seven-scene program in English, then Czech, then Nissiian, and repeats. Use `?lang=en`, `?lang=cs`, or `?lang=mis` to pin one language; parameters can be combined.

The header clock, election phase, and countdown update every second. Opening and closing times come from the configured election's public API response.

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
  -e ELECTION_API_BASE_URL='https://elections-api.mklu.org' \
  -e ELECTION_ID='65e8aa59-1282-498b-804d-0d46d6e6f3f0' \
  election-studio:sample
```

`STREAM_URL`, `ELECTION_API_BASE_URL`, and `ELECTION_ID` are required; the container has no local recording mode. Do not commit or log the ingestion URL. The default encoder output is H.264 at 1080p30, 10 Mbps CBR, with two-second keyframes and AAC stereo audio.

| Variable | Default | Purpose |
| --- | --- | --- |
| `STREAM_URL` | required | Full RTMPS destination |
| `ELECTION_API_BASE_URL` | required | Election Service API origin, without a trailing path |
| `ELECTION_ID` | required | Election UUID polled by the studio |
| `POLL_INTERVAL_MS` | `5000` | Delay between completed result requests |
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
  -e ELECTION_API_BASE_URL='https://elections-api.mklu.org' \
  -e ELECTION_ID='65e8aa59-1282-498b-804d-0d46d6e6f3f0' \
  -e STUDIO_URL='http://127.0.0.1/?scene=parties&lang=cs' \
  election-studio:sample
```

## One-off Kubernetes deployment

The sample manifest expects the image `ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest`. Build and push that tag, replace the placeholder in `election-studio.secret.sample.yaml`, then apply the Secret and Deployment. Do not commit a manifest containing the real stream key.

```sh
docker build -f elections/studio/Dockerfile \
  -t ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest .
docker push ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-election-studio:latest
kubectl apply -f elections/studio/election-studio.secret.sample.yaml
kubectl apply -f elections/studio/election-studio.sample.yaml
```

The Deployment uses the `Recreate` strategy so two pods cannot publish concurrently with the same stream key.

## Data integration

At container startup, `start-studio.sh` writes the API settings to `runtime-config.js` before nginx starts. The browser requests the following endpoints immediately and again after each configured polling interval:

```text
GET /public/elections/{electionId}
GET /public/elections/{electionId}/results/tabular
```

Successful responses replace the displayed snapshot. Failed requests are logged and retain the last successful response while scene animations continue. Party palettes can be keyed by selection ID in `src/config/party-colors.json`; unknown UUIDs receive palettes in list order. Interface strings are in `src/config/translations.json`.

Visible elections and their tabular snapshots are available in every workflow state, including Draft and Closed. `IsPubliclyVisible` remains the access control: hidden elections are not returned on public surfaces.

The candidate scene reads `partyGroups[].candidates[]` from the same tabular results response. Candidate order comes from `position`; `isWinner` and `isWithdrawn` control the optional status treatment. The projected-seats scene applies the D'Hondt highest-averages method to each party group's current vote count and the election's `seatCount`. Equal quotients are resolved by total votes and then ballot-list order. Before any votes are counted, configured seats remain visible in a neutral, unallocated state.
