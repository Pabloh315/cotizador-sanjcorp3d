import { FormEvent, useEffect, useState } from 'react'
import {
  AlertTriangle, BarChart3, Bell, Calculator, CircleHelp, ClipboardList, FileClock, Gauge, Layers3, LockKeyhole,
  LogOut, Menu, MessageCircle, Moon, PackagePlus, PackageSearch, Printer, Settings, ShieldCheck, ShoppingCart, Sun, Users, X,
} from 'lucide-react'
import { api, type Profile } from './api'
import type { InventoryAlert } from './types'
import { ConsumablesPage, MaterialsPage, PrintersPage } from './pages/CatalogPages'
import { DashboardPage } from './pages/DashboardPage'
import { HelpPage } from './pages/HelpPage'
import { HistoryPage } from './pages/HistoryPage'
import { QuotePage } from './pages/QuotePage'
import { StoreQuotePage } from './pages/StoreQuotePage'
import { StoreProductsPage } from './pages/StoreProductsPage'
import { OrdersPage } from './pages/OrdersPage'
import { ReportsPage } from './pages/ReportsPage'
import { SettingsPage } from './pages/SettingsPage'
import { UsersPage } from './pages/UsersPage'
import { SupremeAdminPage } from './pages/SupremeAdminPage'
import { ChatPage } from './pages/ChatPage'
import { PublicHome } from './pages/PublicHome'
import { hasAnyRole, roleLabel } from './ui'

type PageKey = 'dashboard' | 'quote' | 'storeQuote' | 'storeProducts' | 'orders' | 'printers' | 'consumables' | 'materials' | 'history' | 'reports' | 'users' | 'settings' | 'help' | 'chat' | 'supreme'
const brandName = 'Atlas Impresiones 3D'
const lightLogo = '/logo.png'
const darkLogo = '/logoblanco.png'
const themeKey = 'atlas.theme'
const routeKey = 'atlas.route'
type ThemeMode = 'light' | 'dark'

export function App() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)
  const [route, setRoute] = useState(() => window.location.hash.slice(1))
  const [theme, setTheme] = useState<ThemeMode>(() => (localStorage.getItem(themeKey) as ThemeMode | null) ?? 'light')
  const logo = theme === 'dark' ? darkLogo : lightLogo
  const toggleTheme = () => setTheme(current => {
    const next = current === 'dark' ? 'light' : 'dark'
    localStorage.setItem(themeKey, next)
    return next
  })
  useEffect(() => { document.body.classList.toggle('atlas-theme-light', theme === 'light'); document.body.classList.toggle('atlas-theme-dark', theme === 'dark') }, [theme])
  useEffect(() => { const listener = () => setRoute(window.location.hash.slice(1)); window.addEventListener('hashchange', listener); return () => window.removeEventListener('hashchange', listener) }, [])
  useEffect(() => { api.me().then(setProfile).catch(() => setProfile(null)).finally(() => setLoading(false)) }, [])
  if (loading) return <main className="center"><div className="spinner" aria-label="Cargando" /></main>
  if (!profile) return route === 'login' ? <Login onSuccess={setProfile} onBack={() => { window.location.hash = ''; setRoute('') }} theme={theme} logo={logo} onToggleTheme={toggleTheme} /> : <PublicHome onLoginClick={() => { window.location.hash = 'login'; setRoute('login') }} theme={theme} onToggleTheme={toggleTheme} />
  return <Application profile={profile} theme={theme} logo={logo} onToggleTheme={toggleTheme} onProfileChange={setProfile} onLogout={() => api.logout().finally(() => { localStorage.removeItem(routeKey); setProfile(null); window.location.hash = '' })} />
}

