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

declare global {
  interface Window { __STUDIO_CONFIG__?: StudioConfig }
}

const emptySnapshot: ElectionSnapshot = {
  electionId: '', title: '', status: '', totalValidBallots: 0, participatingVoters: 0,
  eligibleVoters: null, turnoutPercentage: null, isLive: false, generatedAt: '', seatCount: null,
  winnerCount: 0, rows: [], partyGroups: [],
}

const electionOpensAt = new Date('2026-08-01T10:00:00+02:00')
const electionClosesAt = new Date('2026-08-02T14:00:00+02:00')
const partyColors = partyColorData as Record<string, PartyPalette>
const sceneNames = ['intro', 'overview', 'parties', 'candidates', 'results', 'turnout'] as const
const localeOrder: Locale[] = ['en', 'cs', 'mis']
type SceneName = (typeof sceneNames)[number]
const sceneDuration = 18000
const localeTags: Record<Locale, string> = { en: 'en-GB', cs: 'cs-CZ', mis: 'mis' }
const configuredPalettes = Object.values(partyColors)

function getPartyPalette(partyListId: string, index: number) {
  return partyColors[partyListId] ?? configuredPalettes[index % configuredPalettes.length]
}

function useElectionSnapshot() {
  const [snapshot, setSnapshot] = useState<ElectionSnapshot>(emptySnapshot)

  useEffect(() => {
    const config = window.__STUDIO_CONFIG__
    if (!config?.electionApiBaseUrl || !config.electionId) {
      console.error('Election studio requires electionApiBaseUrl and electionId runtime configuration')
      return
    }

    const controller = new AbortController()
    const interval = Math.max(1000, config.pollIntervalMs ?? 5000)
    const endpoint = `${config.electionApiBaseUrl.replace(/\/$/, '')}/public/elections/${encodeURIComponent(config.electionId)}/results/tabular`
    let timer: number | undefined

    const poll = async () => {
      try {
        const response = await fetch(endpoint, { signal: controller.signal, cache: 'no-store' })
        if (!response.ok) throw new Error(`Election API returned ${response.status}`)
        const next = await response.json() as ElectionSnapshot
        if (!Array.isArray(next.rows) || !Array.isArray(next.partyGroups)) throw new Error('Election API returned an invalid result shape')
        setSnapshot(next)
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

  return snapshot
}

function getRequestedLocale(): Locale | null {
  const requested = new URLSearchParams(location.search).get('lang')
  return requested && requested in translationData ? requested as Locale : null
}

function getElectionTiming(now: Date) {
  if (now < electionOpensAt) return { phase: 'upcoming' as const, target: electionOpensAt }
  if (now < electionClosesAt) return { phase: 'open' as const, target: electionClosesAt }
  return { phase: 'closed' as const, target: electionClosesAt }
}

function formatCountdown(target: Date, now: Date) {
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

function TimingCard({ now, phase, t }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string }) {
  const timing = getElectionTiming(now)
  return <article className={`timing-card phase-${phase}`}><small>{phase === 'upcoming' ? t('opensIn') : phase === 'open' ? t('closesIn') : t('closedAt')}</small><strong>{phase === 'closed' ? '00:00:00' : formatCountdown(timing.target, now)}</strong><span>{t(phase)}</span></article>
}

function IntroScene({ now, phase, t }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string }) {
  const timing = getElectionTiming(now)
  return <section className="scene intro-scene" aria-label={t('coverage')}>
    <div className="intro-rings" aria-hidden="true"><i /><i /><i /></div>
    <img className="intro-emblem" src="/manticore.svg" alt="" />
    <div className="intro-copy"><p className="kicker">{t('introKicker')}</p><h1>{t('introTitle')}<br /><em>2026</em></h1><div className="intro-rule" /><p className="intro-year">2026</p></div>
    <div className="intro-side"><span>{phase === 'upcoming' ? t('opensIn') : phase === 'open' ? t('closesIn') : t('closedAt')}</span><strong>{phase === 'closed' ? '00:00:00' : formatCountdown(timing.target, now)}</strong><small>{t(phase)}</small></div>
  </section>
}

function OverviewScene({ now, phase, t, locale, snapshot }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot }) {
  const leader = snapshot.rows[0]
  const turnout = snapshot.turnoutPercentage ?? 0
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  return <section className="scene overview-scene" aria-label={t('overviewTitle')}>
    <div className="scene-heading"><p className="kicker">{t('overviewKicker')}</p><h1>{t('overviewTitle')}</h1></div>
    <div className="overview-grid">
      <article className="leader-panel"><p>{t('leadingList')}</p><span className="rank">01</span><h2>{leader?.selectionLabel ?? '—'}</h2><small>{leader?.partyName ?? ''}</small><div className="leader-total"><strong>{(leader?.percentage ?? 0).toFixed(1)}%</strong><span>{formatNumber(leader?.voteCount ?? 0)} {t('votes')}</span></div></article>
      <div className="overview-stats">
        <TimingCard now={now} phase={phase} t={t} />
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

function Scene({ name, now, phase, t, locale, snapshot }: { name: SceneName; now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale; snapshot: ElectionSnapshot }) {
  if (name === 'intro') return <IntroScene now={now} phase={phase} t={t} />
  if (name === 'overview') return <OverviewScene now={now} phase={phase} t={t} locale={locale} snapshot={snapshot} />
  if (name === 'parties') return <PartiesScene t={t} snapshot={snapshot} />
  if (name === 'candidates') return <CandidatesScene t={t} snapshot={snapshot} />
  if (name === 'results') return <ResultsScene t={t} locale={locale} snapshot={snapshot} />
  return <TurnoutScene t={t} locale={locale} snapshot={snapshot} />
}

function App() {
  const snapshot = useElectionSnapshot()
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
  const { phase } = getElectionTiming(now)
  const updatedAt = snapshot.generatedAt ? new Date(snapshot.generatedAt) : now

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    if (scene === 'intro') playCue(titleThemeRef.current, .38)
  }, [scene])

  useEffect(() => {
    if (fixedScene) return
    let transitionTimer: number | undefined
    const timer = window.setInterval(() => {
      playCue(whooshRef.current, .3)
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
    if (scene === 'intro' && titleThemeRef.current?.paused) playCue(titleThemeRef.current, .38)
  }}>
    <audio ref={whooshRef} src="/audio/transition-whoosh.mp3" preload="auto" />
    <audio ref={titleThemeRef} src="/audio/title-theme.mp3" preload="auto" />
    <div className="set set-back" aria-hidden="true" /><div className="set set-floor" aria-hidden="true" /><div className="set set-beam beam-one" aria-hidden="true" /><div className="set set-beam beam-two" aria-hidden="true" />
    <StudioHeader now={now} phase={phase} t={t} locale={locale} /><div className="camera"><Scene name={scene} now={now} phase={phase} t={t} locale={locale} snapshot={snapshot} /></div>
    <footer className="studio-footer"><span>{t('preliminary')}</span><p>{t('disclaimer')}</p><time dateTime={updatedAt.toISOString()}>{t('updated')} {new Intl.DateTimeFormat(localeTags[locale], { timeZone: 'Europe/Prague', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(updatedAt)}</time></footer>
    <div className={`transition-wipe ${transitioning ? 'active' : ''}`} aria-hidden="true"><img src="/manticore.svg" alt="" /><ShimmerMark className="transition-mark" /></div>
  </main>
}

export default App
