import { useEffect, useId, useRef, type ReactNode } from 'react'
import styles from './Modal.module.css'

interface Props { title: string; children: ReactNode; onClose: () => void }

export default function Modal({ title, children, onClose }: Props) {
  const titleId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null
    dialogRef.current?.focus()
    const keydown = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', keydown)
    return () => { document.removeEventListener('keydown', keydown); previous?.focus() }
  }, [onClose])
  return <div className={styles.backdrop} onMouseDown={event => { if (event.target === event.currentTarget) onClose() }}>
    <div ref={dialogRef} className={styles.card} role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1}>
      <div className={styles.header}><h2 id={titleId}>{title}</h2><button type="button" onClick={onClose} aria-label="Close dialog">×</button></div>
      {children}
    </div>
  </div>
}