function Login({ onSuccess, onBack, theme, logo, onToggleTheme }: { onSuccess: (profile: Profile) => void; onBack: () => void; theme: ThemeMode; logo: string; onToggleTheme: () => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [workspace, setWorkspace] = useState<string | null>(null)
  const [requiresCode, setRequiresCode] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!workspace) return
    setBusy(true); setError('')
    try {
      sessionStorage.removeItem('sanjcorp.tenant')
      const next = await api.login(username, password, code || undefined, workspace)
      onSuccess(next)
      const target = next.isSuperAdmin ? 'supreme' : 'dashboard'
      localStorage.setItem(routeKey, target)
      window.location.hash = target
    } catch (reason) {
      const authError = reason as Error & { requiresTwoFactor?: boolean }
      if (authError.requiresTwoFactor) setRequiresCode(true)
      else setError(authError.message)
    } finally { setBusy(false) }
  }

  return <main className="login-shell">
    <section className="brand-panel"><button type="button" className="back-link public-back" onClick={onBack}>Volver a la página principal</button>
      <div className="brand-logo"><img src={logo} alt={brandName} /></div>
      <p className="eyebrow">ATLAS IMPRESIONES 3D</p>
      <h1>Control preciso para cada impresión.</h1>
      <p className="lead">Cotizaciones, inventario, pedidos y ventas en un espacio privado diseñado para tu equipo.</p>
      <div className="security-note"><ShieldCheck size={20} /><span>Acceso protegido, permisos por rol y datos organizados.</span></div>
    </section>
    <section className="login-panel"><button type="button" className="theme-toggle floating-theme-toggle" onClick={onToggleTheme}>{theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}{theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}</button><form className="login-card" onSubmit={submit}>
      <div className="lock"><LockKeyhole size={24} /></div>
      <p className="eyebrow">ACCESO AL SISTEMA</p>
      <h2>{workspace ? 'Iniciar sesión' : 'Elige tu espacio'}</h2>
      {!workspace ? <>
        <p className="muted">Selecciona donde quieres trabajar.</p>
        <div className="workspace-choice"><button type="button" onClick={() => setWorkspace('technology')}>{brandName}</button><button type="button" onClick={() => setWorkspace('makers')}>Makers</button><button type="button" className="supreme-login" onClick={() => setWorkspace('technology')}>Administrador supremo</button></div>
      </> : <>
        <button type="button" className="back-link" onClick={() => { setWorkspace(null); setError(''); setRequiresCode(false) }}>Volver</button>
        <label>Usuario<input type="text" autoComplete="username" value={username} onChange={event => setUsername(event.target.value)} required autoFocus /></label>
        <label>Contrasena<input type="password" autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} required /></label>
        {requiresCode && <label>Código 2FA o de recuperación<input inputMode="numeric" autoComplete="one-time-code" value={code} onChange={event => setCode(event.target.value)} required autoFocus /></label>}
        {error && <p className="error" role="alert">{error}</p>}
        <button disabled={busy}>{busy ? 'Verificando...' : 'Entrar de forma segura'}</button>
      </>}
    </form></section>
  </main>
}

