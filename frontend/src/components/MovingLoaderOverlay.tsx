import { useEffect, useId, useRef } from 'react'
import styles from './MovingLoaderOverlay.module.css'

export default function MovingLoaderOverlay({open,stage,onCancel}:{open:boolean;stage:string;onCancel:()=>void}){
  const title=useId(), card=useRef<HTMLDivElement>(null)
  useEffect(()=>{if(!open)return;const previous=document.activeElement as HTMLElement|null;card.current?.focus();const key=(e:KeyboardEvent)=>{if(e.key==='Escape')onCancel()};document.addEventListener('keydown',key);return()=>{document.removeEventListener('keydown',key);previous?.focus()}},[open,onCancel])
  if(!open)return null
  return <div className={styles.backdrop}><div ref={card} className={styles.card} role="dialog" aria-modal="true" aria-labelledby={title} tabIndex={-1}><div className={styles.scene} aria-hidden="true"><div className={styles.radar}/><div className={styles.pulse}/></div><p id={title} className={styles.title}>SCANNING SAFELY</p><p className={styles.stage} aria-live="polite">{stage}</p><p className={styles.note}>Only the authorized starting page is being inspected. No controls are clicked.</p><button className="button-danger" onClick={onCancel}>Cancel scan</button></div></div>
}
