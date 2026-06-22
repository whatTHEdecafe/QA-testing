import { useState } from 'react'
import DashboardPage from './pages/DashboardPage'
import TargetsPage from './pages/TargetsPage'
import styles from './App.module.css'

type Page = 'dashboard' | 'targets'

export default function App() {
  const [page, setPage] = useState<Page>('dashboard')
  return <div className="app-shell">
    <header className="app-header">
      <div className={`app-header-inner ${styles.header}`}>
        <button className={styles.brand} onClick={() => setPage('dashboard')} aria-label="QA Automation dashboard">
          <span className={styles.mark} aria-hidden="true">QA</span>
          <span><strong>Automation</strong><small>Testing foundation</small></span>
        </button>
        <nav aria-label="Primary navigation" className={styles.nav}>
          {(['dashboard', 'targets'] as Page[]).map(item =>
            <button key={item} onClick={() => setPage(item)} aria-current={page === item ? 'page' : undefined}>
              {item[0].toUpperCase() + item.slice(1)}
            </button>)}
        </nav>
      </div>
    </header>
    <main className="page-container">{page === 'dashboard' ? <DashboardPage onOpenTargets={() => setPage('targets')} /> : <TargetsPage />}</main>
  </div>
}
