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
  selectionType: 'PartyList'
  partyName: string
  voteCount: number
  percentage: number
  territoryCode: string
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
  status: 'Published'
  totalValidBallots: number
  participatingVoters: number
  eligibleVoters: number
  turnoutPercentage: number
  isLive: boolean
  generatedAt: string
  seatCount: number
  winnerCount: number
  rows: ResultRow[]
  partyGroups: PartyResultGroup[]
}

const snapshot: ElectionSnapshot = {
  electionId: '8bb98af5-8717-4cf7-929c-f41d49c22508', title: 'Federal Assembly Election 2026', status: 'Published',
  totalValidBallots: 18642, participatingVoters: 18976, eligibleVoters: 26740, turnoutPercentage: 70.97,
  isLive: true, generatedAt: '2026-07-31T18:42:16Z', seatCount: 48, winnerCount: 0,
  rows: [
    { selectionId: '1', selectionLabel: 'Common Ground', selectionType: 'PartyList', partyName: 'Common Ground Movement', voteCount: 6834, percentage: 36.66, territoryCode: 'MKLU' },
    { selectionId: '2', selectionLabel: 'Forward Together', selectionType: 'PartyList', partyName: 'Civic Alliance', voteCount: 5241, percentage: 28.11, territoryCode: 'MKLU' },
    { selectionId: '3', selectionLabel: 'Open Horizon', selectionType: 'PartyList', partyName: 'Lurkish Social Forum', voteCount: 3798, percentage: 20.37, territoryCode: 'MKLU' },
    { selectionId: '4', selectionLabel: 'Local Voice', selectionType: 'PartyList', partyName: 'Mekniyan Provinces', voteCount: 1891, percentage: 10.14, territoryCode: 'MKLU' },
    { selectionId: '5', selectionLabel: 'New Page', selectionType: 'PartyList', partyName: 'Independent List', voteCount: 878, percentage: 4.71, territoryCode: 'MKLU' },
  ],
  partyGroups: [
    { partyListId: '1', partyName: 'Common Ground Movement', listName: 'Common Ground', voteCount: 6834, percentage: 36.66, candidates: [
      { id: '1-1', displayName: 'Mara Velen', position: 0, isWithdrawn: false, isWinner: false },
      { id: '1-2', displayName: 'Jonas Rhee', position: 1, isWithdrawn: false, isWinner: false },
      { id: '1-3', displayName: 'Elin Sora', position: 2, isWithdrawn: false, isWinner: false },
    ] },
    { partyListId: '2', partyName: 'Civic Alliance', listName: 'Forward Together', voteCount: 5241, percentage: 28.11, candidates: [
      { id: '2-1', displayName: 'Tomas Han', position: 0, isWithdrawn: false, isWinner: false },
      { id: '2-2', displayName: 'Nika Belan', position: 1, isWithdrawn: false, isWinner: false },
      { id: '2-3', displayName: 'Karel Dae', position: 2, isWithdrawn: false, isWinner: false },
    ] },
    { partyListId: '3', partyName: 'Lurkish Social Forum', listName: 'Open Horizon', voteCount: 3798, percentage: 20.37, candidates: [
      { id: '3-1', displayName: 'Yuna Mek', position: 0, isWithdrawn: false, isWinner: false },
      { id: '3-2', displayName: 'Pavel Orin', position: 1, isWithdrawn: false, isWinner: false },
      { id: '3-3', displayName: 'Sena Vol', position: 2, isWithdrawn: false, isWinner: false },
    ] },
    { partyListId: '4', partyName: 'Mekniyan Provinces', listName: 'Local Voice', voteCount: 1891, percentage: 10.14, candidates: [
      { id: '4-1', displayName: 'Lenka Mir', position: 0, isWithdrawn: false, isWinner: false },
      { id: '4-2', displayName: 'Davin Kor', position: 1, isWithdrawn: false, isWinner: false },
      { id: '4-3', displayName: 'Irena Sol', position: 2, isWithdrawn: false, isWinner: false },
    ] },
    { partyListId: '5', partyName: 'Independent List', listName: 'New Page', voteCount: 878, percentage: 4.71, candidates: [
      { id: '5-1', displayName: 'Noa Varek', position: 0, isWithdrawn: false, isWinner: false },
      { id: '5-2', displayName: 'Mila Ren', position: 1, isWithdrawn: false, isWinner: false },
      { id: '5-3', displayName: 'Adam Seon', position: 2, isWithdrawn: false, isWinner: false },
    ] },
  ],
}

const electionOpensAt = new Date('2026-08-01T10:00:00+02:00')
const electionClosesAt = new Date('2026-08-02T14:00:00+02:00')
const partyColors = partyColorData as Record<string, PartyPalette>
const sceneNames = ['intro', 'overview', 'parties', 'candidates', 'results', 'turnout'] as const
const localeOrder: Locale[] = ['en', 'cs', 'mis']
type SceneName = (typeof sceneNames)[number]
const sceneDuration = 18000
const localeTags: Record<Locale, string> = { en: 'en-GB', cs: 'cs-CZ', mis: 'mis' }

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

