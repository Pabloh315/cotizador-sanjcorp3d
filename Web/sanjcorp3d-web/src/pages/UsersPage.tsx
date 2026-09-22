import { FormEvent, useCallback, useEffect, useState } from 'react'
import { Edit3, KeyRound, Plus, Shield, UserRoundCheck, UserRoundX } from 'lucide-react'
import { api } from '../api'
import type { UserAccount } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, Status, SuccessMessage, formatDate, roleLabel } from '../ui'

type Draft = { id?: string; username: string; displayName: string; email: string; role: string; active: boolean; password: string }
const blank: Draft = { username: '', displayName: '', email: '', role: 'Sales', active: true, password: '' }

export function UsersPage() {
  const [items, setItems] = useState<UserAccount[]>([]); const [roles, setRoles] = useState<string[]>([]); const [draft, setDraft] = useState<Draft | null>(null)
  const [error, setError] = useState(''); const [success, setSuccess] = useState(''); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false)
  const load = useCallback(() => { setLoading(true); Promise.all([api.users(), api.roles()]).then(([accounts, availableRoles]) => { setItems(accounts); setRoles(availableRoles) }).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false)) }, [])
  useEffect(load, [load])
  function edit(item: UserAccount) { setDraft({ id: item.id, username: item.userName, displayName: item.displayName, email: item.email ?? '', role: item.role, active: item.active, password: '' }); setError(''); setSuccess('') }
  async function save(event: FormEvent) {
    event.preventDefault(); if (!draft) return; setBusy(true); setError(''); setSuccess('')
    try {
      if (draft.id) {
        await api.updateUser(draft.id, { displayName: draft.displayName, email: draft.email || undefined, role: draft.role, active: draft.active })
        if (draft.password) await api.resetPassword(draft.id, draft.password)
        setSuccess('Cuenta actualizada.')
      } else {
        await api.createUser({ username: draft.username, displayName: draft.displayName, email: draft.email || undefined, password: draft.password, role: draft.role })
        setSuccess('Cuenta creada correctamente.')
      }
      setDraft(null); load()
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }
  return <>
    <PageHeader eyebrow="ADMINISTRACIÓN" title="Usuarios y permisos" description="Crea accesos individuales y limita lo que cada persona puede modificar." actions={<button onClick={() => setDraft({ ...blank })}><Plus size={17} />Nuevo usuario</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <div className={`catalog-layout ${draft ? 'with-editor' : ''}`}><section className="panel">{loading ? <Loading /> : items.length === 0 ? <Empty>No hay cuentas registradas.</Empty> : <div className="user-cards">{items.map(item => <article className="user-card" key={item.id}><div className={`avatar ${item.active ? '' : 'inactive'}`}>{item.displayName.slice(0, 2).toUpperCase()}</div><div className="user-card-main"><div><h3>{item.displayName}</h3><Status active={item.active} /></div><p>@{item.userName}{item.email ? ` · ${item.email}` : ''}</p><small>Último acceso: {formatDate(item.lastLoginAtUtc)}</small></div><div className="user-role"><Shield size={15} />{roleLabel(item.role)}{item.twoFactorEnabled && <span title="2FA activado"><KeyRound size={14} /></span>}</div><button className="ghost small" onClick={() => edit(item)}><Edit3 size={15} />Administrar</button></article>)}</div>}</section>
      {draft && <form className="panel editor" onSubmit={save}><div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR CUENTA' : 'NUEVO ACCESO'}</p><h2>{draft.id ? draft.displayName : 'Crear usuario'}</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}>×</button></div><label>Usuario<input required disabled={Boolean(draft.id)} autoComplete="off" value={draft.username} onChange={e => setDraft({ ...draft, username: e.target.value })} /></label><label>Nombre visible<input required value={draft.displayName} onChange={e => setDraft({ ...draft, displayName: e.target.value })} /></label><label>Correo opcional<input type="email" value={draft.email} onChange={e => setDraft({ ...draft, email: e.target.value })} /></label><label>Rol<select value={draft.role} onChange={e => setDraft({ ...draft, role: e.target.value })}>{roles.map(role => <option key={role} value={role}>{roleLabel(role)}</option>)}</select><small className="field-help">Administrador: todo · Ventas: cotizaciones/ventas · Producción: catálogos · Consulta: solo lectura.</small></label><label>{draft.id ? 'Nueva contraseña (opcional)' : 'Contraseña inicial'}<input type="password" required={!draft.id} minLength={9} autoComplete="new-password" value={draft.password} onChange={e => setDraft({ ...draft, password: e.target.value })} /><small className="field-help">Mínimo 9 caracteres, una minúscula y un número.</small></label>{draft.id && <label className="check account-toggle"><input type="checkbox" checked={draft.active} onChange={e => setDraft({ ...draft, active: e.target.checked })} />{draft.active ? <UserRoundCheck size={17} /> : <UserRoundX size={17} />}{draft.active ? 'Cuenta activa' : 'Cuenta desactivada'}</label>}<div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button disabled={busy}>{busy ? 'Guardando…' : 'Guardar cuenta'}</button></div></form>}
    </div>
  </>
}



