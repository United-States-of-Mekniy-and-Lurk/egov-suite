import { useEffect, useRef, useState, type CSSProperties } from 'react'
import './App.css'
import partyColorData from './config/party-colors.json'
import translationData from './config/translations.json'

type Locale = keyof typeof translationData
type TranslationKey = keyof typeof translationData.en
type ElectionPhase = 'upcoming' | 'open' | 'closed'
type PartyPalette = { primary: string; secondary: string }

type ResultRow = {
  selectionId: string
  selectionLabel: string
  selectionType: string
  partyName: string | null
  voteCount: number
  percentage: number
  territoryCode: string | null
}

type CandidateResult = {
  id: string
  displayName: string
  position: number
  isWithdrawn: boolean
  isWinner: boolean
}

type PartyResultGroup = {
  partyListId: string
  partyName: string
  listName: string
  voteCount: number
  percentage: number
  candidates: CandidateResult[]
}

type ElectionSnapshot = {
  electionId: string
  title: string
  status: string
  totalValidBallots: number
  participatingVoters: number
  eligibleVoters: number | null
  turnoutPercentage: number | null
  isLive: boolean
  generatedAt: string
  seatCount: number | null
  winnerCount: number
  rows: ResultRow[]
  partyGroups: PartyResultGroup[]
}

type StudioConfig = {
  electionApiBaseUrl: string
  electionId: string
  pollIntervalMs?: number
}

type ElectionSchedule = {
  votingStartsAt: string
  votingEndsAt: string
}

type SeatProjection = PartyResultGroup & {
  seats: number
  palette: PartyPalette
}

type SeatPosition = {
  x: number
  y: number
  angle: number
  radius: number
}

declare global {
  interface Window { __STUDIO_CONFIG__?: StudioConfig }
}

const emptySnapshot: ElectionSnapshot = {
  electionId: '', title: '', status: '', totalValidBallots: 0, participatingVoters: 0,
  eligibleVoters: null, turnoutPercentage: null, isLive: false, generatedAt: '', seatCount: null,
  winnerCount: 0, rows: [], partyGroups: [],
}

const partyColors = partyColorData as Record<string, PartyPalette>
const sceneNames = ['intro', 'overview', 'parties', 'candidates', 'results', 'seats', 'turnout'] as const
const localeOrder: Locale[] = ['en', 'cs', 'mis']
type SceneName = (typeof sceneNames)[number]
const sceneDuration = 18000
const localeTags: Record<Locale, string> = { en: 'en-GB', cs: 'cs-CZ', mis: 'mis' }
const configuredPalettes = Object.values(partyColors)

function getPartyPalette(partyListId: string, index: number) {
  return partyColors[partyListId] ?? configuredPalettes[index % configuredPalettes.length]
}

function projectDhondtSeats(groups: PartyResultGroup[], seatCount: number | null): SeatProjection[] {
  const projection = groups.map((group, index) => ({ ...group, seats: 0, palette: getPartyPalette(group.partyListId, index) }))
  if (!seatCount || projection.every((party) => party.voteCount === 0)) return projection

  for (let seat = 0; seat < seatCount; seat += 1) {
    let winnerIndex = 0
    for (let index = 1; index < projection.length; index += 1) {
      const candidate = projection[index]
      const winner = projection[winnerIndex]
      const candidateQuotient = candidate.voteCount / (candidate.seats + 1)
      const winnerQuotient = winner.voteCount / (winner.seats + 1)
      if (candidateQuotient > winnerQuotient || (candidateQuotient === winnerQuotient && candidate.voteCount > winner.voteCount)) winnerIndex = index
    }
    projection[winnerIndex].seats += 1
  }

  return projection
}

