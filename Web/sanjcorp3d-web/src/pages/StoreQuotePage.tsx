import { useEffect, useMemo, useState } from 'react'
import { Calculator, CheckCircle2, ClipboardList, Download, RotateCcw, Save, ShoppingBag, Trash2 } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Printer, PrintOrder, ProductCatalog, QuoteSummary, StoreQuoteCalculation, StoreQuoteLine, StoreQuoteRequest } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number } from '../ui'
import { confirmDialog } from '../confirm'

type QuoteInfo = { customer: string; phone: string; description: string; address: string }
type StoreCart = { id: string; createdAt: string; info: QuoteInfo; lines: Array<StoreQuoteLine & { materialType: string; materialLabel?: string; consumableId?: number }> }
const cartKey = 'sanjcorp.store.carts'

export function StoreQuotePage({ canWrite }: { canWrite: boolean }) {
  const [products, setProducts] = useState<ProductCatalog[]>([])
  const [printers, setPrinters] = useState<Printer[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [carts, setCarts] = useState<StoreCart[]>([])
  const [selectedCartId, setSelectedCartId] = useState('')
  const [printerIds, setPrinterIds] = useState<number[]>([])
  const [calculation, setCalculation] = useState<StoreQuoteCalculation>()
  const [saved, setSaved] = useState<QuoteSummary>()
  const [sold, setSold] = useState(false)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const loadCarts = () => {
    const parsed = JSON.parse(sessionStorage.getItem(cartKey) ?? '[]') as StoreCart[]
    setCarts(parsed)
    setSelectedCartId(current => current || parsed[0]?.id || '')
  }

  useEffect(() => {
    Promise.all([api.products(), api.printers(), api.settings(), api.printOrders()]).then(([productItems, printerItems, config, orderItems]) => {
      const busyPrinterIds = new Set((orderItems as PrintOrder[]).filter(x => x.status !== 'Terminado').map(x => x.printerId))
      const availablePrinters = printerItems.filter(x => x.active && !busyPrinterIds.has(x.id))
      setProducts(productItems.filter(x => x.active)); setPrinters(availablePrinters); setSettings(config)
      setPrinterIds(availablePrinters[0] ? [availablePrinters[0].id] : [])
      loadCarts()
    }).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
    const listener = () => loadCarts()
    window.addEventListener('store-cart-created', listener)
    return () => window.removeEventListener('store-cart-created', listener)
  }, [])

  const productById = useMemo(() => new Map(products.map(x => [x.id, x])), [products])
  const selectedCart = useMemo(() => carts.find(cart => cart.id === selectedCartId) ?? carts[0], [carts, selectedCartId])
  const selectedProducts = useMemo(() => (selectedCart?.lines ?? []).map(line => ({ line, product: productById.get(line.productId) })).filter(x => x.product), [selectedCart, productById])
  const totals = useMemo(() => selectedProducts.reduce((acc, item) => ({
    quantity: acc.quantity + item.line.quantity,
    minutes: acc.minutes + (item.product?.productionMinutes ?? 0) * item.line.quantity,
    material: acc.material + (item.product?.materialCost ?? 0) * item.line.quantity,
    grams: acc.grams + (item.product?.filamentGrams ?? 0) * item.line.quantity
  }), { quantity: 0, minutes: 0, material: 0, grams: 0 }), [selectedProducts])

  function invalidate() { setCalculation(undefined); setSaved(undefined); setSold(false); setSuccess('') }
  function togglePrinter(id: number) {
    setPrinterIds(current => current.includes(id) ? current.filter(x => x !== id) : current.length >= 3 ? current : [...current, id])
    invalidate()
  }
  function notes(cart: StoreCart) { return `Direccion: ${cart.info.address}\nDescripcion: ${cart.info.description}` }
  function request(): StoreQuoteRequest {
    if (!selectedCart) throw new Error('No hay carrito seleccionado.')
    return { customer: selectedCart.info.customer, customerPhone: selectedCart.info.phone, projectName: selectedCart.info.description, printerIds, notes: notes(selectedCart), lines: selectedCart.lines }
  }
  async function calculate() { setBusy(true); setError(''); try { setCalculation(await api.calculateStoreQuote(request())) } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
  async function save() { setBusy(true); setError(''); try { const result = await api.createStoreQuote(request()); setSaved(result); setCalculation(await api.calculateStoreQuote(request())); setSuccess(`Cotizacion ${result.orderCode} guardada correctamente.`) } catch (reason) { setError((reason as Error).message) } finally { setBusy(false) } }
    async function confirmSale() {
    if (!saved) return
    const cartId = selectedCart?.id
    if (!await confirmDialog({ title: 'Confirmar venta', message: 'Se registrara la venta y el pedido pasara a produccion.', highlight: saved.orderCode, confirmLabel: 'Si, confirmar', variant: 'success' })) return
    setBusy(true); setError('')
    try {
      await api.confirmSale(saved.id)
      if (cartId) removeCart(cartId)
      setSaved(undefined); setCalculation(undefined); setSold(false)
      setSuccess('Venta confirmada. El carrito salio de Cotizaciones tienda y ahora esta en Pedidos.')
      window.dispatchEvent(new Event('inventory-changed'))
      window.dispatchEvent(new Event('print-orders-changed'))
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }
  function removeCart(id: string) { const next = carts.filter(cart => cart.id !== id); sessionStorage.setItem(cartKey, JSON.stringify(next)); setCarts(next); setSelectedCartId(next[0]?.id ?? ''); invalidate() }
  function clear() { setCalculation(undefined); setSaved(undefined); setSold(false); setError(''); setSuccess('') }

  if (loading) return <Loading label="Cargando cotizaciones de tienda..." />
  if (!canWrite) return <><PageHeader eyebrow="TIENDA" title="Cotizaciones de tienda" description="Tu rol solo puede consultar informacion." /><div className="panel locked-panel"><h2>Acceso de consulta</h2><p>Solicita rol de Ventas o Administrador para crear cotizaciones.</p></div></>

  return <>
    <PageHeader eyebrow="TIENDA" title="Cotizaciones de tienda" description="Revisa los carritos terminados desde Productos tienda, calcula el total y guarda la cotizacion." actions={<button onClick={() => { localStorage.setItem('atlas.route', 'storeProducts'); window.location.hash = 'storeProducts' }}>Volver a productos</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {carts.length === 0 && <div className="alert error">No hay carritos terminados. Entra a Productos tienda, presiona Cotizar y termina una cotizacion.</div>}
    {printers.length === 0 && <div className="alert error">No hay impresoras libres. Una impresora debe terminar y enfriarse antes de recibir otro pedido.</div>}
    <div className="quote-layout">
      <div className="form-stack">
        <section className="panel"><div className="section-title"><div><span className="step">01</span><h2>Carritos terminados</h2></div></div>{carts.length === 0 ? <Empty>No hay cotizaciones pendientes.</Empty> : <div className="line-list">{carts.map(cart => <div className={`line-item ${selectedCart?.id === cart.id ? 'selected' : ''}`} key={cart.id}><button type="button" className="maker-card-select" onClick={() => { setSelectedCartId(cart.id); invalidate() }}><strong>{cart.info.customer}</strong><small>{cart.info.phone} - {cart.info.description} - {cart.lines.length} productos</small></button><button className="icon danger" type="button" onClick={() => removeCart(cart.id)}><Trash2 size={16} /></button></div>)}</div>}</section>
        {selectedCart && <section className="panel"><div className="section-title"><div><span className="step">02</span><h2>Detalle del carrito</h2></div></div><div className="breakdown"><div><dt>Cliente</dt><dd>{selectedCart.info.customer}</dd></div><div><dt>Telefono</dt><dd>{selectedCart.info.phone}</dd></div><div><dt>Descripcion</dt><dd>{selectedCart.info.description}</dd></div><div><dt>Direccion</dt><dd>{selectedCart.info.address}</dd></div></div><div className="line-list">{selectedCart.lines.map((line, index) => { const product = productById.get(line.productId); return <div className="line-item" key={`${line.productId}-${line.consumableId ?? line.materialType}-${index}`}><div><strong>{product?.name}</strong><small>{line.materialLabel || line.materialType} - {line.quantity} unid.</small></div><b>{number((product?.filamentGrams ?? 0) * line.quantity)} g</b></div> })}</div></section>}
        <section className="panel"><div className="section-title"><div><span className="step">03</span><h2>Impresoras libres</h2></div><span className="muted">Maximo 3</span></div><div className="printer-choice-grid">{printers.map(printer => { const selected = printerIds.includes(printer.id); return <button type="button" key={printer.id} aria-pressed={selected} className={`printer-choice ${selected ? 'selected' : ''}`} onClick={() => togglePrinter(printer.id)}><span><strong>{printer.name}</strong><small>{selected ? 'Seleccionada para este pedido' : 'Libre para asignar'}</small></span><b>{selected ? 'OK' : '+'}</b></button> })}</div>{printerIds.length > 0 && <p className="selection-note">Seleccionadas: {printers.filter(printer => printerIds.includes(printer.id)).map(printer => printer.name).join(', ')}</p>}</section>
      </div>
      <aside className="quote-summary panel"><p className="eyebrow">RESULTADO</p><h2>Resumen de tienda</h2><div className="breakdown"><div><dt>Cantidad total</dt><dd>{totals.quantity}</dd></div><div><dt>Tiempo automatico</dt><dd>{number(totals.minutes)} min</dd></div><div><dt>Filamento requerido</dt><dd>{number(totals.grams)} g</dd></div><div><dt>Costo base registrado</dt><dd>{money(totals.material, settings?.currencySymbol)}</dd></div></div>{!calculation ? <div className="summary-placeholder"><Calculator size={34} /><p>Calcula para ver el precio final.</p></div> : <><div className="price-hero"><small>Total sugerido</small><strong>{money(calculation.recommendedPrice, settings?.currencySymbol)}</strong><span>{number(calculation.printHours)} horas de produccion</span></div><dl className="breakdown"><div><dt>Material</dt><dd>{money(calculation.materialCost, settings?.currencySymbol)}</dd></div><div><dt>Electricidad</dt><dd>{money(calculation.electricityCost, settings?.currencySymbol)}</dd></div><div><dt>Mantenimiento</dt><dd>{money(calculation.maintenanceCost, settings?.currencySymbol)}</dd></div><div><dt>Preparacion</dt><dd>{money(calculation.preparationCost, settings?.currencySymbol)}</dd></div><div><dt>Salarios</dt><dd>{money(calculation.laborCost, settings?.currencySymbol)}</dd></div><div><dt>Merma/fallos</dt><dd>{money(calculation.wasteCost, settings?.currencySymbol)}</dd></div><div><dt>Administracion</dt><dd>{money(calculation.overheadCost, settings?.currencySymbol)}</dd></div><div><dt>Empaque</dt><dd>{money(calculation.packagingCost, settings?.currencySymbol)}</dd></div><div><dt>Transporte</dt><dd>{money(calculation.transportCost, settings?.currencySymbol)}</dd></div><div className="subtotal"><dt>Costo total</dt><dd>{money(calculation.subtotal, settings?.currencySymbol)}</dd></div><div><dt>Ganancia</dt><dd>{money(calculation.profitAmount, settings?.currencySymbol)}</dd></div><div><dt>Impuesto</dt><dd>{money(calculation.taxAmount, settings?.currencySymbol)}</dd></div></dl></>}{saved && <div className="saved-ticket"><CheckCircle2 size={19} /><div><small>Codigo guardado</small><strong>{saved.orderCode}</strong></div><span className={`status ${sold ? 'ok' : 'warning'}`}>{sold ? 'VENDIDA' : 'PENDIENTE'}</span><button className="icon ghost" onClick={() => api.downloadVoucher(saved.id)}><Download size={16} /></button></div>}{sold && <button type="button" className="secondary view-orders-button" onClick={() => { localStorage.setItem('atlas.route', 'orders'); window.location.hash = 'orders' }}><ClipboardList size={17} />Ver en pedidos</button>}<div className="summary-actions"><button className="secondary" disabled={busy || !selectedCart || printerIds.length === 0} onClick={calculate}><Calculator size={17} />Calcular precio</button><button disabled={busy || !selectedCart || printerIds.length === 0} onClick={save}><Save size={17} />Guardar cotizacion</button>{saved && !sold && <button className="sale-button" disabled={busy} onClick={confirmSale}><ShoppingBag size={17} />Confirmar venta</button>}<button className="ghost" disabled={busy} onClick={clear}><RotateCcw size={16} />Limpiar</button></div></aside>
    </div>
  </>
}
