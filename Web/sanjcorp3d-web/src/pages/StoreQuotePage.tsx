import { useEffect, useMemo, useState } from 'react'
import { Calculator, CheckCircle2, ClipboardList, Download, Plus, RotateCcw, Save, ShoppingBag, Trash2 } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Printer, PrintOrder, ProductCatalog, QuoteSummary, StoreQuoteCalculation, StoreQuoteLine, StoreQuoteRequest } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number } from '../ui'
import { confirmDialog } from '../confirm'

type Form = { customer: string; customerPhone: string; projectName: string; notes: string }
const emptyForm: Form = { customer: '', customerPhone: '', projectName: '', notes: '' }

export function StoreQuotePage({ canWrite }: { canWrite: boolean }) {
  const [products, setProducts] = useState<ProductCatalog[]>([])
  const [printers, setPrinters] = useState<Printer[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [form, setForm] = useState<Form>(emptyForm)
  const [printerIds, setPrinterIds] = useState<number[]>([])
  const [lines, setLines] = useState<StoreQuoteLine[]>([])
  const [selectedProduct, setSelectedProduct] = useState(0)
  const [quantity, setQuantity] = useState(1)
  const [calculation, setCalculation] = useState<StoreQuoteCalculation>()
  const [saved, setSaved] = useState<QuoteSummary>()
  const [sold, setSold] = useState(false)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    Promise.all([api.products(), api.printers(), api.settings(), api.printOrders()]).then(([productItems, printerItems, config, orderItems]) => {
      const configured = productItems.filter(x => x.active)
      const busyPrinterIds = new Set((orderItems as PrintOrder[]).filter(x => x.status !== 'Terminado').map(x => x.printerId))
      const availablePrinters = printerItems.filter(x => x.active && !busyPrinterIds.has(x.id))
      setProducts(configured); setPrinters(availablePrinters); setSettings(config)
      setSelectedProduct(configured[0]?.id ?? 0); setPrinterIds(availablePrinters[0] ? [availablePrinters[0].id] : [])
    }).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
  }, [])

  const productById = useMemo(() => new Map(products.map(x => [x.id, x])), [products])
  const selectedProducts = useMemo(() => lines.map(line => ({ line, product: productById.get(line.productId) })).filter(x => x.product), [lines, productById])
  const totals = useMemo(() => selectedProducts.reduce((acc, item) => ({
    quantity: acc.quantity + item.line.quantity,
    minutes: acc.minutes + (item.product?.productionMinutes ?? 0) * item.line.quantity,
    material: acc.material + (item.product?.materialCost ?? 0) * item.line.quantity,
    grams: acc.grams + (item.product?.filamentGrams ?? 0) * item.line.quantity
  }), { quantity: 0, minutes: 0, material: 0, grams: 0 }), [selectedProducts])

  function invalidate() { setCalculation(undefined); setSaved(undefined); setSold(false); setSuccess('') }
  function update<K extends keyof Form>(key: K, value: Form[K]) { setForm(current => ({ ...current, [key]: value })); invalidate() }
  function togglePrinter(id: number) {
    setPrinterIds(current => current.includes(id) ? current.filter(x => x !== id) : current.length >= 3 ? current : [...current, id])
    invalidate()
  }
  function addLine() {
    const product = productById.get(selectedProduct)
    if (!product || quantity <= 0) { setError('Selecciona un producto e indica una cantidad mayor que cero.'); return }
    if (product.productionMinutes <= 0) { setError(`Configura el tiempo de producción de ${product.name} antes de cotizarlo.`); return }
    if (product.filamentGrams <= 0) { setError(`Configura los gramos de filamento de ${product.name} antes de cotizarlo.`); return }
    if (!product.materialType) { setError(`Configura el tipo de material de ${product.name} antes de cotizarlo.`); return }
    setLines(current => {
      const index = current.findIndex(x => x.productId === product.id)
      if (index < 0) return [...current, { productId: product.id, quantity }]
      return current.map((line, i) => i === index ? { ...line, quantity: line.quantity + quantity } : line)
    })
    setQuantity(1); setError(''); invalidate()
  }
  function request(): StoreQuoteRequest { return { customer: form.customer, customerPhone: form.customerPhone, projectName: form.projectName, printerIds, notes: form.notes, lines } }
  async function calculate() { setBusy(true); setError(''); try { setCalculation(await api.calculateStoreQuote(request())) } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  async function save() { if (!form.customerPhone.trim()) { setError('Escribe el celular del cliente antes de guardar.'); return } setBusy(true); setError(''); try { const result = await api.createStoreQuote(request()); setSaved(result); setCalculation(await api.calculateStoreQuote(request())); setSuccess(`Cotización ${result.orderCode} guardada correctamente.`) } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  async function confirmSale() { if (!saved) return; if (!await confirmDialog({ title: 'Confirmar venta', message: 'Se registrará la venta y el pedido pasará a producción.', highlight: saved.orderCode, confirmLabel: 'Sí, confirmar', variant: 'success' })) return; setBusy(true); setError(''); try { await api.confirmSale(saved.id); setSold(true); setSuccess('Venta confirmada y pedido enviado a la cola de la impresora.'); window.dispatchEvent(new Event('inventory-changed')); window.dispatchEvent(new Event('print-orders-changed')) } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  function clear() { setForm({ ...emptyForm }); setLines([]); setCalculation(undefined); setSaved(undefined); setSold(false); setError(''); setSuccess('') }

  if (loading) return <Loading label="Cargando productos..." />
  if (!canWrite) return <><PageHeader eyebrow="TIENDA" title="Cotizador de productos de tienda" description="Tu rol solo puede consultar información." /><div className="panel locked-panel"><h2>Acceso de consulta</h2><p>Solicita rol de Ventas o Administrador para crear cotizaciones.</p></div></>

  return <>
    <PageHeader eyebrow="TIENDA" title="Cotizador de productos de tienda" description="Cotiza productos predefinidos por cantidad; el material, filamento, tiempo y ganancia vienen del producto registrado." />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {products.length === 0 && <div className="alert error">Agrega productos de tienda antes de cotizar.</div>}
    {printers.length === 0 && <div className="alert error">No hay impresoras libres. Una impresora debe terminar y enfriarse antes de recibir otro pedido.</div>}
    <div className="quote-layout">
      <div className="form-stack">
        <section className="panel"><div className="section-title"><div><span className="step">01</span><h2>Cliente y trabajo</h2></div></div><div className="form-grid two">
          <label>Cliente<input value={form.customer} onChange={e => update('customer', e.target.value)} /></label>
          <label>Celular<input value={form.customerPhone} onChange={e => update('customerPhone', e.target.value)} inputMode="tel" /></label>
          <label>Proyecto o referencia<input value={form.projectName} onChange={e => update('projectName', e.target.value)} placeholder="Pedido de tienda" /></label>
        </div></section>
        <section className="panel"><div className="section-title"><div><span className="step">02</span><h2>Productos</h2></div></div><div className="inline-form">
          <label>Producto<select value={selectedProduct} onChange={e => setSelectedProduct(Number(e.target.value))}>{products.map(item => <option key={item.id} value={item.id}>{item.name} · {item.materialType || 'sin material'} · {number(item.filamentGrams)} g · x{number(item.profitMultiplier)}</option>)}</select></label>
          <label>Cantidad<input type="number" min="1" step="1" value={quantity} onChange={e => setQuantity(Number(e.target.value))} /></label>
          <button type="button" onClick={addLine}><Plus size={17} />Agregar</button>
        </div>{lines.length === 0 ? <Empty>Agrega productos para cotizar.</Empty> : <div className="line-list">{lines.map((line, index) => { const product = productById.get(line.productId); return <div className="line-item" key={line.productId}><div><strong>{product?.name}</strong><small>{product?.description || 'Producto de tienda'} · {product?.materialType || 'sin material'}</small></div><b>{line.quantity} unid. · {number((product?.filamentGrams ?? 0) * line.quantity)} g · {number((product?.productionMinutes ?? 0) * line.quantity)} min · x{number(product?.profitMultiplier ?? 1)}</b><button className="icon danger" onClick={() => { setLines(current => current.filter((_, i) => i !== index)); invalidate() }}><Trash2 size={16} /></button></div> })}</div>}</section>
        <section className="panel"><div className="section-title"><div><span className="step">03</span><h2>Impresoras libres</h2></div><span className="muted">Máximo 3</span></div><div className="printer-choice-grid">{printers.map(printer => { const selected = printerIds.includes(printer.id); return <button type="button" key={printer.id} aria-pressed={selected} className={`printer-choice ${selected ? 'selected' : ''}`} onClick={() => togglePrinter(printer.id)}><span><strong>{printer.name}</strong><small>{selected ? 'Seleccionada para este pedido' : 'Libre para asignar'}</small></span><b>{selected ? '✓' : '+'}</b></button> })}</div>{printerIds.length > 0 && <p className="selection-note">Seleccionadas: {printers.filter(printer => printerIds.includes(printer.id)).map(printer => printer.name).join(', ')}</p>}<label>Notas<textarea rows={3} value={form.notes} onChange={e => update('notes', e.target.value)} placeholder="Entrega, acabado, observaciones..." /></label></section>
      </div>
      <aside className="quote-summary panel"><p className="eyebrow">RESULTADO</p><h2>Resumen de tienda</h2><div className="breakdown"><div><dt>Cantidad total</dt><dd>{totals.quantity}</dd></div><div><dt>Tiempo automático</dt><dd>{number(totals.minutes)} min</dd></div><div><dt>Filamento requerido</dt><dd>{number(totals.grams)} g</dd></div><div><dt>Costo base registrado</dt><dd>{money(totals.material, settings?.currencySymbol)}</dd></div></div>{!calculation ? <div className="summary-placeholder"><Calculator size={34} /><p>Calcula para ver el precio final.</p></div> : <><div className="price-hero"><small>Total sugerido</small><strong>{money(calculation.recommendedPrice, settings?.currencySymbol)}</strong><span>{number(calculation.printHours)} horas de producción</span></div><dl className="breakdown"><div><dt>Material</dt><dd>{money(calculation.materialCost, settings?.currencySymbol)}</dd></div><div><dt>Electricidad</dt><dd>{money(calculation.electricityCost, settings?.currencySymbol)}</dd></div><div><dt>Mantenimiento</dt><dd>{money(calculation.maintenanceCost, settings?.currencySymbol)}</dd></div><div className="subtotal"><dt>Costo total</dt><dd>{money(calculation.subtotal, settings?.currencySymbol)}</dd></div><div><dt>Ganancia</dt><dd>{money(calculation.profitAmount, settings?.currencySymbol)}</dd></div><div><dt>Impuesto</dt><dd>{money(calculation.taxAmount, settings?.currencySymbol)}</dd></div></dl></>}{saved && <div className="saved-ticket"><CheckCircle2 size={19} /><div><small>Código guardado</small><strong>{saved.orderCode}</strong></div><span className={`status ${sold ? 'ok' : 'warning'}`}>{sold ? 'VENDIDA' : 'PENDIENTE'}</span><button className="icon ghost" onClick={() => api.downloadVoucher(saved.id)}><Download size={16} /></button></div>}{sold && <button type="button" className="secondary view-orders-button" onClick={() => { window.location.hash = 'orders' }}><ClipboardList size={17} />Ver en pedidos</button>}<div className="summary-actions"><button className="secondary" disabled={busy || lines.length === 0 || printerIds.length === 0} onClick={calculate}><Calculator size={17} />Calcular precio</button><button disabled={busy || lines.length === 0 || printerIds.length === 0} onClick={save}><Save size={17} />Guardar cotización</button>{saved && !sold && <button className="sale-button" disabled={busy} onClick={confirmSale}><ShoppingBag size={17} />Confirmar venta</button>}<button className="ghost" disabled={busy} onClick={clear}><RotateCcw size={16} />Limpiar</button></div></aside>
    </div>
  </>
}