function createSeatPositions(seatCount: number): SeatPosition[] {
  if (seatCount <= 0) return []
  const rowCount = seatCount <= 10 ? 1 : Math.min(seatCount, Math.max(3, Math.min(8, Math.ceil(Math.sqrt(seatCount / 3)))))
  const radii = Array.from({ length: rowCount }, (_, index) => rowCount === 1 ? 300 : 185 + index * 245 / (rowCount - 1))
  const totalRadius = radii.reduce((total, radius) => total + radius, 0)
  const capacities = radii.map((radius) => Math.floor(seatCount * radius / totalRadius))
  for (let assigned = capacities.reduce((total, capacity) => total + capacity, 0); assigned < seatCount; assigned += 1) {
    const row = radii.reduce((best, radius, index) => {
      const remainder = seatCount * radius / totalRadius - capacities[index]
      const bestRemainder = seatCount * radii[best] / totalRadius - capacities[best]
      return remainder > bestRemainder ? index : best
    }, 0)
    capacities[row] += 1
  }

  return radii.flatMap((radius, row) => Array.from({ length: capacities[row] }, (_, index) => {
    const angle = capacities[row] === 1 ? Math.PI / 2 : Math.PI - index * Math.PI / (capacities[row] - 1)
    return { x: 500 + radius * Math.cos(angle), y: 475 - radius * Math.sin(angle), angle, radius }
  })).sort((first, second) => second.angle - first.angle || first.radius - second.radius)
}

function useElectionSnapshot() {
  const [snapshot, setSnapshot] = useState<ElectionSnapshot>(emptySnapshot)
  const [schedule, setSchedule] = useState<ElectionSchedule | null>(null)

  useEffect(() => {
    const config = window.__STUDIO_CONFIG__
    if (!config?.electionApiBaseUrl || !config.electionId) {
      console.error('Election studio requires electionApiBaseUrl and electionId runtime configuration')
      return
    }

    const controller = new AbortController()
    const interval = Math.max(1000, config.pollIntervalMs ?? 5000)
    const electionEndpoint = `${config.electionApiBaseUrl.replace(/\/$/, '')}/public/elections/${encodeURIComponent(config.electionId)}`
    const resultsEndpoint = `${electionEndpoint}/results/tabular`
    let timer: number | undefined

    const poll = async () => {
      try {
        const [resultsResponse, electionResponse] = await Promise.all([
          fetch(resultsEndpoint, { signal: controller.signal, cache: 'no-store' }),
          fetch(electionEndpoint, { signal: controller.signal, cache: 'no-store' }),
        ])
        if (!resultsResponse.ok || !electionResponse.ok) throw new Error(`Election API returned results=${resultsResponse.status}, election=${electionResponse.status}`)
        const [next, nextSchedule] = await Promise.all([
          resultsResponse.json() as Promise<ElectionSnapshot>,
          electionResponse.json() as Promise<ElectionSchedule>,
        ])
        if (!Array.isArray(next.rows) || !Array.isArray(next.partyGroups)) throw new Error('Election API returned an invalid result shape')
        if (!nextSchedule.votingStartsAt || !nextSchedule.votingEndsAt) throw new Error('Election API returned an invalid schedule')
        setSnapshot(next)
        setSchedule(nextSchedule)
      } catch (error) {
        if (!controller.signal.aborted) console.error('Could not refresh election results; retaining the last snapshot', error)
      } finally {
        if (!controller.signal.aborted) timer = window.setTimeout(poll, interval)
      }
    }

    void poll()
    return () => {
      controller.abort()
      if (timer) window.clearTimeout(timer)
    }
  }, [])

  return { snapshot, schedule }
}

function getRequestedLocale(): Locale | null {
  const requested = new URLSearchParams(location.search).get('lang')
  return requested && requested in translationData ? requested as Locale : null
}

function getElectionTiming(now: Date, schedule: ElectionSchedule | null) {
  if (!schedule) return { phase: 'upcoming' as const, target: null }
  const electionOpensAt = new Date(schedule.votingStartsAt)
  const electionClosesAt = new Date(schedule.votingEndsAt)
  if (now < electionOpensAt) return { phase: 'upcoming' as const, target: electionOpensAt }
  if (now < electionClosesAt) return { phase: 'open' as const, target: electionClosesAt }
  return { phase: 'closed' as const, target: electionClosesAt }
}