function OverviewScene({ now, phase, t, locale }: { now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale }) {
  const leader = snapshot.rows[0]
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  return <section className="scene overview-scene" aria-label={t('overviewTitle')}>
    <div className="scene-heading"><p className="kicker">{t('overviewKicker')}</p><h1>{t('overviewTitle')}</h1></div>
    <div className="overview-grid">
      <article className="leader-panel"><p>{t('leadingList')}</p><span className="rank">01</span><h2>{leader.selectionLabel}</h2><small>{leader.partyName}</small><div className="leader-total"><strong>{leader.percentage.toFixed(1)}%</strong><span>{formatNumber(leader.voteCount)} {t('votes')}</span></div></article>
      <div className="overview-stats">
        <TimingCard now={now} phase={phase} t={t} />
        <article><small>{t('ballotsCounted')}</small><strong>{formatNumber(snapshot.totalValidBallots)}</strong><span>{t('estimatedTotal')}</span></article>
        <article><small>{t('turnout')}</small><strong>{snapshot.turnoutPercentage.toFixed(1)}%</strong><span>{formatNumber(snapshot.participatingVoters)} {t('participating')}</span></article>
        <article><small>{t('listsRegistered')}</small><strong>{snapshot.rows.length}</strong><span>{t('across')} {snapshot.rows[0].territoryCode}</span></article>
      </div>
    </div>
  </section>
}

function PartiesScene({ t }: { t: (key: TranslationKey) => string }) {
  return <section className="scene parties-scene" aria-label={t('partyTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('partyKicker')}</p><h1>{t('partyTitle')}</h1></div>
    <div className="party-grid">{snapshot.rows.map((row, index) => {
      const palette = partyColors[row.selectionId]
      return <article className="party-card" key={row.selectionId} style={{ '--party-primary': palette.primary, '--party-secondary': palette.secondary, '--delay': `${index * 100}ms` } as CSSProperties}>
        <div className="party-visual" aria-hidden="true"><i /><i /><span>{String(index + 1).padStart(2, '0')}</span></div>
        <small>{t('listNumber')} {String(index + 1).padStart(2, '0')}</small><h2>{row.selectionLabel}</h2><p>{row.partyName}</p>
      </article>
    })}</div>
  </section>
}

function CandidatesScene({ t }: { t: (key: TranslationKey) => string }) {
  return <section className="scene candidates-scene" aria-label={t('candidateTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('candidateKicker')}</p><h1>{t('candidateTitle')}</h1></div>
    <div className="candidate-grid">{snapshot.partyGroups.map((group, index) => {
      const palette = partyColors[group.partyListId]
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

function ResultsScene({ t, locale }: { t: (key: TranslationKey) => string; locale: Locale }) {
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  return <section className="scene results-scene" aria-label={t('resultsTitle')}>
    <div className="scene-heading compact"><p className="kicker">{t('resultsKicker')}</p><h1>{t('resultsTitle')}</h1></div>
    <div className="result-board">{snapshot.rows.map((row, index) =>
      <article className="result-item" key={row.selectionId} style={{ '--delay': `${index * 110}ms` } as CSSProperties}>
        <span className="result-rank">{String(index + 1).padStart(2, '0')}</span><div className="result-name"><strong>{row.selectionLabel}</strong><small>{row.partyName}</small></div>
        <div className="bar-track"><i style={{ width: `${row.percentage}%`, background: partyColors[row.selectionId].primary }} /></div><strong className="result-percent">{row.percentage.toFixed(1)}<small>%</small></strong><span className="result-votes">{formatNumber(row.voteCount)}</span>
      </article>)}</div>
  </section>
}

function TurnoutScene({ t, locale }: { t: (key: TranslationKey) => string; locale: Locale }) {
  const formatNumber = (value: number) => new Intl.NumberFormat(localeTags[locale]).format(value)
  const circumference = 2 * Math.PI * 164
  const dash = circumference * snapshot.turnoutPercentage / 100
  return <section className="scene turnout-scene" aria-label={t('turnoutTitle')}>
    <div className="scene-heading"><p className="kicker">{t('turnoutKicker')}</p><h1>{t('turnoutTitle')}</h1></div>
    <div className="turnout-layout"><div className="turnout-dial"><svg viewBox="0 0 380 380" aria-hidden="true"><circle className="dial-back" cx="190" cy="190" r="164" /><circle className="dial-value" cx="190" cy="190" r="164" strokeDasharray={`${dash} ${circumference}`} /></svg><div><strong>{snapshot.turnoutPercentage.toFixed(1)}<small>%</small></strong><span>{t('estimatedTurnout')}</span></div></div>
      <dl className="turnout-facts"><div><dt>{t('eligibleVoters')}</dt><dd>{formatNumber(snapshot.eligibleVoters)}</dd></div><div><dt>{t('participating')}</dt><dd>{formatNumber(snapshot.participatingVoters)}</dd></div><div><dt>{t('validBallots')}</dt><dd>{formatNumber(snapshot.totalValidBallots)}</dd></div></dl></div>
  </section>
}

function Scene({ name, now, phase, t, locale }: { name: SceneName; now: Date; phase: ElectionPhase; t: (key: TranslationKey) => string; locale: Locale }) {
  if (name === 'intro') return <IntroScene now={now} phase={phase} t={t} />
  if (name === 'overview') return <OverviewScene now={now} phase={phase} t={t} locale={locale} />
  if (name === 'parties') return <PartiesScene t={t} />
  if (name === 'candidates') return <CandidatesScene t={t} />
  if (name === 'results') return <ResultsScene t={t} locale={locale} />
  return <TurnoutScene t={t} locale={locale} />
}

function App() {
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
    <StudioHeader now={now} phase={phase} t={t} locale={locale} /><div className="camera"><Scene name={scene} now={now} phase={phase} t={t} locale={locale} /></div>
    <footer className="studio-footer"><span>{t('preliminary')}</span><p>{t('disclaimer')}</p><time dateTime={now.toISOString()}>{t('updated')} {new Intl.DateTimeFormat(localeTags[locale], { timeZone: 'Europe/Prague', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(now)}</time></footer>
    <div className={`transition-wipe ${transitioning ? 'active' : ''}`} aria-hidden="true"><img src="/manticore.svg" alt="" /><ShimmerMark className="transition-mark" /></div>
  </main>
}

export default App
