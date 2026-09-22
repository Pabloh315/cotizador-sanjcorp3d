import { createRoot } from 'react-dom/client'
import { AlertTriangle, CheckCircle2, X } from 'lucide-react'

type ConfirmOptions = {
  title: string
  message: string
  highlight?: string
  confirmLabel?: string
  cancelLabel?: string
  variant?: 'default' | 'danger' | 'success'
}

export function confirmDialog(options: ConfirmOptions) {
  return new Promise<boolean>(resolve => {
    const host = document.createElement('div')
    document.body.appendChild(host)
    const root = createRoot(host)
    const close = (value: boolean) => {
      root.unmount()
      host.remove()
      resolve(value)
    }
    root.render(<ConfirmModal {...options} onClose={close} />)
  })
}

function ConfirmModal({ title, message, highlight, confirmLabel = 'Sí, continuar', cancelLabel = 'Cancelar', variant = 'default', onClose }: ConfirmOptions & { onClose: (value: boolean) => void }) {
  const Icon = variant === 'success' ? CheckCircle2 : AlertTriangle
  return <div className="confirm-overlay" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) onClose(false) }}>
    <section className={`confirm-dialog ${variant}`} role="dialog" aria-modal="true" aria-labelledby="confirm-title">
      <button className="icon ghost confirm-close" aria-label="Cerrar" onClick={() => onClose(false)}><X size={18} /></button>
      <div className="confirm-icon"><Icon size={24} /></div>
      <p className="eyebrow">CONFIRMACIÓN</p>
      <h2 id="confirm-title">{title}</h2>
      <p>{message}</p>
      {highlight && <strong className="confirm-highlight">{highlight}</strong>}
      <div className="confirm-actions">
        <button type="button" className="ghost" onClick={() => onClose(false)}>{cancelLabel}</button>
        <button type="button" className={variant === 'danger' ? 'danger-button' : ''} onClick={() => onClose(true)}>{confirmLabel}</button>
      </div>
    </section>
  </div>
}
