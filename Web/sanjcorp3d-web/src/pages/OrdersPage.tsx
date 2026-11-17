import { useEffect, useMemo, useState } from 'react'
import { AlertTriangle, CheckCircle2, Play, RefreshCw, Snowflake, TimerReset, X } from 'lucide-react'
import { api } from '../api'
import type { PrintOrder } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, Status, formatDate, money, number } from '../ui'
import { confirmDialog } from '../confirm'

export function OrdersPage({ canManage }: { canManage: boolean }) {
  const [items, setItems] = useState<PrintOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<number>()
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [failureOrder, setFailureOrder] = useState<PrintOrder>()
  const [failureReason, setFailureReason] = useState('')
  const [failureGrams, setFailureGrams] = useState<Record<number, number>>({})

  const load = () => {
    setLoading(true)
    setError('')
    api.printOrders().then(setItems).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
  }

  useEffect(load, [])

  const groups = useMemo(() => {
    const map = new Map<number, { printerId: number; printerName: string; printerStatus: string; rows: PrintOrder[] }>()
    for (const item of items) {
      if (!map.has(item.printerId)) map.set(item.printerId, { printerId: item.printerId, printerName: item.printerName, printerStatus: item.printerStatus, rows: [] })
      map.get(item.printerId)!.rows.push(item)
    }
    return Array.from(map.values()).map(group => ({ ...group, rows: group.rows.sort((a, b) => new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime() || a.id - b.id) }))
  }, [items])
  const failureMaterials = useMemo(() => {
    const map = new Map<number, PrintOrder['consumables'][number]>()
    for (const line of failureOrder?.consumables ?? []) {
      const current = map.get(line.consumableId)
      map.set(line.consumableId, current ? { ...current, plannedGrams: current.plannedGrams + line.plannedGrams } : line)
    }
    return Array.from(map.values())
  }, [failureOrder])

  async function action(id: number, task: 'start' | 'finish' | 'cooling') {
    const item = items.find(row => row.id === id)
    const confirmed = await confirmDialog({
      title: task === 'start' ? 'Empezar impresión' : task === 'finish' ? 'Marcar como terminado' : 'Finalizar enfriamiento',
      message: task === 'start' ? 'La impresora pasará a estado en uso y comenzará el tiempo estimado.' : task === 'finish' ? 'La impresora entrará en etapa de enfriamiento antes del siguiente pedido.' : 'La impresora volverá a quedar disponible para el siguiente pedido.',
      highlight: item ? `${item.orderCode} · ${item.printerName}` : undefined,
      confirmLabel: 'Sí, continuar',
      variant: task === 'finish' ? 'success' : 'default'
    })
    if (!confirmed) return
    setBusyId(id); setError(''); setSuccess('')
    try {
      if (task === 'start') await api.startPrintOrder(id)
      if (task === 'finish') await api.finishPrintOrder(id)
      if (task === 'cooling') await api.completePrintOrderCooling(id)
      setSuccess(task === 'start' ? 'Impresión iniciada.' : task === 'finish' ? 'Impresión terminada; la impresora entró en enfriamiento.' : 'Enfriamiento finalizado; impresora disponible.')
      await api.printOrders().then(setItems)
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusyId(undefined) }
  }

  function openFailure(item: PrintOrder) {
    setFailureOrder(item); setFailureReason(''); setFailureGrams(Object.fromEntries(item.consumables.map(line => [line.consumableId, 0]))); setError(''); setSuccess('')
  }

  async function cancelFailedPrint() {
    if (!failureOrder) return
    const consumables = failureMaterials.map(line => ({ consumableId: line.consumableId, grams: Math.max(0, failureGrams[line.consumableId] ?? 0) })).filter(line => line.grams > 0)
    const total = consumables.reduce((sum, line) => sum + line.grams, 0)
    if (total <= 0) { setError('Indica cuántos gramos se desperdiciaron en la impresión.'); return }
    setBusyId(failureOrder.id); setError(''); setSuccess('')
    try {
      await api.cancelPrintOrder(failureOrder.id, failureReason, consumables)
      setFailureOrder(undefined)
      setSuccess(`Impresión cancelada. Se descontaron ${number(total)} g y se registraron como pérdida.`)
      await api.printOrders().then(setItems)
      window.dispatchEvent(new Event('inventory-changed'))
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusyId(undefined) }
  }

  return <>
    <PageHeader eyebrow="PRODUCCIÓN" title="Pedidos" description="Cada impresora trabaja con un solo pedido asignado; no puede recibir otro hasta terminar y enfriarse." actions={<button className="secondary" onClick={load}><RefreshCw size={16} />Actualizar</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {loading ? <Loading /> : groups.length === 0 ? <Empty>No hay pedidos asignados todavía. Cuando una venta se confirme con impresora libre aparecerá aquí.</Empty> : <div className="orders-board">{groups.map(group => {
      const hasActive = group.rows.some(x => x.status === 'Imprimiendo' || x.status === 'Enfriamiento')
      const firstPendingId = group.rows.find(x => x.status === 'Pendiente')?.id
      return <section className="panel order-printer" key={group.printerId}>
        <div className="section-title"><div><p className="eyebrow">IMPRESORA</p><h2>{group.printerName}</h2></div><span className={`machine-status ${group.printerStatus.toLowerCase().replaceAll(' ', '-')}`}>{group.printerStatus}</span></div>
        <div className="table-wrap"><table><thead><tr><th>Fila</th><th>Pedido</th><th>Cliente / proyecto</th><th>Tiempo</th><th>Estado</th><th>Fechas</th><th>Precio</th>{canManage && <th>Acción</th>}</tr></thead><tbody>{group.rows.map((item, index) => <tr key={item.id} className={item.status === 'Terminado' || item.status === 'Cancelado' ? 'archived-row' : ''}>
          <td><strong>{index + 1}</strong></td>
          <td className="mono">#{item.quoteId}<small className="cell-note">{item.orderCode}</small></td>
          <td><strong>{item.customer}</strong><small className="cell-note">{item.productName || item.projectName || 'Servicio de impresión'}</small></td>
          <td>{number(item.printHours, 2)} h<small className="cell-note">{item.quantity} pieza(s)</small></td>
          <td><OrderStatus status={item.status} /></td>
          <td><OrderDates item={item} /></td>
          <td>{money(item.recommendedPrice)}</td>
          {canManage && <td><OrderAction item={item} busy={busyId === item.id} disabled={(item.status === 'Pendiente' && (hasActive || firstPendingId !== item.id)) || busyId !== undefined} onAction={action} onFailure={openFailure} /></td>}
        </tr>)}</tbody></table></div>
      </section>
    })}</div>}
    {failureOrder && <div className="confirm-overlay" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget && !busyId) setFailureOrder(undefined) }}><section className="confirm-dialog danger print-failure-dialog" role="dialog" aria-modal="true" aria-labelledby="failure-title"><button className="icon ghost confirm-close" disabled={Boolean(busyId)} onClick={() => setFailureOrder(undefined)} aria-label="Cerrar"><X size={18} /></button><div className="confirm-icon"><AlertTriangle size={24} /></div><p className="eyebrow">IMPRESIÓN FALLIDA</p><h2 id="failure-title">Cancelar y registrar pérdida</h2><p>Pedido {failureOrder.orderCode} · {failureOrder.printerName}. Indica el material realmente desperdiciado; se descontará inmediatamente del inventario.</p><div className="failure-materials">{failureMaterials.map(line => <label key={line.consumableId}><span><strong>{line.name} · {line.material} · {line.color}</strong><small>Planificado: {number(line.plannedGrams)} g</small></span><input type="number" min="0" step="0.01" value={failureGrams[line.consumableId] ?? 0} onChange={event => setFailureGrams(current => ({ ...current, [line.consumableId]: Number(event.target.value) }))} /><b>g</b></label>)}</div><label className="failure-reason">Motivo de la falla<textarea rows={3} maxLength={240} value={failureReason} onChange={event => setFailureReason(event.target.value)} placeholder="Ej. se desprendió de la cama, corte de energía..." /></label><div className="confirm-actions"><button type="button" className="ghost" disabled={Boolean(busyId)} onClick={() => setFailureOrder(undefined)}>Volver</button><button type="button" className="danger-button" disabled={Boolean(busyId)} onClick={cancelFailedPrint}>{busyId ? <TimerReset size={15} className="spin" /> : <AlertTriangle size={15} />}Cancelar impresión y descontar</button></div></section></div>}
  </>
}