function formatCountdown(target: Date | null, now: Date) {
  if (!target) return '—'
  const totalSeconds = Math.max(0, Math.floor((target.getTime() - now.getTime()) / 1000))
  const days = Math.floor(totalSeconds / 86400)
  const hours = Math.floor(totalSeconds % 86400 / 3600)
  const minutes = Math.floor(totalSeconds % 3600 / 60)
  const seconds = totalSeconds % 60
  return `${days ? `${days}d ` : ''}${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function ShimmerMark({ className = '' }: { className?: string }) {
  return <span className={`shimmer-mark ${className}`} aria-label="MKLU">
    {[...'MKLU'].map((letter, index) => <span key={letter} className="mark-letter" aria-hidden="true" style={{ '--letter-index': index } as CSSProperties}>{letter}</span>)}
  </span>
}

function playCue(audio: HTMLAudioElement | null, volume: number) {
  if (!audio) return
  audio.currentTime = 0
  audio.volume = volume
  void audio.play().catch(() => undefined)
}

function StudioHeader({ now, phase, t, locale }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale }) {
  const currentTime = new Intl.DateTimeFormat(localeTags[locale], { timeZone: 'Europe/Prague', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(now)
  return <header className="studio-header">
    <div className="wordmark"><img className="flag-mark" src="/MKLU-Flag-Small.svg" alt="" /><span><strong>{t('countryName')}</strong><small>{t('serviceName')}</small></span></div>
    <div className={`broadcast-state phase-${phase}`}><span className="live-dot" /> {t(phase)} <span className="divider" /> {t('coverage')}</div>
    <time dateTime={now.toISOString()}>{currentTime} <small>MKL</small></time>
  </header>
}

function TimingCard({ now, phase, t, schedule }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; schedule: ElectionSchedule | null }) {
  const timing = getElectionTiming(now, schedule)
  return <article className={`timing-card phase-${phase}`}><small>{phase === 'upcoming' ? t('opensIn') : phase === 'open' ? t('closesIn') : t('closedAt')}</small><strong>{phase === 'closed' ? '00:00:00' : formatCountdown(timing.target, now)}</strong><span>{t(phase)}</span></article>
}

function IntroScene({ now, phase, t, schedule }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; schedule: ElectionSchedule | null }) {
  const timing = getElectionTiming(now, schedule)
  return <section className="scene intro-scene" aria-label={t('coverage')}>
    <div className="intro-rings" aria-hidden="true"><i /><i /><i /></div>
    <img className="intro-emblem" src="/manticore.svg" alt="" />
    <div className="intro-copy"><p className="kicker">{t('introKicker')}</p><h1>{t('introTitle')}<br /><em>2026</em></h1><div className="intro-rule" /><p className="intro-year">2026</p></div>
    <div className="intro-side"><span>{phase === 'upcoming' ? t('opensIn') : phase === 'open' ? t('closesIn') : t('closedAt')}</span><strong>{phase === 'closed' ? '00:00:00' : formatCountdown(timing.target, now)}</strong><small>{t(phase)}</small></div>
  </section>
}

function OverviewScene({ now, phase, t, locale, snapshot, schedule }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot; schedule: ElectionSchedule | null }) {
  const leader = snapshot.rows[0]
  const turnout = snapshot.turnoutPercentage ?? 0
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  return <section className="scene overview-scene" aria-label={t('overviewTitle')}>
    <div className="scene-heading"><p className="kicker">{t('overviewKicker')}</p><h1>{t('overviewTitle')}</h1></div>
    <div className="overview-grid">
      <article className="leader-panel"><p>{t('leadingList')}</p><span className="rank">01</span><h2>{leader?.selectionLabel ?? '—'}</h2><small>{leader?.partyName ?? ''}</small><div className="leader-total"><strong>{(leader?.percentage ?? 0).toFixed(1)}%</strong><span>{formatNumber(leader?.voteCount ?? 0)} {t('votes')}</span></div></article>
      <div className="overview-stats">
        <TimingCard now={now} phase={phase} t={t} schedule={schedule} />
        <article><small>{t('ballotsCounted')}</small><strong>{formatNumber(snapshot.totalValidBallots)}</strong><span>{t('estimatedTotal')}</span></article>
        <article><small>{t('turnout')}</small><strong>{turnout.toFixed(1)}%</strong><span>{formatNumber(snapshot.participatingVoters)} {t('participating')}</span></article>
        <article><small>{t('listsRegistered')}</small><strong>{snapshot.rows.length}</strong><span>{t('across')} {snapshot.rows[0]?.territoryCode ?? 'MKLU'}</span></article>
      </div>
    </div>
  </section>
}

function PartiesScene({ t, snapshot }: { t: (key: TranslationKey) => string; snapshot: ElectionSnapshot }) {
  return <section className="scene parties-scene" aria-label={t('partyTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('partyKicker')}</p><h1>{t('partyTitle')}</h1></div>
    <div className="party-grid">{snapshot.rows.map((row, index) => {
      const palette = getPartyPalette(row.selectionId, index)
      return <article className="party-card" key={row.selectionId} style={{ '--party-primary': palette.primary, '--party-secondary': palette.secondary, '--delay': `${index * 100}ms` } as CSSProperties}>
        <div className="party-visual" aria-hidden="true"><i /><i /><span>{String(index + 1).padStart(2, '0')}</span></div>
        <small>{t('listNumber')} {String(index + 1).padStart(2, '0')}</small><h2>{row.selectionLabel}</h2><p>{row.partyName}</p>
      </article>
    })}</div>
  </section>
}

function CandidatesScene({ t, snapshot }: { t: (key: TranslationKey) => string; snapshot: ElectionSnapshot }) {
  return <section className="scene candidates-scene" aria-label={t('candidateTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('candidateKicker')}</p><h1>{t('candidateTitle')}</h1></div>
    <div className="candidate-grid">{snapshot.partyGroups.map((group, index) => {
      const palette = getPartyPalette(group.partyListId, index)
      return <article className="candidate-card" key={group.partyListId} style={{ '--party-primary': palette.primary, '--party-secondary': palette.secondary, '--delay': `${index * 100}ms` } as CSSProperties}>
        <header><span>{String(index + 1).padStart(2, '0')}</span><div><h2>{group.listName}</h2><p>{group.partyName}</p></div></header>
        <ol>{group.candidates.map((candidate) => <li className={`${candidate.isWithdrawn ? 'withdrawn' : ''} ${candidate.isWinner ? 'winner' : ''}`} key={candidate.id}>
          <span>{String(candidate.position + 1).padStart(2, '0')}</span><strong>{candidate.displayName}</strong>
          {candidate.isWinner && <small>{t('elected')}</small>}{candidate.isWithdrawn && <small>{t('withdrawn')}</small>}
        </li>)}</ol>
      </article>
    })}</div>
  </section>
}

function ResultsScene({ t, locale, snapshot }: { t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot }) {
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  return <section className="scene results-scene" aria-label={t('resultsTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('resultsKicker')}</p><h1>{t('resultsTitle')}</h1></div>
    <div className="result-board">{snapshot.rows.map((row, index) =>
      <article className="result-item" key={row.selectionId} style={{ '--delay': `${index * 110}ms` } as CSSProperties}>
        <span className="result-rank">{String(index + 1).padStart(2, '0')}</span><div className="result-name"><strong>{row.selectionLabel}</strong><small>{row.partyName}</small></div>
        <div className="bar-track"><i style={{ width: `${row.percentage}%`, background: getPartyPalette(row.selectionId, index).primary }} /></div><strong className="result-percent">{row.percentage.toFixed(1)}<small>%</small></strong><span className="result-votes">{formatNumber(row.voteCount)}</span>
      </article>)}</div>
  </section>
}

function SeatsScene({ t, snapshot }: { t: (key: TranslationKey) => string; snapshot: ElectionSnapshot }) {
  const projection = projectDhondtSeats(snapshot.partyGroups, snapshot.seatCount)
  const hasProjection = projection.some((party) => party.seats > 0)
  const seatParties = projection.flatMap((party) => Array.from({ length: party.seats }, () => party))
  const positions = createSeatPositions(snapshot.seatCount ?? 0)
  const seatRadius = positions.length <= 10 ? 34 : Math.max(6, Math.min(13, 92 / Math.sqrt(positions.length / 10)))

  return <section className="scene seats-scene" aria-label={t('seatsTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('seatsKicker')}</p><h1>{t('seatsTitle')}</h1></div>
    <div className="seats-layout">
      <div className="hemicycle-wrap">
        {positions.length > 0 ? <svg className="hemicycle" viewBox="0 0 1000 510" role="img" aria-label={`${snapshot.seatCount} ${t('seats')}`}>
          <path d="M 45 475 A 455 455 0 0 1 955 475" />
          {positions.map((position, index) => <circle className={seatParties[index] ? '' : 'unallocated'} key={`${seatParties[index]?.partyListId ?? 'unallocated'}-${index}`} cx={position.x} cy={position.y} r={seatRadius} fill={seatParties[index]?.palette.primary ?? '#687278'} style={{ '--delay': `${index * 45}ms` } as CSSProperties} />)}
        </svg> : <div className="projection-empty">{t('projectionUnavailable')}</div>}
        {!hasProjection && positions.length > 0 && <p className="projection-note">{t('projectionUnavailable')}</p>}
        <div className="seat-total"><strong>{snapshot.seatCount ?? '—'}</strong><span>{t('seats')}</span></div>
      </div>
      <div className="seat-legend">{projection.map((party) => <article key={party.partyListId} style={{ '--party-primary': party.palette.primary } as CSSProperties}>
        <i /><div><strong>{party.listName}</strong><small>{party.percentage.toFixed(1)}%</small></div><b>{party.seats}</b>
      </article>)}</div>
    </div>
  </section>
}

function TurnoutScene({ t, locale, snapshot }: { t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot }) {
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  const turnout = snapshot.turnoutPercentage ?? 0
  const circumference = 2 * Math.PI * 164
  const dash = circumference * turnout / 100
  return <section className="scene turnout-scene" aria-label={t('turnoutTitle')}>
    <div className="scene-heading"><p className="kicker">{t('turnoutKicker')}</p><h1>{t('turnoutTitle')}</h1></div>
    <div className="turnout-layout"><div className="turnout-dial"><svg viewBox="0 0 380 380" aria-hidden="true"><circle className="dial-back" cx="190" cy="190" r="164" /><circle className="dial-value" cx="190" cy="190" r="164" strokeDasharray={`${dash} ${circumference}`} /></svg><div><strong>{turnout.toFixed(1)}<small>%</small></strong><span>{t('estimatedTurnout')}</span></div></div>
      <dl className="turnout-facts"><div><dt>{t('eligibleVoters')}</dt><dd>{formatNumber(snapshot.eligibleVoters ?? 0)}</dd></div><div><dt>{t('participating')}</dt><dd>{formatNumber(snapshot.participatingVoters)}</dd></div><div><dt>{t('validBallots')}</dt><dd>{formatNumber(snapshot.totalValidBallots)}</dd></div></dl></div>
  </section>
}

function Scene({ name, now, phase, t, locale, snapshot, schedule }: { name: SceneName; now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot; schedule: ElectionSchedule | null }) {
  if (name === 'intro') return <IntroScene now={now} phase={phase} t={t} schedule={schedule} />
  if (name === 'overview') return <OverviewScene now={now} phase={phase} t={t} locale={locale} snapshot={snapshot} schedule={schedule} />
  if (name === 'parties') return <PartiesScene t={t} snapshot={snapshot} />
  if (name === 'candidates') return <CandidatesScene t={t} snapshot={snapshot} />
  if (name === 'results') return <ResultsScene t={t} locale={locale} snapshot={snapshot} />
  if (name === 'seats') return <SeatsScene t={t} snapshot={snapshot} />
  return <TurnoutScene t={t} locale={locale} snapshot={snapshot} />
}

function App() {
  const { snapshot, schedule } = useElectionSnapshot()
  const requestedLocale = getRequestedLocale()
  const [localeIndex, setLocaleIndex] = useState(requestedLocale ? localeOrder.indexOf(requestedLocale) : 0)
  const locale = localeOrder[localeIndex]
  const t = (key: TranslationKey) => translationData[locale][key]
  const requestedScene = new URLSearchParams(location.search).get('scene') as SceneName | null
  const fixedScene = requestedScene && sceneNames.includes(requestedScene) ? requestedScene : null
  const [sceneIndex, setSceneIndex] = useState(fixedScene ? sceneNames.indexOf(fixedScene) : 0)
  const [transitioning, setTransitioning] = useState(false)
  const [now, setNow] = useState(() => new Date())
  const whooshRef = useRef<HTMLAudioElement>(null)
  const titleThemeRef = useRef<HTMLAudioElement>(null)

  const scene = sceneNames[sceneIndex]
  const { phase } = getElectionTiming(now, schedule)
  const updatedAt = snapshot.generatedAt ? new Date(snapshot.generatedAt) : now

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    if (scene === 'intro') playCue(titleThemeRef.current, .228)
  }, [scene])

  useEffect(() => {
    if (fixedScene) return
    let transitionTimer: number | undefined
    const timer = window.setInterval(() => {
      playCue(whooshRef.current, .18)
      setTransitioning(true)
      transitionTimer = window.setTimeout(() => {
        setSceneIndex((current) => {
          const next = (current + 1) % sceneNames.length
          if (next === 0 && !requestedLocale) setLocaleIndex((locale) => (locale + 1) % localeOrder.length)
          return next
        })
        setTransitioning(false)
      }, 850)
    }, sceneDuration)
    return () => {
      window.clearInterval(timer)
      if (transitionTimer) window.clearTimeout(transitionTimer)
    }
  }, [fixedScene, requestedLocale])

  return <main className={`studio scene-${scene}`} onPointerDown={() => {
    if (scene === 'intro' && titleThemeRef.current?.paused) playCue(titleThemeRef.current, .228)
  }}>
    <audio ref={whooshRef} src="/audio/transition-whoosh.mp3" preload="auto" />
    <audio ref={titleThemeRef} src="/audio/title-theme.mp3" preload="auto" />
    <div className="set set-back" aria-hidden="true" /><div className="set set-floor" aria-hidden="true" /><div className="set set-beam beam-one" aria-hidden="true" /><div className="set set-beam beam-two" aria-hidden="true" />
    <StudioHeader now={now} phase={phase} t={t} locale={locale} /><div className="camera"><Scene name={scene} now={now} phase={phase} t={t} locale={locale} snapshot={snapshot} schedule={schedule} /></div>
    <footer className="studio-footer"><span>{t('preliminary')}</span><p>{t('disclaimer')}</p><time dateTime={updatedAt.toISOString()}>{t('updated')} {new Intl.DateTimeFormat(localeTags[locale], { timeZone: 'Europe/Prague', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(updatedAt)}</time></footer>
    <div className={`transition-wipe ${transitioning ? 'active' : ''}`} aria-hidden="true"><img src="/manticore.svg" alt="" /><ShimmerMark className="transition-mark" /></div>
  </main>
}

export default App
