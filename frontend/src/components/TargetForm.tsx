import { useState, type FormEvent } from 'react'
import type { Target, TargetEnvironment, TargetInput } from '../types'
import { ApiError } from '../api'
import styles from './TargetForm.module.css'

const empty: TargetInput = { name:'', startingUrl:'', allowedHost:'', environment:'Staging', description:null, isEnabled:true }
interface Props { target?: Target; onSave: (input: TargetInput) => Promise<void>; onCancel: () => void }

export default function TargetForm({ target, onSave, onCancel }: Props) {
  const [value, setValue] = useState<TargetInput>(target ? { name:target.name, startingUrl:target.startingUrl, allowedHost:target.allowedHost, environment:target.environment, description:target.description, isEnabled:target.isEnabled } : empty)
  const [errors, setErrors] = useState<Record<string,string[]>>({})
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)
  const set = <K extends keyof TargetInput>(key: K, next: TargetInput[K]) => setValue(current => ({...current,[key]:next}))
  const error = (key:string) => errors[key]?.join(' ')
  async function submit(event: FormEvent) {
    event.preventDefault(); setSaving(true); setMessage(''); setErrors({})
    try { await onSave(value) } catch (reason) {
      const apiError = reason instanceof ApiError ? reason : new ApiError('The target could not be saved.')
      setMessage(apiError.message); setErrors(apiError.fieldErrors)
    } finally { setSaving(false) }
  }
  return <form onSubmit={submit} noValidate className={styles.form}>
    {message && <div className="alert-error" role="alert">{message}</div>}
    <div className="responsive-grid">
      <label className="field"><span className="field-label">Customer or deployment name</span><input autoFocus className="input" value={value.name} onChange={e=>set('name',e.target.value)} aria-invalid={!!error('name')} aria-describedby={error('name')?'name-error':undefined}/>{error('name')&&<span id="name-error" className={styles.error}>{error('name')}</span>}</label>
      <label className="field"><span className="field-label">Environment</span><select className="select" value={value.environment} onChange={e=>set('environment',e.target.value as TargetEnvironment)}>{['Development','Staging','Production'].map(x=><option key={x}>{x}</option>)}</select></label>
    </div>
    <label className="field"><span className="field-label">Starting URL</span><input className="input" type="url" placeholder="https://staging.example.com" value={value.startingUrl} onChange={e=>set('startingUrl',e.target.value)} aria-invalid={!!error('startingUrl')} aria-describedby={error('startingUrl')?'url-error':undefined}/>{error('startingUrl')&&<span id="url-error" className={styles.error}>{error('startingUrl')}</span>}<span className="field-hint">Use a complete HTTP or HTTPS URL. Credentials do not belong here.</span></label>
    <label className="field"><span className="field-label">Allowed host or domain</span><input className="input" placeholder="example.com" value={value.allowedHost} onChange={e=>set('allowedHost',e.target.value)} aria-invalid={!!error('allowedHost')} aria-describedby={error('allowedHost')?'host-error':undefined}/>{error('allowedHost')&&<span id="host-error" className={styles.error}>{error('allowedHost')}</span>}<span className="field-hint">Host only—no scheme, port, or path. Future automation will remain inside this boundary.</span></label>
    <label className="field"><span className="field-label">Description or notes</span><textarea className="textarea" value={value.description??''} onChange={e=>set('description',e.target.value||null)} maxLength={1000}/>{error('description')&&<span className={styles.error}>{error('description')}</span>}</label>
    <label className="check-field"><input type="checkbox" checked={value.isEnabled} onChange={e=>set('isEnabled',e.target.checked)}/><span>Target is enabled</span></label>
    <div className={styles.actions}><button type="button" className="button-secondary" onClick={onCancel} disabled={saving}>Cancel</button><button className="button-primary" disabled={saving}>{saving?'Saving…':target?'Save changes':'Add target'}</button></div>
  </form>
}
