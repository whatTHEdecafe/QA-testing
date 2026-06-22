import { useEffect, useState } from 'react'
import { api } from '../api'
import type { DashboardSummary } from '../types'
import styles from './DashboardPage.module.css'

export default function DashboardPage({onOpenTargets}:{onOpenTargets:()=>void}) {
  const [summary,setSummary]=useState<DashboardSummary|null>(null); const [error,setError]=useState('')
  useEffect(()=>{api.dashboard().then(setSummary).catch(()=>setError('Dashboard information is unavailable. Confirm that the API and database are running.'))},[])
  return <div className="stack"><div><h1 className="page-title">Dashboard</h1><p className="page-description">A clear starting point for authorized QA targets. Scanning and test execution are intentionally not part of this foundation phase.</p></div>
    {error&&<div className="alert-error" role="alert">{error}</div>}
    <section className="responsive-grid" aria-label="Target summary">
      <article className={`card ${styles.metric}`}><span>Saved targets</span><strong>{summary?.totalTargets??'—'}</strong><small>Authorized deployments configured</small></article>
      <article className={`card ${styles.metric}`}><span>Enabled targets</span><strong>{summary?.enabledTargets??'—'}</strong><small>Ready for future QA workflows</small></article>
      <article className={`card ${styles.next}`}><span className="status-badge status-badge-info">Phase 1</span><h2>Foundation active</h2><p>Manage safe target boundaries and confirm persistence before adding browser automation.</p><button className="button-primary" onClick={onOpenTargets}>Manage targets</button></article>
    </section>
  </div>
}
