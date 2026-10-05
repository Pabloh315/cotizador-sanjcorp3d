import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Archive, Edit3, PackagePlus, Plus, Send, ShoppingCart, Trash2, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Consumable, ConsumableMaterialType, ProductCatalog, StoreQuoteLine } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

const blank: ProductCatalog = { id: 0, name: '', description: '', materialType: '', filamentGrams: 0, materialCost: 0, productionMinutes: 0, profitMultiplier: 1.4, active: true }
const cartKey = 'sanjcorp.store.carts'
type QuoteInfo = { customer: string; phone: string; description: string; address: string }
type CartLine = StoreQuoteLine & { materialType: string }
type StoreCart = { id: string; createdAt: string; info: QuoteInfo; lines: CartLine[] }

export function StoreProductsPage({ canManage }: { canManage: boolean }) {
  const [items, setItems] = useState<ProductCatalog[]>([])
  const [materialTypes, setMaterialTypes] = useState<ConsumableMaterialType[]>([])
  const [consumables, setConsumables] = useState<Consumable[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [draft, setDraft] = useState<ProductCatalog | null>(null)
  const [quoteOpen, setQuoteOpen] = useState(false)
  const [quoteInfo, setQuoteInfo] = useState<QuoteInfo>({ customer: '', phone: '', description: '', address: '' })
  const [cartLines, setCartLines] = useState<CartLine[]>([])
  const [lineProduct, setLineProduct] = useState<ProductCatalog | null>(null)
  const [lineQuantity, setLineQuantity] = useState(1)
  const [lineMaterial, setLineMaterial] = useState('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const priceByMaterial = useMemo(() => {
    const groups = new Map<string, number[]>()
    consumables.filter(item => item.active).forEach(item => groups.set(item.material, [...(groups.get(item.material) ?? []), item.pricePerUnit]))
    return new Map([...groups.entries()].map(([name, values]) => [name, values.reduce((sum, value) => sum + value, 0) / values.length]))
  }, [consumables])
  const materialOptions = useMemo(() => [...new Set([...materialTypes.filter(x => x.active).map(x => x.name), ...consumables.map(x => x.material).filter(Boolean)])].sort((a, b) => a.localeCompare(b)), [materialTypes, consumables])
  const productById = useMemo(() => new Map(items.map(item => [item.id, item])), [items])

  function materialUnitCost(item: ProductCatalog, materialType = item.materialType) {
    const materialPrice = priceByMaterial.get(materialType) ?? 0
    const calculatedMaterial = item.filamentGrams > 0 && materialPrice > 0 ? item.filamentGrams / 1000 * materialPrice : item.materialCost
    return calculatedMaterial
  }
  function unitProductionCost(item: ProductCatalog, materialType = item.materialType) {
    const material = materialUnitCost(item, materialType)
    if (!settings) return material
    return material
      + settings.maintenancePerPrint
      + material * settings.maintenancePercent / 100
      + material * settings.preparationPercent / 100
      + material * settings.laborPercent / 100
      + material * settings.wastePercent / 100
      + material * settings.overheadPercent / 100
      + settings.packagingCost
      + settings.transportCost
  }
  function finalUnitPrice(item: ProductCatalog, materialType = item.materialType) { return unitProductionCost(item, materialType) * Math.max(1, item.profitMultiplier) }

  const cartEstimate = useMemo(() => cartLines.reduce((sum, line) => {
    const product = productById.get(line.productId)
    return sum + (product ? finalUnitPrice(product, line.materialType) * line.quantity : 0)
  }, 0), [cartLines, productById, settings, priceByMaterial])

  async function load() {
    setLoading(true); setError('')
    try {
      const [productItems, typeItems, consumableItems, config] = await Promise.all([api.products(true), api.materialTypes(true), api.consumables(), api.settings()])
      setItems(productItems); setMaterialTypes(typeItems); setConsumables(consumableItems); setSettings(config)
    }
    catch (reason) { setError((reason as Error).message) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])

  function edit(item?: ProductCatalog) {
    const next = item ? { ...item } : { ...blank, materialType: materialOptions[0] ?? '', profitMultiplier: settings?.defaultProfitMultiplier ?? 1.4 }
    setDraft(next); setError('')
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!draft) return
    if (!draft.name.trim()) { setError('El nombre del producto es obligatorio.'); return }
    if (!draft.materialType.trim()) { setError('Selecciona el tipo de material base.'); return }
    if (draft.filamentGrams <= 0) { setError('Indica los gramos de filamento por unidad.'); return }
    if (draft.productionMinutes <= 0) { setError('Indica el tiempo de produccion por unidad.'); return }
    if (draft.profitMultiplier < 1) { setError('El multiplicador debe ser igual o mayor que 1.'); return }
    setBusy(true); setError(''); setSuccess('')
    try { await api.saveStoreProduct({ ...draft, materialCost: materialUnitCost(draft) }); setSuccess('Producto de tienda guardado.'); setDraft(null); await load() }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function archive(item: ProductCatalog) {
    if (!await confirmDialog({ title: 'Archivar producto', message: 'El producto dejara de estar disponible para nuevas cotizaciones.', highlight: item.name, confirmLabel: 'Si, archivar', variant: 'danger' })) return
    setBusy(true); setError('')
    try { await api.archiveProduct(item.id); setSuccess('Producto archivado.'); await load() }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  function startQuote() {
    setQuoteOpen(true); setSuccess(''); setError('')
  }

  function openLine(product: ProductCatalog) {
    if (!quoteOpen) { setError('Primero presiona Cotizar y registra los datos del cliente.'); return }
    setLineProduct(product); setLineQuantity(1); setLineMaterial(product.materialType || materialOptions[0] || '')
  }

  function addLine(event: FormEvent) {
    event.preventDefault()
    if (!lineProduct) return
    if (lineQuantity <= 0) { setError('La cantidad debe ser mayor que cero.'); return }
    if (!lineMaterial) { setError('Selecciona el material solicitado.'); return }
    setCartLines(current => {
      const index = current.findIndex(line => line.productId === lineProduct.id && line.materialType === lineMaterial)
      if (index < 0) return [...current, { productId: lineProduct.id, quantity: lineQuantity, materialType: lineMaterial }]
      return current.map((line, i) => i === index ? { ...line, quantity: line.quantity + lineQuantity } : line)
    })
    setLineProduct(null); setLineQuantity(1); setError('')
  }

  function finishQuote() {
    if (!quoteInfo.customer.trim()) { setError('Escribe el nombre del cliente.'); return }
    if (!quoteInfo.phone.trim()) { setError('Escribe el telefono del cliente.'); return }
    if (!quoteInfo.description.trim()) { setError('Escribe una descripcion de la cotizacion.'); return }
    if (!quoteInfo.address.trim()) { setError('Escribe la direccion de entrega.'); return }
    if (cartLines.length === 0) { setError('Agrega al menos un producto al carrito.'); return }
    const cart: StoreCart = { id: `${Date.now()}`, createdAt: new Date().toISOString(), info: quoteInfo, lines: cartLines }
    const current = JSON.parse(sessionStorage.getItem(cartKey) ?? '[]') as StoreCart[]
    sessionStorage.setItem(cartKey, JSON.stringify([cart, ...current].slice(0, 20)))
    window.dispatchEvent(new Event('store-cart-created'))
    window.location.hash = 'storeQuote'
  }

  return <>
    <PageHeader eyebrow="TIENDA" title="Productos de tienda" description="Administra productos y arma cotizaciones tipo carrito desde el catalogo." actions={<div className="summary-actions"><button className="secondary" onClick={startQuote}><ShoppingCart size={17} />Cotizar</button>{quoteOpen && <button className="danger-button" onClick={finishQuote}><Send size={17} />Terminar cotizacion</button>}{canManage && <button onClick={() => edit()}><Plus size={17} />Nuevo producto</button>}</div>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {quoteOpen && <section className="panel quote-search-panel"><div className="section-title"><div><p className="eyebrow">CARRITO DE TIENDA</p><h2>Datos de la cotizacion</h2></div><button className="icon ghost" onClick={() => setQuoteOpen(false)}><X size={18} /></button></div><div className="form-grid two"><label>Nombre<input value={quoteInfo.customer} onChange={e => setQuoteInfo({ ...quoteInfo, customer: e.target.value })} /></label><label>Telefono<input inputMode="tel" value={quoteInfo.phone} onChange={e => setQuoteInfo({ ...quoteInfo, phone: e.target.value })} /></label><label>Descripcion<input value={quoteInfo.description} onChange={e => setQuoteInfo({ ...quoteInfo, description: e.target.value })} /></label><label>Direccion<input value={quoteInfo.address} onChange={e => setQuoteInfo({ ...quoteInfo, address: e.target.value })} /></label></div>{cartLines.length === 0 ? <Empty>Selecciona productos del catalogo para agregarlos al carrito.</Empty> : <div className="line-list">{cartLines.map((line, index) => { const product = productById.get(line.productId); return <div className="line-item" key={`${line.productId}-${line.materialType}`}><div><strong>{product?.name}</strong><small>{line.materialType} - {line.quantity} unid.</small></div><b>{money(product ? finalUnitPrice(product, line.materialType) * line.quantity : 0, settings?.currencySymbol)}</b><button className="icon danger" onClick={() => setCartLines(current => current.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div> })}</div>}<div className="formula-note"><strong>Estimado del carrito: {money(cartEstimate, settings?.currencySymbol)}</strong><span>El precio final se recalcula al terminar la cotizacion y asignar impresoras libres.</span></div></section>}
    <div className={`catalog-layout ${draft ? 'with-editor' : ''}`}>
      <section className="panel">
        {loading ? <Loading /> : items.length === 0 ? <Empty>No hay productos de tienda registrados.</Empty> : <div className="card-grid">{items.map(item => <article className={`catalog-card ${item.active ? '' : 'archived'}`} key={item.id}>
          <div className="catalog-icon"><PackagePlus size={20} /></div>
          <div className="catalog-main"><div><h3>{item.name}</h3>{!item.active && <span className="status warning">ARCHIVADO</span>}</div><p>{item.description || 'Sin descripcion'}</p><small>{item.materialType || 'Sin material'} - {weight(item.filamentGrams)} por unidad - {number(item.productionMinutes)} min - x{number(item.profitMultiplier)}</small><small>Costo produccion {money(unitProductionCost(item), settings?.currencySymbol)} - Precio sugerido {money(finalUnitPrice(item), settings?.currencySymbol)}</small></div>
          <div className="card-actions">{quoteOpen && item.active && <button className="small" onClick={() => openLine(item)}><ShoppingCart size={15} />Agregar</button>}{canManage && <button className="ghost small" onClick={() => edit(item)}><Edit3 size={15} />Editar</button>}{canManage && item.active && <button className="ghost small danger-text" disabled={busy} onClick={() => archive(item)}><Archive size={15} />Archivar</button>}</div>
        </article>)}</div>}
      </section>
      {draft && <form className="panel editor" onSubmit={save}>
        <div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR PRODUCTO' : 'NUEVO PRODUCTO'}</p><h2>{draft.id ? draft.name : 'Producto de tienda'}</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}><X size={18} /></button></div>
        <label>Nombre<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Descripcion<textarea rows={3} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
        <div className="form-grid two">
          <label>Material base<select required value={draft.materialType} onChange={e => setDraft({ ...draft, materialType: e.target.value })}><option value="">Seleccionar material</option>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label>
          <label>Filamento por unidad (g)<input type="number" min="0.01" step="0.01" value={draft.filamentGrams} onChange={e => setDraft({ ...draft, filamentGrams: Number(e.target.value) })} /></label>
          <label>Tiempo por unidad (min)<input type="number" min="0.01" step="0.01" value={draft.productionMinutes} onChange={e => setDraft({ ...draft, productionMinutes: Number(e.target.value) })} /></label>
          <label>Multiplicador de ganancia<input type="number" min="1" step="0.01" value={draft.profitMultiplier} onChange={e => setDraft({ ...draft, profitMultiplier: Number(e.target.value) })} /></label>
        </div>
        <div className="breakdown"><div><dt>Costo de produccion estimado</dt><dd>{money(unitProductionCost(draft), settings?.currencySymbol)}</dd></div><div><dt>Precio sugerido por unidad</dt><dd>{money(finalUnitPrice(draft), settings?.currencySymbol)}</dd></div></div>
        <p className="field-help">El material usa el precio promedio por kg del tipo seleccionado; el costo de produccion agrega mantenimiento, preparacion, salarios, merma, administracion, empaque y transporte configurados.</p>
        <div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button disabled={busy}>{busy ? 'Guardando...' : 'Guardar producto'}</button></div>
      </form>}
    </div>
    {lineProduct && <div className="drawer-backdrop"><form className="detail-drawer" onSubmit={addLine}><div className="section-title"><div><p className="eyebrow">AGREGAR AL CARRITO</p><h2>{lineProduct.name}</h2></div><button type="button" className="icon ghost" onClick={() => setLineProduct(null)}><X size={18} /></button></div><div className="form-grid two"><label>Cantidad<input type="number" min="1" step="1" value={lineQuantity} onChange={e => setLineQuantity(Number(e.target.value))} /></label><label>Material solicitado<select value={lineMaterial} onChange={e => setLineMaterial(e.target.value)}>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label></div><div className="breakdown"><div><dt>Filamento</dt><dd>{weight(lineProduct.filamentGrams * lineQuantity)}</dd></div><div><dt>Precio estimado</dt><dd>{money(finalUnitPrice(lineProduct, lineMaterial) * lineQuantity, settings?.currencySymbol)}</dd></div></div><div className="editor-actions"><button type="button" className="ghost" onClick={() => setLineProduct(null)}>Cancelar</button><button><Plus size={17} />Agregar</button></div></form></div>}
  </>
}
