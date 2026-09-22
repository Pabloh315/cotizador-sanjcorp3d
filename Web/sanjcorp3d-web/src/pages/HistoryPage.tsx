import { useCallback, useEffect, useState } from 'react'
import { Copy, Download, Eye, Pencil, Printer, Search, ShoppingBag, Trash2, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, QuoteDetail, QuoteSummary } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, Status, SuccessMessage, formatDate, money, number } from '../ui'
import { confirmDialog } from '../confirm'

export function HistoryPage({ initialId, canSale, isAdmin, logoUrl, businessName = 'Atlas Impresiones 3D' }: { initialId?: number; canSale: boolean; isAdmin: boolean; logoUrl?: string; businessName?: string }) {
  const [items, setItems] = useState<QuoteSummary[]>([])
  const [detail, setDetail] = useState<QuoteDetail>()
  const [settings, setSettings] = useState<BusinessSettings>()
  const [search, setSearch] = useState(''); const [from, setFrom] = useState(''); const [to, setTo] = useState('')
  const [error, setError] = useState(''); const [success, setSuccess] = useState(''); const [loading, setLoading] = useState(true); const [detailLoading, setDetailLoading] = useState(false)
  const [editedPrice, setEditedPrice] = useState<number>()

  const load = useCallback(() => { setLoading(true); setError(''); api.quotes({ search, from, to }).then(setItems).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false)) }, [search, from, to])
  useEffect(() => { api.settings().then(setSettings).catch(() => undefined) }, [])
  useEffect(() => { load() }, [load])
  useEffect(() => { if (initialId) open(initialId) }, [initialId])

  async function open(id: number) { setDetailLoading(true); setError(''); try { const value = await api.quote(id); setDetail(value); setEditedPrice(value.recommendedPrice) } catch (reason) { setError((reason as Error).message) } finally { setDetailLoading(false) } }
  async function updatePrice() { if (!detail || !editedPrice || editedPrice <= 0) return; try { await api.updateQuotePrice(detail.id, editedPrice); setSuccess('Precio final actualizado.'); await open(detail.id); load() } catch (reason) { setError((reason as Error).message) } }
  async function copyQuote(id: number) { try { const copy = await api.copyQuote(id); setSuccess(`Se creó la copia ${copy.orderCode} como nueva cotización.`); load(); await open(copy.id) } catch (reason) { setError((reason as Error).message) } }
  async function sale() { if (!detail) return; if (!await confirmDialog({ title: 'Confirmar venta', message: 'Se registrará la venta y se descontará el filamento del inventario.', highlight: `${detail.orderCode} · ${money(detail.recommendedPrice, settings?.currencySymbol)}`, confirmLabel: 'Sí, confirmar', variant: 'success' })) return; try { await api.confirmSale(detail.id); setSuccess('Venta confirmada y filamento descontado del inventario.'); window.dispatchEvent(new Event('inventory-changed')); await open(detail.id); load() } catch (reason) { setError((reason as Error).message) } }
  async function remove() { if (!detail) return; if (!await confirmDialog({ title: 'Eliminar cotización', message: detail.sale ? 'También se eliminará su venta y se devolverá el stock asociado.' : 'Esta acción eliminará definitivamente la cotización.', highlight: detail.orderCode, confirmLabel: 'Sí, eliminar', variant: 'danger' })) return; try { await api.deleteQuote(detail.id); setDetail(undefined); setSuccess('Cotización eliminada y stock restaurado cuando correspondía.'); window.dispatchEvent(new Event('inventory-changed')); load() } catch (reason) { setError((reason as Error).message) } }

  return <>
    <PageHeader eyebrow="REGISTRO" title="Historial de cotizaciones" description="Busca por código, cliente o proyecto; revisa el detalle y confirma ventas." actions={<button className="secondary" onClick={() => api.exportQuotes({ search, from, to }).catch(reason => setError((reason as Error).message))}><Download size={17} />Exportar Excel</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <section className="panel">
      <div className="filters"><label className="search-field"><Search size={17} /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Código, cliente o proyecto…" /></label><label>Desde<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Hasta<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label><button className="ghost" onClick={load}>Actualizar</button></div>
      {loading ? <Loading /> : items.length === 0 ? <Empty>No se encontraron cotizaciones.</Empty> : <div className="table-wrap"><table><thead><tr><th>Código</th><th>Fecha</th><th>Cliente / proyecto</th><th>Impresora</th><th>Peso</th><th>Costo</th><th>Precio</th><th>Estado</th><th></th></tr></thead><tbody>{items.map(item => <tr key={item.id} className="clickable" onClick={() => open(item.id)}><td className="mono">{item.orderCode}</td><td>{formatDate(item.createdAtUtc)}</td><td><strong>{item.customer}</strong><small className="cell-note">{item.productName || item.projectName}</small></td><td>{item.printerName}</td><td>{number(item.totalWeight)} g</td><td>{money(item.costTotal, settings?.currencySymbol)}</td><td><strong>{money(item.recommendedPrice, settings?.currencySymbol)}</strong></td><td><Status active={Boolean(item.soldAtUtc)} trueLabel="VENDIDA" falseLabel="PENDIENTE" /></td><td><button className="icon ghost" aria-label="Ver detalle"><Eye size={16} /></button><button className="icon ghost" aria-label="Copiar" onClick={event => { event.stopPropagation(); copyQuote(item.id) }}><Copy size={16} /></button></td></tr>)}</tbody></table></div>}
    </section>
    {(detail || detailLoading) && <div className="drawer-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) setDetail(undefined) }}><aside className="detail-drawer printable receipt">
      {detailLoading && !detail ? <Loading /> : detail && <>
        <div className="receipt-header"><div className="receipt-logo">{logoUrl ? <img src={logoUrl} alt={`Logo de ${businessName}`} /> : <span>{businessName.slice(0, 2).toUpperCase()}</span>}</div><div className="receipt-heading"><p className="receipt-business">{businessName}</p><h1>{detail.sale ? 'RECIBO DE PAGO' : 'COTIZACION'}</h1><div className="receipt-meta"><span>FECHA:<b>{formatDate(detail.sale?.soldAtUtc ?? detail.createdAtUtc)}</b></span><span>NUMERO:<b>{detail.orderCode}</b></span></div></div><button className="icon ghost no-print receipt-close" onClick={() => setDetail(undefined)}><X size={20} /></button></div>
        <div className="detail-status"><Status active={Boolean(detail.sale)} trueLabel="VENTA CONFIRMADA" falseLabel="COTIZACION PENDIENTE" /><span>{formatDate(detail.sale?.soldAtUtc ?? detail.createdAtUtc)}</span></div>
        <section className="receipt-specs"><div><span>Proyecto</span><b>{detail.projectName || 'Servicio de impresión'}</b></div><div><span>Producto</span><b>{detail.productName || 'Pieza personalizada'}</b></div><div><span>Impresora</span><b>{detail.printerName}</b></div><div><span>Tiempo estimado</span><b>{number(detail.printHours)} h</b></div><div><span>Cantidad</span><b>{detail.quantity} pieza(s)</b></div><div><span>Peso total</span><b>{number(detail.totalWeight)} g</b></div></section>        <section className="detail-section receipt-customer"><h3>Información del cliente</h3><dl className="detail-grid"><div><dt>Nombre</dt><dd>{detail.customer}</dd></div><div><dt>Teléfono</dt><dd>{detail.customerPhone || '—'}</dd></div></dl></section>
        <section className="detail-section"><h3>Descripción</h3><div className="receipt-description"><span>{detail.productName || detail.projectName || 'Servicio de impresión'} · {detail.quantity} pieza(s)</span><b>{money(detail.recommendedPrice, settings?.currencySymbol)}</b></div></section>
        <section className="detail-section receipt-internal"><h3>Consumibles</h3>{detail.consumables.map(line => <div className="detail-line" key={line.id}><span>{line.name} · {line.material} · {line.color}<small>{number(line.grams)} g por pieza</small></span><b>{money(line.lineCost, settings?.currencySymbol)}</b></div>)}</section>
        {detail.materials.length > 0 && <section className="detail-section receipt-internal"><h3>Materiales adicionales</h3>{detail.materials.map(line => <div className="detail-line" key={line.id}><span>{line.name}<small>{number(line.quantity)} × {money(line.unitPrice, settings?.currencySymbol)}</small></span><b>{money(line.lineCost, settings?.currencySymbol)}</b></div>)}</section>}
        <section className="detail-section"><h3>Desglose y cargos</h3><dl className="breakdown"><div><dt>Consumibles</dt><dd>{money(detail.materialCost, settings?.currencySymbol)}</dd></div><div><dt>Electricidad</dt><dd>{money(detail.electricityCost, settings?.currencySymbol)}</dd></div><div><dt>Mantenimiento</dt><dd>{money(detail.maintenanceCost, settings?.currencySymbol)}</dd></div><div><dt>Adicionales y extras</dt><dd>{money(detail.additionalCost, settings?.currencySymbol)}</dd></div><div className="subtotal"><dt>Sub-total</dt><dd>{money(detail.subtotal, settings?.currencySymbol)}</dd></div><div><dt>Ganancia</dt><dd>{money(detail.profitAmount, settings?.currencySymbol)}</dd></div><div><dt>Impuesto</dt><dd>{money(detail.taxAmount, settings?.currencySymbol)}</dd></div></dl><label className="price-edit no-print">Precio final editable<input type="number" min="0.01" step="0.01" value={editedPrice ?? detail.recommendedPrice} disabled={Boolean(detail.sale)} onChange={e => setEditedPrice(Number(e.target.value))} /><button disabled={Boolean(detail.sale)} onClick={updatePrice}><Pencil size={15} />Guardar precio</button></label><div className="detail-total"><span>Total</span><strong>{money(detail.recommendedPrice, settings?.currencySymbol)}</strong></div></section>
        {detail.notes && <section className="detail-section"><h3>Observaciones</h3><p className="notes">{detail.notes}</p></section>}<p className="receipt-thanks">Gracias por confiar en Atlas Impresiones 3D<small>Documento no válido como factura</small></p>
        <div className="drawer-actions no-print"><button className="ghost" onClick={() => window.print()}><Printer size={16} />Imprimir</button>{canSale && !detail.sale && <button className="sale-button" onClick={sale}><ShoppingBag size={16} />Confirmar venta</button>}{isAdmin && <button className="ghost danger-text" onClick={remove}><Trash2 size={16} />Eliminar</button>}</div>
      </>}
    </aside></div>}
  </>
}






