import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { api, ApiError } from '../api'
import type { DetectedElement, Diagnostic, ElementClassification, PagedResponse, ScannedPage, ScanDetails, ScannerSettingsMetadata, ScanSettings, ScanSummary, Target } from '../types'
import MovingLoaderOverlay from '../components/MovingLoaderOverlay'
import styles from './ScansPage.module.css'

const active = (scan: ScanSummary) => scan.status === 'Queued' || scan.status === 'Running'
const apiBase = import.meta.env.VITE_API_BASE_URL ?? '/api'
const cancelErrorMessage = 'The scan could not be cancelled.'
const classifications: ElementClassification[] = ['Informational','Navigational','Input','Action','Submission','Upload','DateOrTime','PotentiallyDestructive','UnknownCustomControl']
const tabs = ['overview','pages','elements','diagnostics','settings'] as const
type Tab = typeof tabs[number]

function scanIsPersistedAsCancelled(history: ScanSummary[], id: string | null) {
  return !!id && history.some(scan => scan.id === id && scan.status === 'Cancelled')
}

const emptyPaged = <T,>(): PagedResponse<T> => ({ items: [], pageNumber: 1, pageSize: 25, totalCount: 0 })

export default function ScansPage() {
  const [targets, setTargets] = useState<Target[]>([])
  const [scanPage, setScanPage] = useState<PagedResponse<ScanSummary>>(emptyPaged)
  const scans = scanPage.items
  const [selectedTarget, setSelectedTarget] = useState('')
  const [selectedScan, setSelectedScan] = useState<string | null>(() => new URLSearchParams(location.search).get('scan'))
  const [details, setDetails] = useState<ScanDetails | null>(null)
  const [activeId, setActiveId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState('')
  const [loading, setLoading] = useState(true)
  const [historyFilters, setHistoryFilters] = useState({ search:'', status:'', targetId:'', pageNumber:1, pageSize:25 })
  const [settingsMeta, setSettingsMeta] = useState<ScannerSettingsMetadata | null>(null)
  const [settings, setSettings] = useState<ScanSettings | null>(null)
  const [activeTab, setActiveTab] = useState<Tab>('overview')
  const [elementFilters, setElementFilters] = useState({ search:'', classification:'', isPotentiallyDestructive:'', hasManualReview:'', hasManualSelector:'', pageNumber:1, pageSize:25 })
  const [diagnosticFilters, setDiagnosticFilters] = useState({ search:'', category:'', severity:'', statusCode:'', pageNumber:1, pageSize:25 })
  const [elements, setElements] = useState<PagedResponse<DetectedElement>>(emptyPaged)
  const [diagnostics, setDiagnostics] = useState<PagedResponse<Diagnostic>>(emptyPaged)
  const [viewer, setViewer] = useState<{src:string; title:string; meta:string} | null>(null)
  const selected = targets.find(x => x.id === selectedTarget)
  const running = useMemo(() => scans.find(x => x.id === activeId), [scans, activeId])

  const refresh = useCallback(async () => {
    const page = await api.scans(historyFilters)
    setScanPage(page)
    const current = page.items.find(active)
    setActiveId(current?.id ?? null)
    setError(previous => scanIsPersistedAsCancelled(page.items, activeId) && previous === cancelErrorMessage ? '' : previous)
    return page.items
  }, [historyFilters, activeId])

  const loadDetails = useCallback(async (id: string) => {
    const value = await api.scan(id)
    setDetails(value)
    setSelectedScan(id)
    const url = new URL(location.href)
    url.searchParams.set('scan', id)
    history.replaceState(null, '', url)
  }, [])

  useEffect(() => {
    let live = true
    Promise.all([api.targets(), api.scannerSettings(), api.scans(historyFilters)])
      .then(([all, meta, history]) => {
        if (!live) return
        const enabled = all.filter(x => x.isEnabled)
        setTargets(enabled)
        setSelectedTarget(enabled[0]?.id ?? '')
        setSettingsMeta(meta)
        setSettings(defaultSettings(meta))
        setScanPage(history)
        setActiveId(history.items.find(active)?.id ?? null)
        if (selectedScan) void loadDetails(selectedScan).catch(() => setError('The selected scan could not be loaded.'))
      })
      .catch(() => { if (live) setError('Scanner information could not be loaded. Confirm that the API and database are running.') })
      .finally(() => { if (live) setLoading(false) })
    return () => { live = false }
    // loadDetails intentionally stable; initial load should run once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void refresh().catch(() => setError('Scan history could not be refreshed.'))
    }, 0)
    return () => window.clearTimeout(timer)
  }, [refresh])

  useEffect(() => {
    if (!activeId) return
    const timer = window.setInterval(() => {
      void refresh().then(() => selectedScan ? loadDetails(selectedScan) : undefined).catch(() => setError('Scan status could not be refreshed.'))
    }, 1000)
    return () => window.clearInterval(timer)
  }, [activeId, refresh, selectedScan, loadDetails])

  useEffect(() => {
    if (!selectedScan || activeTab !== 'elements') return
    void api.scanElements(selectedScan, elementFilters).then(setElements).catch(() => setError('Elements could not be loaded.'))
  }, [selectedScan, activeTab, elementFilters])

  useEffect(() => {
    if (!selectedScan || activeTab !== 'diagnostics') return
    void api.scanDiagnostics(selectedScan, diagnosticFilters).then(setDiagnostics).catch(() => setError('Diagnostics could not be loaded.'))
  }, [selectedScan, activeTab, diagnosticFilters])

  async function start() {
    if (!selectedTarget || !settings) return
    setError(''); setSaved('')
    const validation = validateSettings(settings, settingsMeta)
    if (validation) { setError(validation); return }
    try {
      const result = await api.startScan(selectedTarget, settings)
      setActiveId(result.id)
      setSelectedScan(result.id)
      await refresh()
      await loadDetails(result.id)
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
      setScanPage(previous => ({ ...previous, items: previous.items.map(scan => scan.id === result.id ? result : scan) }))
      if (result.status === 'Cancelled') setActiveId(null)
      const history = await refresh()
      if (scanIsPersistedAsCancelled(history, cancellingId)) setError('')
    } catch (reason) {
      try {
        const history = await refresh()
        if (scanIsPersistedAsCancelled(history, cancellingId)) { setError(''); return }
      } catch { /* keep original cancellation error */ }
      setError(reason instanceof ApiError ? reason.message : cancelErrorMessage)
    }
  }

  async function openScan(id: string) {
    setError(''); setSaved(''); setDetails(null)
    try { await loadDetails(id); setActiveTab('overview') } catch { setError('Scan details could not be loaded.') }
  }

  function updateHistory(name: string, value: string) { setHistoryFilters(x => ({ ...x, [name]: value, pageNumber: 1 })) }
  function updateElementFilter(name: string, value: string) { setElementFilters(x => ({ ...x, [name]: value, pageNumber: 1 })) }
  function updateDiagnosticFilter(name: string, value: string) { setDiagnosticFilters(x => ({ ...x, [name]: value, pageNumber: 1 })) }

  return (
    <div className="stack">
      <div><h1 className="page-title">Safe scanner</h1><p className="page-description">Inspect one authorized starting page, then review and correct saved scan evidence without changing the original scanner-generated data.</p></div>
      {error&&<div className="alert-error" role="alert">{error}</div>}
      {saved&&<div className="alert-success" role="status">{saved}</div>}

      <section className={`card ${styles.launch}`} aria-labelledby="start-scan">
        <div><h2 id="start-scan">Start a one-page scan</h2><p>Only enabled targets are available. The fixed safety rules below are not editable.</p></div>
        {loading?<p role="status">Loading targets…</p>:targets.length===0?<div className="alert-info">There are no enabled targets. Enable a target on the Targets page first.</div>:<div className={styles.controls}><label className="field"><span className="field-label">Authorized target</span><select className="select" value={selectedTarget} onChange={e=>setSelectedTarget(e.target.value)}>{targets.map(t=><option key={t.id} value={t.id}>{t.name} · {t.environment}</option>)}</select></label><button className="button-primary" onClick={()=>void start()} disabled={!!activeId}>Start safe scan</button></div>}
        {selected&&<dl className={styles.targetInfo}><div><dt>Starting URL</dt><dd>{selected.startingUrl}</dd></div><div><dt>Allowed host</dt><dd><code>{selected.allowedHost}</code></dd></div><div><dt>Environment</dt><dd>{selected.environment}</dd></div></dl>}
        {settingsMeta&&settings&&<ScanLimitEditor meta={settingsMeta} settings={settings} onChange={setSettings} onReset={()=>setSettings(defaultSettings(settingsMeta))}/>}
      </section>

      <section className="card">
        <div className={styles.sectionHeader}><h2 className="section-heading">Recent scans</h2><span className="status-badge">{scanPage.totalCount} matching</span></div>
        <div className={`${styles.filterGrid} ${styles.spacedControlGrid}`}>
          <label className="field"><span className="field-label">Search</span><input className="input" value={historyFilters.search} onChange={e=>updateHistory('search',e.target.value)} placeholder="Target, URL, or title"/></label>
          <label className="field"><span className="field-label">Status</span><select className="select" value={historyFilters.status} onChange={e=>updateHistory('status',e.target.value)}><option value="">Any</option>{['Queued','Running','Completed','Failed','Cancelled'].map(x=><option key={x}>{x}</option>)}</select></label>
          <label className="field"><span className="field-label">Target</span><select className="select" value={historyFilters.targetId} onChange={e=>updateHistory('targetId',e.target.value)}><option value="">Any</option>{targets.map(t=><option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
          <button className="button-secondary" type="button" onClick={()=>setHistoryFilters({ search:'', status:'', targetId:'', pageNumber:1, pageSize:25 })}>Reset filters</button>
        </div>
        {activeFilterText(historyFilters)&&<p className={styles.activeFilters}>Active filters: {activeFilterText(historyFilters)}</p>}
        {scans.length===0?<div className="empty-state"><p>No scans match the current filters.</p></div>:<div className={`table-container ${styles.recentTableWrap}`}><table className={`data-table ${styles.recentTable}`}><thead><tr><th>Target</th><th>Status</th><th>Requested</th><th>Duration</th><th>Final URL</th><th className={styles.metricHeading}>Pages</th><th className={styles.metricHeading}>Elements</th><th className={styles.metricHeading}>Warnings</th><th className={styles.metricHeading}>Errors</th></tr></thead><tbody>{scans.map(scan=><tr key={scan.id}><td><button className={styles.linkButton} onClick={()=>void openScan(scan.id)}>{scan.targetName}</button></td><td><span className={`status-badge ${statusClass(scan.status)}`}>{scan.status}</span><small className={styles.stageSmall}>{scan.stage}</small></td><td>{formatDate(scan.requestedAtUtc)}</td><td>{duration(scan)}</td><td className={styles.url}>{scan.finalUrl??'—'}</td><td className={styles.metricCell}>{scan.detectedPageCount}</td><td className={styles.metricCell}>{scan.detectedElementCount}</td><td className={styles.metricCell}>{scan.warningCount}</td><td className={styles.metricCell}>{scan.errorCount}</td></tr>)}</tbody></table></div>}
        <Pager page={scanPage} onPage={pageNumber=>setHistoryFilters(x=>({...x,pageNumber}))}/>
      </section>

      {details&&<ScanDetail details={details} activeTab={activeTab} setActiveTab={setActiveTab} elements={elements} diagnostics={diagnostics} elementFilters={elementFilters} diagnosticFilters={diagnosticFilters} updateElementFilter={updateElementFilter} updateDiagnosticFilter={updateDiagnosticFilter} onElementPage={pageNumber=>setElementFilters(x=>({...x,pageNumber}))} onDiagnosticPage={pageNumber=>setDiagnosticFilters(x=>({...x,pageNumber}))} onViewer={setViewer} onRefresh={async()=>{await loadDetails(details.summary.id); if(activeTab==='elements')setElements(await api.scanElements(details.summary.id, elementFilters)); if(activeTab==='diagnostics')setDiagnostics(await api.scanDiagnostics(details.summary.id, diagnosticFilters));}} onSaved={setSaved} onError={setError}/>}
      {viewer&&<ImageViewer image={viewer} onClose={()=>setViewer(null)}/>}
      <MovingLoaderOverlay open={!!running} stage={running?.stage??'Preparing scan'} onCancel={()=>void cancel()}/>
    </div>
  )
}

function ScanLimitEditor({meta,settings,onChange,onReset}:{meta:ScannerSettingsMetadata;settings:ScanSettings;onChange:(s:ScanSettings)=>void;onReset:()=>void}) {
  const row = (key:keyof ScanSettings,label:string,limit:{min:number;max:number}) => <label className="field"><span className="field-label">{label}</span><input className="input" type="number" min={limit.min} max={limit.max} value={settings[key]} onChange={e=>onChange({...settings,[key]:Number(e.target.value)})}/><span className="field-hint">{limit.min}–{limit.max}</span></label>
  return <div className={styles.settingsBox}><div className={styles.sectionHeader}><h3>Scan limits</h3><button className="button-secondary" type="button" onClick={onReset}>Reset to defaults</button></div><div className={`${styles.filterGrid} ${styles.spacedControlGrid}`}>{row('overallTimeoutSeconds','Overall timeout (sec)',meta.overallTimeoutSeconds)}{row('navigationTimeoutMilliseconds','Navigation timeout (ms)',meta.navigationTimeoutMilliseconds)}{row('actionTimeoutMilliseconds','Action timeout (ms)',meta.actionTimeoutMilliseconds)}{row('maximumDetectedElements','Max elements',meta.maximumDetectedElements)}{row('maximumDiagnosticRecords','Max diagnostics',meta.maximumDiagnosticRecords)}{row('elementScreenshotPadding','Crop padding',meta.elementScreenshotPadding)}{row('viewportWidth','Viewport width',meta.viewportWidth)}{row('viewportHeight','Viewport height',meta.viewportHeight)}</div><details className={styles.safety}><summary>Advanced safety behavior</summary><ul>{meta.fixedSafetyRules.map(rule=><li key={rule}>{rule}</li>)}</ul></details></div>
}

function ScanDetail(props:{details:ScanDetails;activeTab:Tab;setActiveTab:(t:Tab)=>void;elements:PagedResponse<DetectedElement>;diagnostics:PagedResponse<Diagnostic>;elementFilters:Record<string,string|number>;diagnosticFilters:Record<string,string|number>;updateElementFilter:(n:string,v:string)=>void;updateDiagnosticFilter:(n:string,v:string)=>void;onElementPage:(n:number)=>void;onDiagnosticPage:(n:number)=>void;onViewer:(v:{src:string;title:string;meta:string})=>void;onRefresh:()=>Promise<void>;onSaved:(m:string)=>void;onError:(m:string)=>void}) {
  const {details,activeTab,setActiveTab}=props
  const tabRefs = useRef<Array<HTMLButtonElement|null>>([])
  function keyTabs(event:React.KeyboardEvent<HTMLDivElement>){const current=tabs.indexOf(activeTab);if(event.key==='ArrowRight'||event.key==='ArrowLeft'){event.preventDefault();const next=event.key==='ArrowRight'?(current+1)%tabs.length:(current+tabs.length-1)%tabs.length;setActiveTab(tabs[next]);tabRefs.current[next]?.focus()}}
  return <section className={`card ${styles.details}`}><div className={styles.detailHeading}><div><span className={`status-badge ${statusClass(details.summary.status)}`}>{details.summary.status}</span><h2>{details.summary.pageTitle||details.summary.targetName}</h2><p>{details.summary.finalUrl||details.summary.startingUrl}</p></div></div>{details.summary.failureSummary&&<div className="alert-error">{details.summary.failureSummary}</div>}<div className={`tab-list ${styles.detailTabs}`} role="tablist" aria-label="Scan detail sections" onKeyDown={keyTabs}>{tabs.map((tab,index)=><button key={tab} ref={el=>{tabRefs.current[index]=el}} className="tab-button" role="tab" aria-selected={activeTab===tab} onClick={()=>setActiveTab(tab)}>{tab[0].toUpperCase()+tab.slice(1)}</button>)}</div><div className={styles.tabPanel}>{activeTab==='overview'&&<Overview details={details}/>} {activeTab==='pages'&&<PagesTab {...props}/>} {activeTab==='elements'&&<ElementsTab {...props}/>} {activeTab==='diagnostics'&&<DiagnosticsTab {...props}/>} {activeTab==='settings'&&<SettingsTab details={details}/>}</div></section>
}

function Overview({details}:{details:ScanDetails}){return <div className="responsive-grid"><div className="panel"><h3>Summary</h3><p>{details.pages.length} page saved · {details.summary.detectedElementCount} elements · {details.summary.warningCount} warnings · {details.summary.errorCount} errors</p></div><div className="panel"><h3>Effective settings</h3><p>{details.settings.viewportWidth} × {details.settings.viewportHeight}, {details.settings.maximumDetectedElements} max elements, {details.settings.overallTimeoutSeconds}s timeout</p></div><div className="panel"><h3>Review model</h3><p>Manual names, classification overrides, and selector choices are stored separately from scanner-generated values and can be cleared.</p></div></div>}

function PagesTab({details,onViewer,onRefresh,onSaved,onError}:{details:ScanDetails;onViewer:(v:{src:string;title:string;meta:string})=>void;onRefresh:()=>Promise<void>;onSaved:(m:string)=>void;onError:(m:string)=>void}){return <div className="stack">{details.pages.map(page=><PageReview key={page.id} scanId={details.summary.id} page={page} onViewer={onViewer} onRefresh={onRefresh} onSaved={onSaved} onError={onError}/>)}</div>}

function PageReview({scanId,page,onViewer,onRefresh,onSaved,onError}:{scanId:string;page:ScannedPage;onViewer:(v:{src:string;title:string;meta:string})=>void;onRefresh:()=>Promise<void>;onSaved:(m:string)=>void;onError:(m:string)=>void}){const [name,setName]=useState(page.userDisplayName??'');const [saving,setSaving]=useState(false);async function save(value:string|null){setSaving(true);try{await api.updatePageReview(scanId,page.id,value);await onRefresh();onSaved(value?'Page name saved.':'Manual page name cleared.')}catch(reason){onError(reason instanceof ApiError?reason.message:'Page review could not be saved.')}finally{setSaving(false)}}return <article className={styles.page}><div className={styles.pageOverview}>{page.hasThumbnail&&<button className={`${styles.imageButton} ${styles.pagePreviewButton}`} onClick={()=>onViewer({src:`${apiBase}/scans/pages/${page.id}/screenshot`,title:page.displayName,meta:`PNG · ${page.screenshotWidth??'?'} × ${page.screenshotHeight??'?'}`})}><span className={styles.pagePreviewScroll}><img src={`${apiBase}/scans/pages/${page.id}/screenshot`} alt={`Preview of ${page.displayName}`}/></span></button>}<div><h3>{page.displayName}</h3><p>Generated: {page.generatedDisplayName}{page.userDisplayName&&' · manual override active'}</p><p>{page.finalUrl}</p><div className={styles.reviewRow}><input className="input" value={name} onChange={e=>setName(e.target.value)} aria-label={`Manual name for ${page.generatedDisplayName}`} placeholder="Manual page name"/><div className={styles.reviewActions}><button className="button-primary" disabled={saving} onClick={()=>void save(name)}>Save</button><button className="button-secondary" disabled={saving} onClick={()=>{setName('');void save(null)}}>Clear</button></div></div></div></div></article>}

function ElementsTab({details,elements,elementFilters,updateElementFilter,onElementPage,onViewer,onRefresh,onSaved,onError}:Parameters<typeof ScanDetail>[0]){return <div className="stack"><div className={styles.filterGrid}><label className="field"><span className="field-label">Search elements</span><input className="input" value={String(elementFilters.search)} onChange={e=>updateElementFilter('search',e.target.value)} /></label><label className="field"><span className="field-label">Classification</span><select className="select" value={String(elementFilters.classification)} onChange={e=>updateElementFilter('classification',e.target.value)}><option value="">Any</option>{classifications.map(x=><option key={x}>{x}</option>)}</select></label><label className="field"><span className="field-label">Destructive</span><select className="select" value={String(elementFilters.isPotentiallyDestructive)} onChange={e=>updateElementFilter('isPotentiallyDestructive',e.target.value)}><option value="">Any</option><option value="true">Potentially destructive</option><option value="false">Not destructive</option></select></label><label className="field"><span className="field-label">Reviewed</span><select className="select" value={String(elementFilters.hasManualReview)} onChange={e=>updateElementFilter('hasManualReview',e.target.value)}><option value="">Any</option><option value="true">Manual review</option><option value="false">No manual review</option></select></label><button className="button-secondary" onClick={()=>{updateElementFilter('search','');updateElementFilter('classification','');updateElementFilter('isPotentiallyDestructive','');updateElementFilter('hasManualReview','');updateElementFilter('hasManualSelector','')}}>Reset filters</button></div><p className={styles.activeFilters}>{elements.totalCount} matching elements</p>{elements.items.length===0?<div className="empty-state"><p>No elements match the current filters.</p></div>:<div className={styles.elementGrid}>{elements.items.map(element=><ElementReview key={element.id} scanId={details.summary.id} element={element} onViewer={onViewer} onRefresh={onRefresh} onSaved={onSaved} onError={onError}/>)}</div>}<Pager page={elements} onPage={onElementPage}/></div>}

function ElementReview({scanId,element,onViewer,onRefresh,onSaved,onError}:{scanId:string;element:DetectedElement;onViewer:(v:{src:string;title:string;meta:string})=>void;onRefresh:()=>Promise<void>;onSaved:(m:string)=>void;onError:(m:string)=>void}) {
  const [name,setName]=useState(element.userDisplayName??'')
  const [classification,setClassification]=useState<ElementClassification|''>(element.classificationOverride??'')
  const [savedElement,setSavedElement]=useState<DetectedElement | null>(null)
  const [saving,setSaving]=useState(false)
  const currentElement = savedElement?.id === element.id ? savedElement : element

  function applyElementReviewUpdate(updated: DetectedElement) {
    setSavedElement(updated)
    setName(updated.userDisplayName??'')
    setClassification(updated.classificationOverride??'')
  }

  async function saveReview(clear=false) {
    setSaving(true)
    try {
      const updated = await api.updateElementReview(scanId,currentElement.id,clear?null:name,clear?null:(classification||null))
      applyElementReviewUpdate(updated)
      await onRefresh()
      onSaved(clear?'Element review cleared.':'Element review saved.')
    } catch(reason) {
      onError(reason instanceof ApiError?reason.message:'Element review could not be saved.')
    } finally {
      setSaving(false)
    }
  }

  async function selector(id:string|null) {
    setSaving(true)
    try {
      const updated = await api.selectManualSelector(scanId,currentElement.id,id)
      applyElementReviewUpdate(updated)
      await onRefresh()
      onSaved(id?'Manual selector saved.':'Manual selector cleared.')
    } catch(reason) {
      onError(reason instanceof ApiError?reason.message:'Selector review could not be saved.')
    } finally {
      setSaving(false)
    }
  }

  return <article className="panel">{currentElement.hasCrop&&<button className={`${styles.imageButton} ${styles.elementImageButton}`} onClick={()=>onViewer({src:`${apiBase}/scans/elements/${currentElement.id}/crop`,title:currentElement.displayName,meta:'PNG element crop'})}><img className={styles.crop} src={`${apiBase}/scans/elements/${currentElement.id}/crop`} alt={`Detected ${currentElement.effectiveClassification} element`}/></button>}<div className={styles.elementTitle}><h4>{currentElement.displayName}</h4><span className="status-badge status-badge-info">{currentElement.effectiveClassification}</span></div><p className={styles.meta}>{currentElement.pageDisplayName} · {currentElement.tagName}{currentElement.inputType?` · ${currentElement.inputType}`:''} · {currentElement.isEnabled?'Enabled':'Disabled'}</p>{currentElement.isPotentiallyDestructive&&<div className="alert-warning">Potentially destructive control — detected only, never activated.</div>}<div className={styles.elementReviewGrid}><label className="field"><span className="field-label">Manual element name</span><input className="input" value={name} onChange={e=>setName(e.target.value)} placeholder="Manual element name (optional)"/></label><label className="field"><span className="field-label">Classification override</span><select className="select" value={classification} onChange={e=>setClassification(e.target.value as ElementClassification|'' )}><option value="">Use scanner classification ({currentElement.classification})</option>{classifications.map(x=><option key={x}>{x}</option>)}</select></label></div><div className={styles.reviewActions}><button className="button-primary" disabled={saving} onClick={()=>void saveReview()}>Save review</button><button className="button-secondary" disabled={saving} onClick={()=>void saveReview(true)}>Clear overrides</button></div><details className={styles.selectorDetails}><summary>Selector candidates ({currentElement.selectors.length})</summary><div className={styles.selectorList}>{currentElement.selectors.map(s=><label key={s.id} className={styles.selectorOption}><input type="radio" name={`selector-${currentElement.id}`} checked={s.isManualPreferred} onChange={()=>void selector(s.id)}/><span><code>{s.type}: {s.value}</code><small>Priority {s.priority} · {s.wasUnique?'unique':'not unique'} · confidence {Math.round(s.confidence*100)}% {s.isScannerPreferred?'· scanner-preferred':''} {s.isEffectivePreferred?'· effective':''}</small></span></label>)}</div><button className="button-secondary" disabled={saving||!currentElement.manualPreferredSelectorCandidateId} onClick={()=>void selector(null)}>Clear manual selector</button></details>{currentElement.screenshotError&&<p className="alert-warning">{currentElement.screenshotError}</p>}</article>
}
function DiagnosticsTab({diagnostics,diagnosticFilters,updateDiagnosticFilter,onDiagnosticPage}:Parameters<typeof ScanDetail>[0]){return <div className="stack"><div className={styles.filterGrid}><label className="field"><span className="field-label">Search diagnostics</span><input className="input" value={String(diagnosticFilters.search)} onChange={e=>updateDiagnosticFilter('search',e.target.value)} /></label><label className="field"><span className="field-label">Category</span><select className="select" value={String(diagnosticFilters.category)} onChange={e=>updateDiagnosticFilter('category',e.target.value)}><option value="">Any</option>{['BrowserConsoleError','BrowserConsoleWarning','PageError','FailedNetworkRequest','HttpResponseError','NavigationError','ScreenshotError','ScannerWarning'].map(x=><option key={x}>{x}</option>)}</select></label><label className="field"><span className="field-label">Severity</span><select className="select" value={String(diagnosticFilters.severity)} onChange={e=>updateDiagnosticFilter('severity',e.target.value)}><option value="">Any</option>{['Information','Warning','Error'].map(x=><option key={x}>{x}</option>)}</select></label><label className="field"><span className="field-label">HTTP status</span><input className="input" value={String(diagnosticFilters.statusCode)} onChange={e=>updateDiagnosticFilter('statusCode',e.target.value)} /></label><button className="button-secondary" onClick={()=>{updateDiagnosticFilter('search','');updateDiagnosticFilter('category','');updateDiagnosticFilter('severity','');updateDiagnosticFilter('statusCode','')}}>Reset filters</button></div>{diagnostics.items.length===0?<div className="empty-state"><p>No diagnostics match the current filters.</p></div>:<div className="stack">{diagnostics.items.map(d=><details className="expandable-row" key={d.id}><summary className="expandable-row-summary"><span className={`status-badge ${d.severity==='Error'?'status-badge-error':d.severity==='Warning'?'status-badge-warning':'status-badge-info'}`}>{d.severity}</span> {d.category} · {formatDate(d.createdAtUtc)}</summary><div className="expandable-row-details"><p>{d.message}</p>{d.url&&<p className={styles.url}>{d.url}</p>}<p>{d.method??'No method'} {d.statusCode??''}</p></div></details>)}</div>}<Pager page={diagnostics} onPage={onDiagnosticPage}/></div>}

function SettingsTab({details}:{details:ScanDetails}){return <dl className={styles.settingsList}>{Object.entries(details.settings).map(([key,value])=><div key={key}><dt>{key}</dt><dd>{value}</dd></div>)}</dl>}

function ImageViewer({image,onClose}:{image:{src:string;title:string;meta:string};onClose:()=>void}){const [zoom,setZoom]=useState(1);const close=useRef<HTMLButtonElement|null>(null);useEffect(()=>{const previous=document.activeElement as HTMLElement|null;close.current?.focus();const onKey=(e:KeyboardEvent)=>{if(e.key==='Escape')onClose()};document.addEventListener('keydown',onKey);return()=>{document.removeEventListener('keydown',onKey);previous?.focus()}},[onClose]);return <div className={styles.viewerBackdrop} role="dialog" aria-modal="true" aria-labelledby="image-viewer-title"><div className={styles.viewerCard}><div className={styles.sectionHeader}><div><h2 id="image-viewer-title">{image.title}</h2><p>{image.meta}</p></div><button ref={close} className="button-secondary" onClick={onClose}>Close</button></div><div className={styles.viewerTools}><button className="button-secondary" onClick={()=>setZoom(z=>Math.max(.25,z-.25))}>Zoom out</button><button className="button-secondary" onClick={()=>setZoom(1)}>Reset zoom</button><button className="button-secondary" onClick={()=>setZoom(z=>Math.min(3,z+.25))}>Zoom in</button></div><div className={styles.viewerImageWrap}><img src={image.src} alt={image.title} style={{transform:`scale(${zoom})`}}/></div></div></div>}

function Pager<T>({page,onPage}:{page:PagedResponse<T>;onPage:(n:number)=>void}){const max=Math.max(1,Math.ceil(page.totalCount/page.pageSize));return <div className={styles.pager}><button className="button-secondary" disabled={page.pageNumber<=1} onClick={()=>onPage(page.pageNumber-1)}>Previous</button><span>Page {page.pageNumber} of {max}</span><button className="button-secondary" disabled={page.pageNumber>=max} onClick={()=>onPage(page.pageNumber+1)}>Next</button></div>}
function defaultSettings(meta:ScannerSettingsMetadata):ScanSettings{return {overallTimeoutSeconds:meta.overallTimeoutSeconds.default,navigationTimeoutMilliseconds:meta.navigationTimeoutMilliseconds.default,actionTimeoutMilliseconds:meta.actionTimeoutMilliseconds.default,maximumDetectedElements:meta.maximumDetectedElements.default,maximumDiagnosticRecords:meta.maximumDiagnosticRecords.default,elementScreenshotPadding:meta.elementScreenshotPadding.default,viewportWidth:meta.viewportWidth.default,viewportHeight:meta.viewportHeight.default}}
function validateSettings(settings:ScanSettings,meta:ScannerSettingsMetadata|null){if(!meta)return null;for(const [key,value] of Object.entries(settings)){const limit=meta[key as keyof ScanSettings] as {min:number;max:number};if(value<limit.min||value>limit.max)return `${key} must be between ${limit.min} and ${limit.max}.`}return null}
function activeFilterText(filters:Record<string,string|number>){return Object.entries(filters).filter(([k,v])=>!['pageNumber','pageSize'].includes(k)&&v).map(([k,v])=>`${k}: ${v}`).join(', ')}
function statusClass(status:string){return status==='Completed'?'status-badge-success':status==='Failed'?'status-badge-error':status==='Cancelled'?'status-badge-warning':'status-badge-info'}
function formatDate(value:string){return new Intl.DateTimeFormat(undefined,{dateStyle:'medium',timeStyle:'short'}).format(new Date(value))}
function duration(scan:ScanSummary){if(!scan.startedAtUtc)return'—';const end=scan.completedAtUtc?new Date(scan.completedAtUtc).getTime():Date.now();return`${Math.max(0,Math.round((end-new Date(scan.startedAtUtc).getTime())/1000))}s`}
