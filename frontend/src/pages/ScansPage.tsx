import { useCallback, useEffect, useMemo, useState } from 'react'
import { api, ApiError } from '../api'
import type { ScanDetails, ScanSummary, Target } from '../types'
import MovingLoaderOverlay from '../components/MovingLoaderOverlay'
import styles from './ScansPage.module.css'

const active = (scan: ScanSummary) => scan.status === 'Queued' || scan.status === 'Running'
const apiBase = import.meta.env.VITE_API_BASE_URL ?? '/api'
const cancelErrorMessage = 'The scan could not be cancelled.'

function scanIsPersistedAsCancelled(history: ScanSummary[], id: string | null) {
  return !!id && history.some(scan => scan.id === id && scan.status === 'Cancelled')
}

export default function ScansPage() {
  const [targets, setTargets] = useState<Target[]>([])
  const [scans, setScans] = useState<ScanSummary[]>([])
  const [selectedTarget, setSelectedTarget] = useState('')
  const [selectedScan, setSelectedScan] = useState<string | null>(null)
  const [details, setDetails] = useState<ScanDetails | null>(null)
  const [activeId, setActiveId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const selected = targets.find(x => x.id === selectedTarget)
  const running = useMemo(() => scans.find(x => x.id === activeId), [scans, activeId])

  const refresh = useCallback(async () => {
    const history = await api.scans()
    setScans(history)
    const current = history.find(active)
    setActiveId(current?.id ?? null)
    setError(previous =>
      scanIsPersistedAsCancelled(history, activeId) && previous === cancelErrorMessage ? '' : previous,
    )
    return history
  }, [activeId])

  useEffect(() => {
    let live = true

    Promise.all([api.targets(), api.scans()])
      .then(([all, history]) => {
        if (!live) return
        const enabled = all.filter(x => x.isEnabled)
        setTargets(enabled)
        setSelectedTarget(enabled[0]?.id ?? '')
        setScans(history)
        setActiveId(history.find(active)?.id ?? null)
      })
      .catch(() => {
        if (live) setError('Scanner information could not be loaded. Confirm that the API and database are running.')
      })
      .finally(() => {
        if (live) setLoading(false)
      })

    return () => {
      live = false
    }
  }, [])

  useEffect(() => {
    if (!activeId) return
    const timer = window.setInterval(() => {
      void refresh()
        .then(() => (selectedScan ? api.scan(selectedScan).then(setDetails) : undefined))
        .catch(() => setError('Scan status could not be refreshed.'))
    }, 1000)
    return () => window.clearInterval(timer)
  }, [activeId, refresh, selectedScan])

  useEffect(() => {
    if (!selectedScan) return
    let live = true
    api.scan(selectedScan)
      .then(value => {
        if (live) setDetails(value)
      })
      .catch(() => {
        if (live) setError('Scan details could not be loaded.')
      })
    return () => {
      live = false
    }
  }, [selectedScan])

  async function start() {
    if (!selectedTarget) return
    setError('')
    try {
      const result = await api.startScan(selectedTarget)
      setActiveId(result.id)
      setSelectedScan(result.id)
      await refresh()
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'The scan could not be started.')
    }
  }

  async function cancel() {
    if (!activeId) return
    const cancellingId = activeId
    setError('')

    try {
      const result = await api.cancelScan(cancellingId)
      setScans(previous => previous.map(scan => (scan.id === result.id ? result : scan)))
      if (result.status === 'Cancelled') setActiveId(null)
      const history = await refresh()
      if (scanIsPersistedAsCancelled(history, cancellingId)) setError('')
    } catch (reason) {
      try {
        const history = await refresh()
        if (scanIsPersistedAsCancelled(history, cancellingId)) {
          setError('')
          return
        }
      } catch {
        // Keep the original cancellation error below if refresh cannot verify the persisted state.
      }
      setError(reason instanceof ApiError ? reason.message : cancelErrorMessage)
    }
  }

  async function openScan(id: string) {
    setSelectedScan(id)
    setDetails(null)
    try {
      setDetails(await api.scan(id))
    } catch {
      setError('Scan details could not be loaded.')
    }
  }

  return (
    <div className="stack">
      <div>
        <h1 className="page-title">Safe scanner</h1>
        <p className="page-description">
          Inspect one authorized starting page without clicking controls, submitting forms, entering data, or crawling links.
        </p>
      </div>

      {error && (
        <div className="alert-error" role="alert">
          {error}
        </div>
      )}

      <section className={`card ${styles.launch}`} aria-labelledby="start-scan">
        <div>
          <h2 id="start-scan">Start a one-page scan</h2>
          <p>Only enabled targets are available.</p>
        </div>
        {loading ? (
          <p role="status">Loading targets…</p>
        ) : targets.length === 0 ? (
          <div className="alert-info">There are no enabled targets. Enable a target on the Targets page first.</div>
        ) : (
          <div className={styles.controls}>
            <label className="field">
              <span className="field-label">Authorized target</span>
              <select className="select" value={selectedTarget} onChange={e => setSelectedTarget(e.target.value)}>
                {targets.map(t => (
                  <option key={t.id} value={t.id}>
                    {t.name} · {t.environment}
                  </option>
                ))}
              </select>
            </label>
            <button className="button-primary" onClick={() => void start()} disabled={!!activeId}>
              Start safe scan
            </button>
          </div>
        )}
        {selected && (
          <dl className={styles.targetInfo}>
            <div>
              <dt>Starting URL</dt>
              <dd>{selected.startingUrl}</dd>
            </div>
            <div>
              <dt>Allowed host</dt>
              <dd>
                <code>{selected.allowedHost}</code>
              </dd>
            </div>
            <div>
              <dt>Environment</dt>
              <dd>{selected.environment}</dd>
            </div>
          </dl>
        )}
      </section>

      <section className="card">
        <h2 className="section-heading">Recent scans</h2>
        {scans.length === 0 ? (
          <div className="empty-state">
            <p>No scans have been requested yet.</p>
          </div>
        ) : (
          <div className={`table-container ${styles.recentTableWrap}`}>
            <table className={`data-table ${styles.recentTable}`}>
              <thead>
                <tr>
                  <th>Target</th>
                  <th>Status</th>
                  <th>Requested</th>
                  <th>Duration</th>
                  <th>Final URL</th>
                  <th className={styles.metricHeading}>Pages</th>
                  <th className={styles.metricHeading}>Elements</th>
                  <th className={styles.metricHeading}>Warnings</th>
                  <th className={styles.metricHeading}>Errors</th>
                </tr>
              </thead>
              <tbody>
                {scans.map(scan => (
                  <tr key={scan.id}>
                    <td>
                      <button className={styles.linkButton} onClick={() => void openScan(scan.id)}>
                        {scan.targetName}
                      </button>
                    </td>
                    <td>
                      <span className={`status-badge ${statusClass(scan.status)}`}>{scan.status}</span>
                      <small className={styles.stageSmall}>{scan.stage}</small>
                    </td>
                    <td>{formatDate(scan.requestedAtUtc)}</td>
                    <td>{duration(scan)}</td>
                    <td className={styles.url}>{scan.finalUrl ?? '—'}</td>
                    <td className={styles.metricCell}>{scan.detectedPageCount}</td>
                    <td className={styles.metricCell}>{scan.detectedElementCount}</td>
                    <td className={styles.metricCell}>{scan.warningCount}</td>
                    <td className={styles.metricCell}>{scan.errorCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {details && <ScanDetail details={details} />}
      <MovingLoaderOverlay open={!!running} stage={running?.stage ?? 'Preparing scan'} onCancel={() => void cancel()} />
    </div>
  )
}

function ScanDetail({ details }: { details: ScanDetails }) {
  return (
    <section className={`card ${styles.details}`}>
      <div className={styles.detailHeading}>
        <div>
          <span className={`status-badge ${statusClass(details.summary.status)}`}>{details.summary.status}</span>
          <h2>{details.summary.pageTitle || details.summary.targetName}</h2>
          <p>{details.summary.finalUrl || details.summary.startingUrl}</p>
        </div>
      </div>
      {details.summary.failureSummary && <div className="alert-error">{details.summary.failureSummary}</div>}
      {details.pages.map(page => (
        <article key={page.id} className={styles.page}>
          <div className={styles.pageOverview}>
            {page.hasThumbnail && (
              <img src={`${apiBase}/scans/pages/${page.id}/thumbnail`} alt={`Thumbnail of ${page.displayName}`} />
            )}
            <div>
              <h3>{page.displayName}</h3>
              {page.mainHeading && <p>Main heading: {page.mainHeading}</p>}
              <p>
                {page.elements.length} detected elements · {page.screenshotWidth} × {page.screenshotHeight}
              </p>
              {page.hasScreenshot && (
                <a
                  className="button-secondary"
                  href={`${apiBase}/scans/pages/${page.id}/screenshot`}
                  target="_blank"
                  rel="noreferrer"
                >
                  View full screenshot
                </a>
              )}
            </div>
          </div>
          <div className={styles.elementGrid}>
            {page.elements.map(element => (
              <article className="panel" key={element.id}>
                {element.hasCrop && (
                  <img
                    className={styles.crop}
                    src={`${apiBase}/scans/elements/${element.id}/crop`}
                    alt={`Detected ${element.classification} element`}
                  />
                )}
                <div className={styles.elementTitle}>
                  <h4>{bestLabel(element)}</h4>
                  <span className="status-badge status-badge-info">{element.classification}</span>
                </div>
                <p className={styles.meta}>
                  {element.tagName}
                  {element.inputType ? ` · ${element.inputType}` : ''} · {element.isEnabled ? 'Enabled' : 'Disabled'}
                </p>
                {element.isPotentiallyDestructive && (
                  <div className="alert-warning">Potentially destructive control — detected only, never activated.</div>
                )}
                {element.selectors.find(x => x.isPreferred) && (
                  <p className={styles.selector}>
                    <strong>Preferred:</strong> {element.selectors.find(x => x.isPreferred)?.type} ·{' '}
                    <code>{element.selectors.find(x => x.isPreferred)?.value}</code>
                  </p>
                )}
                <details>
                  <summary>Selector candidates ({element.selectors.length})</summary>
                  <ul>
                    {element.selectors.map(s => (
                      <li key={s.id}>
                        <code>
                          {s.type}: {s.value}
                        </code>{' '}
                        — {s.wasUnique ? 'unique' : 'not unique'}
                      </li>
                    ))}
                  </ul>
                </details>
                {element.screenshotError && <p className="alert-warning">{element.screenshotError}</p>}
              </article>
            ))}
          </div>
        </article>
      ))}
      <details className={styles.diagnostics}>
        <summary>Diagnostics ({details.diagnostics.length})</summary>
        {details.diagnostics.length === 0 ? (
          <p>No diagnostics were recorded.</p>
        ) : (
          <ul>
            {details.diagnostics.map(d => (
              <li key={d.id}>
                <strong>
                  {d.severity} · {d.category}
                </strong>
                <br />
                {d.message}
                {d.url && <small>{d.url}</small>}
              </li>
            ))}
          </ul>
        )}
      </details>
    </section>
  )
}

function statusClass(status: string) {
  return status === 'Completed'
    ? 'status-badge-success'
    : status === 'Failed'
      ? 'status-badge-error'
      : status === 'Cancelled'
        ? 'status-badge-warning'
        : 'status-badge-info'
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function duration(scan: ScanSummary) {
  if (!scan.startedAtUtc) return '—'
  const end = scan.completedAtUtc ? new Date(scan.completedAtUtc).getTime() : Date.now()
  return `${Math.max(0, Math.round((end - new Date(scan.startedAtUtc).getTime()) / 1000))}s`
}

function bestLabel(element: {
  accessibleName: string | null
  associatedLabel: string | null
  visibleText: string | null
  placeholder: string | null
  tagName: string
}) {
  return element.accessibleName || element.associatedLabel || element.visibleText || element.placeholder || element.tagName
}
