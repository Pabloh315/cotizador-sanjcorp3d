import { useEffect, useMemo, useState } from 'react'
import { ArrowRight, CircleDollarSign, FilePlus2, FileText, PackageSearch, Printer, ShoppingBag, ShoppingCart, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Dashboard, PrintOrder, Printer as PrinterItem, QuoteSummary } from '../types'
import { Empty, ErrorMessage, Loading, Metric, PageHeader, Status, formatDate, money, number } from '../ui'

export function DashboardPage({ goTo }: { goTo: (page: string) => void }) {
  const [data, setData] = useState<Dashboard>()
  const [quotes, setQuotes] = useState<QuoteSummary[]>([])
  const [printers, setPrinters] = useState<PrinterItem[]>([])
  const [orders, setOrders] = useState<PrintOrder[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [quoteDialog, setQuoteDialog] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([api.dashboard(), api.quotes(), api.settings(), api.printers(), api.printOrders()])
      .then(([dashboard, recent, configuration, printerItems, orderItems]) => {
        setData(dashboard); setQuotes(recent.slice(0, 5)); setSettings(configuration); setPrinters(printerItems.filter(x => x.active)); setOrders(orderItems)
      })
      .catch(reason => setError((reason as Error).message))
  }, [])

  const printerRows = useMemo(() => printers.map(printer => ({
    printer,
    orders: orders
      .filter(order => order.printerId === printer.id && order.status !== 'Terminado')
      .sort((a, b) => new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime() || a.id - b.id)
  })), [printers, orders])

  function chooseQuote(target: 'quote' | 'storeQuote') {
    setQuoteDialog(false)
    goTo(target)
  }

  return <>
    <PageHeader eyebrow="RESUMEN OPERATIVO" title="Panel de control" description="Actividad actual de cotizaciones, ventas e inventario." actions={<button onClick={() => setQuoteDialog(true)}><FilePlus2 size={17} />Nueva cotización</button>} />
    <ErrorMessage error={error} />
    {!data ? <Loading /> : <section className="metrics">
      <Metric label="Cotizaciones" value={data.quotes} icon={<FileText size={20} />} />
      <Metric label="Ventas del mes" value={data.salesThisMonth} icon={<ShoppingBag size={20} />} />
      <Metric label="Ingresos del mes" value={money(data.revenueThisMonth, settings?.currencySymbol)} icon={<CircleDollarSign size={20} />} />
      <Metric label="Consumibles con stock bajo" value={data.lowStock} icon={<PackageSearch size={20} />} />
    </section>}

    <section className="panel top-gap">
      <div className="section-title"><div><p className="eyebrow">IMPRESORAS</p><h2>Estados y pedidos asignados</h2></div><button className="ghost" onClick={() => goTo('orders')}>Ver pedidos <ArrowRight size={16} /></button></div>
      {!data ? <Loading /> : printerRows.length === 0 ? <Empty>No hay impresoras activas registradas.</Empty> : <div className="printer-status-grid">{printerRows.map(({ printer, orders: assigned }) => <article className="printer-status-card" key={printer.id}>
        <div className="printer-status-head"><div><strong>{printer.name}</strong><small>{printer.buildX}x{printer.buildY}x{printer.buildZ} mm · {printer.colorCount} color(es)</small></div><span className={`machine-status ${printer.status.toLowerCase().replaceAll(' ', '-')}`}>{printer.status}</span></div>
        {assigned.length === 0 ? <p className="muted">Sin pedidos asignados.</p> : <div className="assigned-orders">{assigned.slice(0, 4).map((order, index) => <button key={order.id} onClick={() => goTo('orders')}>
          <span className="queue-number">{index + 1}</span><span><b>{order.orderCode}</b><small>{order.customer} · {order.productName || order.projectName || 'Servicio de impresión'}</small></span><em>{order.status}</em>
        </button>)}</div>}
        {assigned.length > 4 && <button className="ghost small" onClick={() => goTo('orders')}>Ver {assigned.length - 4} más</button>}
      </article>)}</div>}
    </section>

    <section className="panel top-gap">
      <div className="section-title"><div><p className="eyebrow">MOVIMIENTOS RECIENTES</p><h2>Últimas cotizaciones</h2></div><button className="ghost" onClick={() => goTo('history')}>Ver historial <ArrowRight size={16} /></button></div>
      {!data ? <Loading /> : quotes.length === 0 ? <Empty>Aún no existen cotizaciones.</Empty> : <div className="table-wrap"><table>
        <thead><tr><th>Código</th><th>Fecha</th><th>Cliente</th><th>Proyecto</th><th>Precio</th><th>Estado</th></tr></thead>
        <tbody>{quotes.map(item => <tr key={item.id} className="clickable" onClick={() => goTo(`history:${item.id}`)}>
          <td className="mono">{item.orderCode}</td><td>{formatDate(item.createdAtUtc)}</td><td>{item.customer}</td><td>{item.projectName}</td>
          <td>{money(item.recommendedPrice, settings?.currencySymbol)}</td><td><Status active={Boolean(item.soldAtUtc)} trueLabel="VENDIDA" falseLabel="PENDIENTE" /></td>
        </tr>)}</tbody>
      </table></div>}
    </section>

    {quoteDialog && <div className="confirm-overlay" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setQuoteDialog(false) }}>
      <section className="confirm-dialog quote-choice-dialog" role="dialog" aria-modal="true" aria-labelledby="quote-choice-title">
        <button className="icon ghost confirm-close" aria-label="Cerrar" onClick={() => setQuoteDialog(false)}><X size={18} /></button>
        <div className="confirm-icon"><FilePlus2 size={24} /></div>
        <p className="eyebrow">NUEVA COTIZACIÓN</p>
        <h2 id="quote-choice-title">¿Qué tipo de cotización quieres crear?</h2>
        <p>Elige el flujo que corresponde al trabajo que vas a registrar.</p>
        <div className="quote-choice-actions">
          <button type="button" onClick={() => chooseQuote('quote')}><FilePlus2 size={18} /><span><b>Cotización personalizada</b><small>Para piezas únicas con filamento, materiales y tiempos manuales.</small></span></button>
          <button type="button" onClick={() => chooseQuote('storeQuote')}><ShoppingCart size={18} /><span><b>Cotización de tienda</b><small>Para productos ya registrados y vendidos por cantidad.</small></span></button>
        </div>
      </section>
    </div>}
  </>
}