function Application({ profile, theme, logo, onToggleTheme, onProfileChange, onLogout }: { profile: Profile; theme: ThemeMode; logo: string; onToggleTheme: () => void; onProfileChange: (profile: Profile) => void; onLogout: () => void }) {
  const [route, setRoute] = useState(() => window.location.hash.slice(1) || localStorage.getItem(routeKey) || 'dashboard')
  const [mobileOpen, setMobileOpen] = useState(false)
  useEffect(() => { const listener = () => { const next = window.location.hash.slice(1) || localStorage.getItem(routeKey) || 'dashboard'; setRoute(next); localStorage.setItem(routeKey, next) }; listener(); window.addEventListener('hashchange', listener); return () => window.removeEventListener('hashchange', listener) }, [])
  const page = route.split(':')[0] as PageKey
  const initialHistoryId = page === 'history' ? Number(route.split(':')[1] || 0) || undefined : undefined
  const isSuperAdmin = profile.isSuperAdmin === true || profile.roles.includes('SuperAdmin')
  const [supremeMode, setSupremeMode] = useState<'technology' | 'makers'>(() => (sessionStorage.getItem('sanjcorp.mode') as 'technology' | 'makers') || 'technology')
  const isAdmin = isSuperAdmin || profile.roles.includes('Administrator')
  const canCatalog = isSuperAdmin || profile.roles.includes('Administrator') || (profile.roles.includes('Maker') && profile.isMakerOwner === true)
  const canSale = isSuperAdmin || hasAnyRole(profile.roles, ['Administrator', 'Sales', 'Maker'])
  const canOrders = isSuperAdmin || hasAnyRole(profile.roles, ['Administrator', 'Sales', 'Production', 'Maker'])
  const goTo = (next: string) => { localStorage.setItem(routeKey, next); window.location.hash = next; setRoute(next); setMobileOpen(false) }
  type NavItem = { key: PageKey; label: string; icon: typeof Gauge; admin?: boolean }
  const navItem = (key: PageKey, label: string, icon: typeof Gauge, admin = false): NavItem => ({ key, label, icon, admin })
  const navigationGroups: Array<{ label: string; items: NavItem[] }> = [
    { label: 'Inicio', items: [navItem('dashboard', 'Resumen', Gauge)] },
    { label: 'Trabajo diario', items: [
      navItem('quote', 'Cotizador', Calculator),
      navItem('storeQuote', 'Cotizaciones tienda', ShoppingCart),
      ...(canOrders ? [navItem('orders', 'Pedidos', ClipboardList)] : []),
      navItem('chat', 'Chat', MessageCircle),
    ] },
    { label: 'Catalogos', items: [
      ...(canCatalog ? [navItem('storeProducts', 'Productos tienda', PackagePlus)] : []),
      navItem('printers', 'Impresoras', Printer),
      navItem('consumables', 'Filamentos y resinas', PackageSearch),
      navItem('materials', 'Materiales', Layers3),
    ] },
    { label: 'Ventas y sistema', items: [
      navItem('history', 'Historial', FileClock),
      navItem('reports', 'Reportes y ventas', BarChart3),
      ...(isAdmin ? [navItem('users', 'Usuarios', Users, true)] : []),
      navItem('settings', 'Configuracion', Settings),
      navItem('help', 'Ayuda', CircleHelp),
      ...(isSuperAdmin ? [navItem('supreme', 'Administrar espacios', ShieldCheck, true)] : []),
    ] },
  ].filter(group => group.items.length > 0)
  const visibleNavigationGroups: Array<{ label: string; items: NavItem[] }> = isSuperAdmin ? [{ label: 'Sistema', items: [navItem('supreme', 'Administracion', ShieldCheck)] }] : navigationGroups
  const activeWorkspace = profile.workspaces?.find(x => x.id === (profile.tenantId ?? sessionStorage.getItem('sanjcorp.tenant')))
  const receiptLogo = isSuperAdmin ? (profile.workspaces?.find(x => x.kind === 'technology')?.logoUrl ?? profile.logoUrl ?? logo) : (activeWorkspace?.logoUrl ?? profile.logoUrl ?? logo)
  let content
  switch (page) {
    case 'supreme': content = isSuperAdmin ? <SupremeAdminPage profile={profile} mode={supremeMode} /> : <DashboardPage goTo={goTo} />; break
    case 'chat': content = <ChatPage profile={profile} />; break
    case 'quote': content = <QuotePage canWrite={canSale} />; break
    case 'storeQuote': content = <StoreQuotePage canWrite={canSale} />; break
    case 'storeProducts': content = canCatalog ? <StoreProductsPage canManage={canCatalog} /> : <DashboardPage goTo={goTo} />; break
    case 'orders': content = canOrders ? <OrdersPage canManage={canOrders} /> : <DashboardPage goTo={goTo} />; break
    case 'printers': content = <PrintersPage canManage={isAdmin} canFavorite={!isSuperAdmin} />; break
    case 'consumables': content = <ConsumablesPage canEdit={canCatalog} />; break
    case 'materials': content = <MaterialsPage canEdit={canCatalog} />; break
    case 'history': content = <HistoryPage initialId={initialHistoryId} canSale={canSale} isAdmin={isAdmin} logoUrl={receiptLogo} businessName={brandName} />; break
    case 'reports': content = <ReportsPage />; break
    case 'users': content = isAdmin ? <UsersPage /> : <DashboardPage goTo={goTo} />; break
    case 'settings': content = <SettingsPage profile={profile} isAdmin={isAdmin} onProfileChange={onProfileChange} />; break
    case 'help': content = <HelpPage />; break
    default: content = <DashboardPage goTo={goTo} />
  }
  return <div className="app-shell">
    {mobileOpen && <button className="sidebar-scrim" aria-label="Cerrar menu" onClick={() => setMobileOpen(false)} />}
    <aside className={`sidebar ${mobileOpen ? 'open' : ''}`}><div className="sidebar-brand"><div className="brand-logo small-mark"><img src={logo} alt={brandName} /></div><div><strong>{isSuperAdmin ? 'ADMINISTRACION' : 'ATLAS'}</strong><span>{isSuperAdmin ? 'PANEL SUPREMO' : 'IMPRESIONES 3D'}</span></div><button className="icon mobile-close" onClick={() => setMobileOpen(false)}><X size={20} /></button></div><nav>{visibleNavigationGroups.map(group => <section className="nav-section" key={group.label}><p>{group.label}</p>{group.items.map(({ key, label, icon: Icon }) => <button key={key} className={page === key ? 'active' : ''} onClick={() => goTo(key)}><Icon size={18} /><span>{label}</span></button>)}</section>)}</nav><div className="sidebar-footer"><div className="signed-user"><div className="avatar"><img src={profile.profilePhotoUrl || logo} alt="Logo del espacio" /></div><div><strong>{profile.displayName}</strong><span>{isSuperAdmin ? 'Administrador supremo' : profile.roles.map(roleLabel).join(' - ')}</span></div></div><button className="logout" onClick={onLogout}><LogOut size={17} />Cerrar sesión</button></div></aside>
    <div className="workspace"><header className="topbar"><button className="icon menu-button" onClick={() => setMobileOpen(true)}><Menu size={21} /></button><div><span className="connection-dot" />{isSuperAdmin ? 'Panel de administración' : brandName}</div>{isSuperAdmin && <select className="workspace-switcher" value={supremeMode} onChange={e => { const mode = e.target.value as 'technology' | 'makers'; setSupremeMode(mode); sessionStorage.setItem('sanjcorp.mode', mode); goTo('supreme') }}><option value="technology">{brandName}</option><option value="makers">Makers</option></select>}<button type="button" className="theme-toggle" onClick={onToggleTheme}>{theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}{theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}</button>{!isSuperAdmin && <InventoryAlerts onOpen={() => goTo('consumables')} />}<details className="profile-menu"><summary className="profile-chip"><span>{profile.displayName}</span><div className="avatar mini"><img src={profile.profilePhotoUrl || logo} alt="Logo del espacio" /></div></summary><div className="profile-popover"><small>{isSuperAdmin ? 'Administrador supremo' : profile.roles.map(roleLabel).join(' - ')}</small>{!isSuperAdmin && <button onClick={() => goTo('settings')}>Configuración</button>}<button onClick={onLogout}>Cerrar sesión</button></div></details></header><main className="page-content">{content}</main><footer className="site-copyright">Atlas Impresiones 3D - Todos los derechos reservados.</footer></div>
  </div>
}

