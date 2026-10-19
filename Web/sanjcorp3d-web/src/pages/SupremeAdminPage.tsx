import { FormEvent, useCallback, useEffect, useState } from 'react'
import { CheckCircle2, ImagePlus, Plus, UserRoundX, XCircle } from 'lucide-react'
import { api } from '../api'
import type { Tenant } from '../types'
import { ErrorMessage, Loading, PageHeader } from '../ui'
import { confirmDialog } from '../confirm'

export function SupremeAdminPage() {
  const [tenants, setTenants] = useState<Tenant[]>([])
  const [creating, setCreating] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const load = useCallback(async () => { setError(''); setTenants(await api.tenants()) }, [])
  useEffect(() => { load().catch(reason => setError((reason as Error).message)) }, [load])
  async function createCompany(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); setBusy(true); setError(''); try { const logo = form.get('logo') as File | null; const logoUrl = logo?.size ? await readImage(logo) : undefined; await api.createMakerTenant({ name: String(form.get('name')), slug: String(form.get('slug')), username: String(form.get('username')), displayName: String(form.get('displayName')), email: String(form.get('email') || ''), password: String(form.get('password')), logoUrl }); setCreating(false); await load() } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  async function toggle(item: Tenant) { setBusy(true); setError(''); try { await api.updateTenant(item.id, { name: item.name, logoUrl: item.logoUrl, active: !item.active }); await load() } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  async function remove(item: Tenant) { if (!await confirmDialog({ title: 'Eliminar empresa', message: 'Se eliminarÃ¡n permanentemente los usuarios y datos de esta empresa.', highlight: item.name, confirmLabel: 'Eliminar empresa', variant: 'danger' })) return; setBusy(true); setError(''); try { await api.deleteMakerTenant(item.id); await load() } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  return <>
    <PageHeader eyebrow="PLATAFORMA" title="AdministraciÃ³n de empresas" description="Crea y administra cuentas empresariales independientes. No se muestra informaciÃ³n operativa de cada empresa." actions={<button onClick={() => { setError(''); setCreating(true) }}><Plus size={16} />Nueva empresa</button>} />
    <ErrorMessage error={error} />
    {creating && <form className="panel editor top-gap" onSubmit={createCompany}><div className="section-title"><div><p className="eyebrow">NUEVA EMPRESA</p><h2>Cuenta independiente</h2></div><button type="button" className="ghost" onClick={() => setCreating(false)}><XCircle size={16} />Cancelar</button></div><label>Nombre de la empresa<input name="name" placeholder="Ej. Delta 3D" required /></label><label>Identificador<input name="slug" placeholder="delta-3d" pattern="[A-Za-z0-9-]+" required /></label><label>Logo opcional<input name="logo" type="file" accept="image/*" /></label><hr /><label>Usuario administrador<input name="username" required /></label><label>Nombre del administrador<input name="displayName" required /></label><label>Correo opcional<input name="email" type="email" /></label><label>ContraseÃ±a inicial<input name="password" type="password" minLength={9} required /></label><button disabled={busy}>{busy ? 'Creando...' : 'Crear empresa'}</button></form>}
    {!creating && (tenants.length === 0 ? <Loading /> : <section className="panel maker-list"><div className="section-title"><div><p className="eyebrow">EMPRESAS</p><h2>Cuentas activas e inactivas</h2></div><span className="muted">{tenants.length} empresa{tenants.length === 1 ? '' : 's'}</span></div><div className="maker-cards">{tenants.map(item => <article className="maker-card" key={item.id}><div className="maker-card-select">{item.logoUrl ? <img className="company-logo" src={item.logoUrl} alt="" /> : <ImagePlus size={20} />}<div><strong>{item.name}</strong><small>{item.userCount} usuario{item.userCount === 1 ? '' : 's'} Â· {item.active ? 'Activa' : 'Inactiva'}</small></div></div><button className={`ghost small ${item.active ? 'danger' : ''}`} disabled={busy} onClick={() => toggle(item)}>{item.active ? <><UserRoundX size={15} />Inactivar</> : <><CheckCircle2 size={15} />Activar</>}</button>{item.kind !== 'technology' && <button className="ghost small danger" disabled={busy} onClick={() => remove(item)}>Eliminar</button>}</article>)}</div></section>)}
  </>
}
function readImage(file: File) { if (file.size > 1_500_000) return Promise.reject(new Error('El logo debe pesar como mÃ¡ximo 1.5 MB.')); return new Promise<string>((resolve, reject) => { const reader = new FileReader(); reader.onload = () => resolve(String(reader.result)); reader.onerror = () => reject(new Error('No se pudo leer el logo.')); reader.readAsDataURL(file) }) }