function OrderStatus({ status }: { status: PrintOrder['status'] }) {
  if (status === 'Terminado') return <Status active trueLabel="TERMINADO" />
  if (status === 'Cancelado') return <span className="status danger-status">CANCELADO</span>
  if (status === 'Imprimiendo') return <span className="status warning">IMPRIMIENDO</span>
  if (status === 'Enfriamiento') return <span className="status muted-status">ENFRIAMIENTO</span>
  return <span className="status warning">PENDIENTE</span>
}

function OrderDates({ item }: { item: PrintOrder }) {
  const target = item.status === 'Imprimiendo' ? item.estimatedFinishedAtUtc : item.status === 'Enfriamiento' ? item.coolingUntilUtc : item.completedAtUtc || item.startedAtUtc
  return <span className="order-dates"><small>Creado: {formatDate(item.createdAtUtc)}</small>{target && <small>{item.status === 'Imprimiendo' ? 'Estimado' : item.status === 'Enfriamiento' ? 'Enfriar hasta' : 'Último cambio'}: {formatDate(target)}</small>}</span>
}

function OrderAction({ item, busy, disabled, onAction, onFailure }: { item: PrintOrder; busy: boolean; disabled: boolean; onAction: (id: number, task: 'start' | 'finish' | 'cooling') => void; onFailure: (item: PrintOrder) => void }) {
  if (item.status === 'Pendiente') return <button className="small" disabled={disabled} onClick={() => onAction(item.id, 'start')}>{busy ? <TimerReset size={15} className="spin" /> : <Play size={15} />}Empezar impresión</button>
  if (item.status === 'Imprimiendo') return <div className="order-action-group"><button className="small secondary" disabled={busy} onClick={() => onAction(item.id, 'finish')}>{busy ? <TimerReset size={15} className="spin" /> : <CheckCircle2 size={15} />}Terminado</button><button className="small danger-button" disabled={busy} onClick={() => onFailure(item)}><AlertTriangle size={15} />Falló</button></div>
  if (item.status === 'Enfriamiento') return <div className="order-action-group"><button className="small ghost" disabled={busy} onClick={() => onAction(item.id, 'cooling')}>{busy ? <TimerReset size={15} className="spin" /> : <Snowflake size={15} />}Finalizar enfriamiento</button><button className="small danger-button" disabled={busy} onClick={() => onFailure(item)}><AlertTriangle size={15} />Falló</button></div>
  return <span className="muted">Completado</span>
}