function InventoryAlerts({ onOpen }: { onOpen: () => void }) {
  const [alerts, setAlerts] = useState<InventoryAlert[]>([])
  const [open, setOpen] = useState(false)

  useEffect(() => {
    let active = true
    const load = () => { api.alerts().then(next => { if (active) setAlerts(next) }).catch(() => undefined) }
    load()
    const interval = window.setInterval(load, 30_000)
    const listener = () => load()
    window.addEventListener('inventory-changed', listener)
    return () => { active = false; window.clearInterval(interval); window.removeEventListener('inventory-changed', listener) }
  }, [])

  return <div className="alerts-menu">
    <button className={`icon ghost alerts-button ${alerts.length > 0 ? 'has-alerts' : ''}`} aria-label={`Alarmas de inventario${alerts.length ? `: ${alerts.length}` : ''}`} aria-expanded={open} onClick={() => setOpen(current => !current)}><Bell size={18} />{alerts.length > 0 && <span className="notification-badge">{alerts.length > 99 ? '99+' : alerts.length}</span>}</button>
    {open && <div className="alerts-popover" role="dialog" aria-label="Alarmas de inventario"><div className="alerts-popover-header"><div><p className="eyebrow">INVENTARIO</p><h3>Alarmas</h3></div><button className="icon ghost" aria-label="Cerrar alarmas" onClick={() => setOpen(false)}><X size={16} /></button></div>{alerts.length === 0 ? <p className="alerts-empty">No hay filamentos bajo el umbral.</p> : <div className="alerts-list">{alerts.map(alert => <button key={alert.id} className={`inventory-alert ${alert.severity}`} onClick={() => { setOpen(false); onOpen() }}><AlertTriangle size={17} /><span><strong>{alert.severity === 'out' ? 'Agotado' : 'Stock bajo'}</strong><small>{alert.message}</small></span></button>)}</div>}</div>}
  </div>
}






