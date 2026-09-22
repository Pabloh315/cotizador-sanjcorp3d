import { FormEvent, useCallback, useEffect, useState } from 'react'
import { BarChart3, CheckCircle2, MessageCircle, Plus, Printer, ShieldCheck, UserPlus, UserRoundX, Users, XCircle } from 'lucide-react'
import { api } from '../api'
import { PrintersPage } from './CatalogPages'
import type { Profile, Report, Tenant, UserAccount } from '../types'
import { ErrorMessage, Loading, Metric, PageHeader, firstDayOfMonth, money, todayInput } from '../ui'
import { confirmDialog } from '../confirm'
import { ChatPage } from './ChatPage'

type Mode = 'technology' | 'makers'

export function SupremeAdminPage({ profile, mode }: { profile: Profile; mode: Mode }) {
  const [tenants, setTenants] = useState<Tenant[]>([])
  const [selected, setSelected] = useState('')
  const [tab, setTab] = useState<'summary' | 'users' | 'printers' | 'chat'>('summary')
  const [report, setReport] = useState<Report>()
  const [users, setUsers] = useState<UserAccount[]>([])
  const [userId, setUserId] = useState('')
  const [from, setFrom] = useState(firstDayOfMonth())
  const [to, setTo] = useState(todayInput())
  const [draft, setDraft] = useState(false)
  const [makerDraft, setMakerDraft] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const technology = profile.workspaces?.find(x => x.kind === 'technology')?.id ?? '00000000-0000-0000-0000-000000000001'
  const makers = tenants.filter(x => x.kind === 'maker')

  const loadTenants = useCallback(async () => {
    setError('')
    const items = await api.tenants()
    setTenants(items)
    setSelected(current => {
      const currentTenant = items.find(x => x.id === current)
      if (mode === 'makers') return currentTenant?.kind === 'maker' ? current : (items.find(x => x.kind === 'maker')?.id ?? '')
      return currentTenant?.kind === 'technology' ? current : technology
    })
  }, [mode, technology])
  useEffect(() => { loadTenants().catch(e => setError((e as Error).message)) }, [loadTenants])
  const tenant = tenants.find(x => x.id === selected)
  const isMaker = tenant?.kind === 'maker'

  useEffect(() => {
    if (!selected) { setReport(undefined); setUsers([]); return }
    sessionStorage.setItem('sanjcorp.tenant', selected)
    setBusy(true); setError('')
    Promise.all([api.report(from, to, isMaker && userId ? userId : undefined), api.users()])
      .then(([next, accounts]) => { setReport(next); setUsers(accounts) })
      .catch(e => setError((e as Error).message)).finally(() => setBusy(false))
  }, [selected, from, to, userId, isMaker])

  async function createUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selected) return
    const form = new FormData(event.currentTarget); setBusy(true); setError('')
    try {
      const photo = form.get('profilePhoto') as File | null
      const profilePhotoUrl = photo && photo.size ? await readPhoto(photo) : undefined
      const item = { username: String(form.get('username')), displayName: String(form.get('displayName')), email: String(form.get('email') || ''), password: String(form.get('password')), profilePhotoUrl }
      if (isMaker) await api.createMakerUser(selected, item)
      else await api.createUser({ ...item, role: String(form.get('role')) })
      setDraft(false); setUsers(await api.users())
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  async function createMaker(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget); setBusy(true); setError('')
    try {
      await api.createMakerTenant({ name: String(form.get('name')), slug: String(form.get('slug')), username: String(form.get('username')), displayName: String(form.get('displayName')), email: String(form.get('email') || ''), password: String(form.get('password')) })
      setMakerDraft(false); await loadTenants()
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  async function toggleMaker(item: Tenant) {
    setBusy(true); setError('')
    try { await api.updateTenant(item.id, { name: item.name, logoUrl: item.logoUrl, active: !item.active }); await loadTenants() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  async function deleteMaker(item: Tenant) {
    if (!await confirmDialog({ title: 'Eliminar espacio Maker', message: 'Se eliminarán permanentemente sus usuarios y todos sus datos. Esta acción no se puede deshacer.', highlight: item.name, confirmLabel: 'Sí, eliminar', variant: 'danger' })) return
    setBusy(true); setError('')
    try { await api.deleteMakerTenant(item.id); if (selected === item.id) setSelected(''); await loadTenants() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  async function deleteUser(item: UserAccount) {
    if (!await confirmDialog({ title: 'Eliminar usuario', message: 'Esta acción eliminará permanentemente la cuenta seleccionada.', highlight: item.displayName, confirmLabel: 'Sí, eliminar', variant: 'danger' })) return
    setBusy(true); setError('')
    try { await api.deleteUser(item.id); setUsers(await api.users()) }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  return <>
    <PageHeader eyebrow="ADMINISTRACIÓN SUPREMA" title={mode === 'makers' ? 'Makers' : 'Atlas Impresiones 3D'} description="Consulta reportes y administra los accesos sin mezclar datos entre espacios." actions={mode === 'makers' && tab !== 'printers' && tab !== 'chat' ? <button onClick={() => { setError(''); setMakerDraft(true) }}><Plus size={16} />Nueva cuenta Maker</button> : undefined} />
    <ErrorMessage error={error} />
    <div className="tabs"><button className={tab === 'users' ? 'active' : ''} onClick={() => setTab('users')}><Users size={16} />Usuarios</button><button className={tab === 'summary' ? 'active' : ''} onClick={() => setTab('summary')}><BarChart3 size={16} />Reportes de fallas</button><button className={tab === 'chat' ? 'active' : ''} onClick={() => setTab('chat')}><MessageCircle size={16} />Chat</button><button className={tab === 'printers' ? 'active' : ''} onClick={() => setTab('printers')}><Printer size={16} />Impresoras globales</button></div>
    {tab === 'chat' && selected && <ChatPage profile={profile} title={`Chat con ${tenant?.name ?? 'Maker'}`} />}
    {tab === 'printers' && <PrintersPage canManage />}
    {mode === 'makers' && tab !== 'printers' && tab !== 'chat' && <section className="panel maker-list"><div className="section-title"><div><p className="eyebrow">CUENTAS MAKER</p><h2>Espacios independientes</h2></div><span className="muted">{makers.length} cuenta{makers.length === 1 ? '' : 's'}</span></div>{makers.length === 0 ? <p className="muted">Todavía no hay cuentas Maker.</p> : <div className="maker-cards">{makers.map(item => <article className={`maker-card ${selected === item.id ? 'selected' : ''}`} key={item.id}><button className="maker-card-select" onClick={() => { setSelected(item.id); setUserId('') }}><strong>{item.name}</strong><small>{item.userCount} de 3 usuarios · {item.active ? 'Activa' : 'Inactiva'}</small></button><button className={`ghost small ${item.active ? 'danger' : ''}`} disabled={busy} onClick={() => toggleMaker(item)}>{item.active ? <><UserRoundX size={15} />Inactivar</> : <><CheckCircle2 size={15} />Activar</>}</button><button className="ghost small danger" disabled={busy} onClick={() => deleteMaker(item)} aria-label={`Eliminar ${item.name}`}>Eliminar</button></article>)}</div>}</section>}
    {tab === 'summary' && <><section className="panel filters-panel"><div className="filters"><label>Espacio<select value={selected} onChange={e => { setSelected(e.target.value); setUserId('') }}>{mode === 'technology' ? <option value={technology}>Atlas Impresiones 3D</option> : makers.map(x => <option key={x.id} value={x.id} disabled={!x.active}>{x.name}{x.active ? '' : ' (inactiva)'}</option>)}</select></label><label>Desde<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Hasta<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label>{isMaker && <label>Usuario Maker<select value={userId} onChange={e => setUserId(e.target.value)}><option value="">Todos los usuarios</option>{users.map(x => <option key={x.id} value={x.id}>{x.displayName} (@{x.userName})</option>)}</select></label>}</div></section>{busy || !report ? <Loading /> : <section className="metrics report-metrics"><Metric label="Ventas confirmadas" value={report.saleCount} icon={<BarChart3 size={20} />} /><Metric label="Ingresos reales" value={money(report.salesRevenue)} icon={<BarChart3 size={20} />} /><Metric label="Ganancia real" value={money(report.salesProfit)} icon={<ShieldCheck size={20} />} /><Metric label="Cotizaciones" value={report.quoteCount} icon={<BarChart3 size={20} />} /></section>}</>}
    {tab === 'users' && <section className="panel"><div className="section-title"><div><p className="eyebrow">{tenant?.name ?? 'ESPACIO'}</p><h2>Usuarios y accesos</h2></div><button disabled={!tenant?.active || (isMaker && users.length >= 3)} onClick={() => setDraft(true)}><UserPlus size={16} />Agregar usuario</button></div>{isMaker && <p className="field-help">Una cuenta Maker admite hasta tres usuarios y comparte datos únicamente dentro de este espacio.</p>}{users.length === 0 ? <p className="muted">No hay usuarios en este espacio.</p> : <div className="user-cards">{users.map(x => <article className="user-card" key={x.id}><div className="avatar">{x.profilePhotoUrl ? <img src={x.profilePhotoUrl} alt="" /> : x.displayName.slice(0, 2).toUpperCase()}</div><div className="user-card-main"><h3>{x.displayName}</h3><p>@{x.userName}{x.email ? ` · ${x.email}` : ''}</p></div><strong>{x.role}</strong><button className="ghost small danger" disabled={busy} onClick={() => deleteUser(x)}>Eliminar</button></article>)}</div>}{draft && <form className="panel editor top-gap" onSubmit={createUser}><div className="section-title"><h2>Nuevo usuario</h2><button type="button" className="ghost" onClick={() => setDraft(false)}><XCircle size={16} />Cancelar</button></div><label>Usuario<input name="username" required /></label><label>Nombre visible<input name="displayName" required /></label><label>Foto de perfil opcional<input name="profilePhoto" type="file" accept="image/*" /></label><label>Correo opcional<input name="email" type="email" /></label>{!isMaker && <label>Rol<select name="role" defaultValue="Viewer"><option>Administrator</option><option>Sales</option><option>Production</option><option>Viewer</option></select></label>}<label>Contraseña inicial<input name="password" type="password" minLength={9} required /></label><button disabled={busy}>{busy ? 'Guardando…' : 'Crear usuario'}</button></form>}</section>}
    {makerDraft && tab !== 'printers' && tab !== 'chat' && <form className="panel editor top-gap" onSubmit={createMaker}><div className="section-title"><div><p className="eyebrow">NUEVO ESPACIO AISLADO</p><h2>Crear cuenta Maker</h2></div><button type="button" className="ghost" onClick={() => setMakerDraft(false)}><XCircle size={16} />Cancelar</button></div><label>Nombre de la cuenta<input name="name" placeholder="Ej. Maker de Ana" required /></label><label>Identificador<input name="slug" placeholder="maker-ana" pattern="[A-Za-z0-9-]+" required /></label><label>Usuario principal<input name="username" required /></label><label>Nombre visible<input name="displayName" required /></label><label>Correo opcional<input name="email" type="email" /></label><label>Contraseña inicial<input name="password" type="password" minLength={9} required /></label><button disabled={busy}>{busy ? 'Creando…' : 'Crear cuenta Maker'}</button></form>}
  </>
}

function readPhoto(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result))
    reader.onerror = () => reject(new Error('No se pudo leer la foto de perfil.'))
    reader.readAsDataURL(file)
  })
}





