/* eslint-disable react-refresh/only-export-components */
import type { ReactNode } from 'react'
import { AlertCircle, CheckCircle2, Inbox, LoaderCircle } from 'lucide-react'

export function todayInput(date = new Date()) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

export function firstDayOfMonth() {
  const now = new Date()
  return todayInput(new Date(now.getFullYear(), now.getMonth(), 1))
}

export function formatDate(value?: string) {
  return value ? new Intl.DateTimeFormat('es-BO', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—'
}

export function money(value: number, symbol = 'Bs') {
  return `${symbol} ${Number(value || 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

export function number(value: number, digits = 2) {
  return Number(value || 0).toLocaleString('es-BO', { maximumFractionDigits: digits })
}

export function weight(value: number) {
  const grams = Math.max(0, Number(value || 0))
  const kilos = Math.floor(grams / 1000)
  const remainder = Number((grams - kilos * 1000).toFixed(2))
  if (kilos > 0 && remainder > 0) return `${number(kilos, 0)} kg ${number(remainder)} g`
  if (kilos > 0) return `${number(kilos, 0)} kg`
  return `${number(remainder)} g`
}

export function roleLabel(role: string) {
  return ({ Administrator: 'Administrador', SuperAdmin: 'Administrador supremo', Maker: 'Maker', Sales: 'Ventas', Production: 'Producción', Viewer: 'Consulta' } as Record<string, string>)[role] ?? role
}

export function hasAnyRole(roles: string[], allowed: string[]) {
  return roles.some(role => allowed.includes(role))
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description: string; actions?: ReactNode }) {
  return <div className="page-header">
    <div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p>{description}</p></div>
    {actions && <div className="page-actions">{actions}</div>}
  </div>
}

export function Loading({ label = 'Cargando…' }: { label?: string }) {
  return <div className="loading-block"><LoaderCircle className="spin" size={22} /><span>{label}</span></div>
}

export function ErrorMessage({ error }: { error?: string }) {
  return error ? <div className="alert error" role="alert"><AlertCircle size={18} /><span>{error}</span></div> : null
}

export function SuccessMessage({ message }: { message?: string }) {
  return message ? <div className="alert success" role="status"><CheckCircle2 size={18} /><span>{message}</span></div> : null
}

export function Empty({ children = 'Todavia no hay informacion para mostrar.' }: { children?: ReactNode }) {
  return <div className="empty-state"><Inbox size={28} /><span>{children}</span></div>
}

export function Status({ active, trueLabel = 'ACTIVO', falseLabel = 'ARCHIVADO' }: { active: boolean; trueLabel?: string; falseLabel?: string }) {
  return <span className={`status ${active ? 'ok' : 'muted-status'}`}>{active ? trueLabel : falseLabel}</span>
}

export function Metric({ label, value, icon }: { label: string; value: ReactNode; icon: ReactNode }) {
  return <article className="metric-card"><div className="metric-icon">{icon}</div><p>{label}</p><strong>{value}</strong></article>
}



