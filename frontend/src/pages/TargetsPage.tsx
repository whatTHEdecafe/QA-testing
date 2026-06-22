import { useCallback, useEffect, useState } from 'react'
import { api, ApiError } from '../api'
import type { Target, TargetInput } from '../types'
import Modal from '../components/Modal'
import TargetForm from '../components/TargetForm'
import styles from './TargetsPage.module.css'

export default function TargetsPage() {
  const [targets,setTargets]=useState<Target[]>([]); const [editing,setEditing]=useState<Target|null|undefined>(undefined)
  const [deleting,setDeleting]=useState<Target|null>(null); const [error,setError]=useState(''); const [loading,setLoading]=useState(true)
  const load=useCallback(async()=>{setLoading(true);setError('');try{setTargets(await api.targets())}catch{setError('Targets could not be loaded. Confirm that the API and SQL Server are available.')}finally{setLoading(false)}},[])
  useEffect(()=>{
    let active=true
    api.targets().then(items=>{if(active)setTargets(items)}).catch(()=>{if(active)setError('Targets could not be loaded. Confirm that the API and SQL Server are available.')}).finally(()=>{if(active)setLoading(false)})
    return()=>{active=false}
  },[])
  async function save(input:TargetInput){if(editing)await api.updateTarget(editing.id,input);else await api.createTarget(input);setEditing(undefined);await load()}
  async function toggle(target:Target){setError('');try{await api.updateTarget(target.id,{name:target.name,startingUrl:target.startingUrl,allowedHost:target.allowedHost,environment:target.environment,description:target.description,isEnabled:!target.isEnabled});await load()}catch(reason){setError(reason instanceof ApiError?reason.message:'The target status could not be changed.')}}
  async function remove(){if(!deleting)return;setError('');try{await api.deleteTarget(deleting.id);setDeleting(null);await load()}catch(reason){setError(reason instanceof ApiError?reason.message:'The target could not be deleted.')}}
  return <div className="stack">
    <div className={styles.heading}><div><h1 className="page-title">Targets</h1><p className="page-description">Save the customer websites and deployments you are authorized to test. Allowed hosts establish a safety boundary for future automation.</p></div><button className="button-primary" onClick={()=>setEditing(null)}>Add target</button></div>
    {error&&<div className="alert-error" role="alert">{error}</div>}
    {loading?<div className="panel" role="status">Loading targets…</div>:targets.length===0?<section className="empty-state"><h2>No targets yet</h2><p>Add an authorized deployment to establish the foundation for future QA scans.</p><button className="button-primary" onClick={()=>setEditing(null)}>Add your first target</button></section>:
    <div className={styles.grid}>{targets.map(target=><article className={`card ${styles.target}`} key={target.id}>
      <div className={styles.targetHeading}><div><div className={styles.badges}><span className={`status-badge ${target.isEnabled?'status-badge-success':'status-badge-warning'}`}>{target.isEnabled?'Enabled':'Disabled'}</span><span className="status-badge status-badge-info">{target.environment}</span></div><h2>{target.name}</h2></div></div>
      <dl><div><dt>Starting URL</dt><dd><a href={target.startingUrl} target="_blank" rel="noreferrer">{target.startingUrl}</a></dd></div><div><dt>Allowed host</dt><dd><code>{target.allowedHost}</code></dd></div><div><dt>Updated</dt><dd>{new Intl.DateTimeFormat(undefined,{dateStyle:'medium',timeStyle:'short'}).format(new Date(target.updatedAtUtc))}</dd></div></dl>
      {target.description&&<p className={styles.description}>{target.description}</p>}
      <div className={styles.actions}><button className="button-secondary" onClick={()=>setEditing(target)}>Edit</button><button className="button-secondary" onClick={()=>void toggle(target)}>{target.isEnabled?'Disable':'Enable'}</button><button className="button-danger" onClick={()=>setDeleting(target)}>Delete</button></div>
    </article>)}</div>}
    {editing!==undefined&&<Modal title={editing?'Edit target':'Add target'} onClose={()=>setEditing(undefined)}><TargetForm target={editing??undefined} onSave={save} onCancel={()=>setEditing(undefined)}/></Modal>}
    {deleting&&<Modal title="Delete target?" onClose={()=>setDeleting(null)}><div className="stack"><div className="alert-warning">This permanently removes <strong>{deleting.name}</strong>. It will not affect the customer website.</div><p className={styles.confirm}>This action cannot be undone.</p><div className={styles.modalActions}><button className="button-secondary" onClick={()=>setDeleting(null)}>Keep target</button><button className="button-danger" onClick={()=>void remove()}>Delete target</button></div></div></Modal>}
  </div>
}
