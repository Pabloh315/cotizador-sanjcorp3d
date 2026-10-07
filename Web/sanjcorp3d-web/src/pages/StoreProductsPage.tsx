import { FormEvent, useEffect, useMemo, useState, type MouseEvent } from 'react'
import { Archive, ArrowLeft, ArrowRight, Edit3, MapPin, PackagePlus, Plus, Search, Send, ShoppingCart, Trash2, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Consumable, ConsumableMaterialType, ProductCatalog, StoreQuoteLine } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

const minProfitMargin = 25
const deliveryCostPerKm = 3.20
const mapZoom = 14
const tileSize = 256
const defaultMapCenter = { lat: -17.7833, lng: -63.1821 }
const cartKey = 'sanjcorp.store.carts'
const quoteSteps = [
  { title: 'Cliente y entrega', description: 'Datos, direccion, pin y recorrido.' },
  { title: 'Productos', description: 'Selecciona productos y materiales del inventario.' },
  { title: 'Resumen', description: 'Revisa el carrito antes de terminar.' },
] as const
const blank: ProductCatalog = { id: 0, name: '', description: '', materialType: '', filamentGrams: 0, materialCost: 0, productionMinutes: 0, maintenancePercent: 6, preparationPercent: 10, laborPercent: 20, wastePercent: 7, overheadPercent: 5, packagingCost: 0, profitMultiplier: 1.4, active: true }

type QuoteInfo = { customer: string; phone: string; description: string; address: string; latitude: number; longitude: number; transportCost: number; routeKm: number; routeMinutes: number }
type CartLine = StoreQuoteLine & { materialType: string; materialLabel: string; consumableId: number }
type StoreCart = { id: string; createdAt: string; info: QuoteInfo; lines: CartLine[] }
type AddressResult = { display_name: string; lat: string; lon: string }
type OriginPoint = { latitude: number; longitude: number }

const marginToMultiplier = (margin: number) => Number((1 + Math.max(minProfitMargin, Number.isFinite(margin) ? margin : minProfitMargin) / 100).toFixed(4))
const multiplierToMargin = (multiplier: number) => Number(Math.max(minProfitMargin, ((Number.isFinite(multiplier) ? multiplier : 1.25) - 1) * 100).toFixed(2))
const clampPercent = (value: number) => Math.min(100, Math.max(0, Number.isFinite(value) ? Number(value.toFixed(2)) : 0))
const defaultQuoteInfo = (): QuoteInfo => ({ customer: '', phone: '', description: '', address: '', latitude: defaultMapCenter.lat, longitude: defaultMapCenter.lng, transportCost: 0, routeKm: 0, routeMinutes: 0 })

function latLngToPoint(lat: number, lng: number, zoom = mapZoom) {
  const scale = tileSize * 2 ** zoom
  const sinLat = Math.sin(lat * Math.PI / 180)
  return { x: (lng + 180) / 360 * scale, y: (0.5 - Math.log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * scale }
}
function pointToLatLng(x: number, y: number, zoom = mapZoom) {
  const scale = tileSize * 2 ** zoom
  const lng = x / scale * 360 - 180
  const n = Math.PI - 2 * Math.PI * y / scale
  const lat = 180 / Math.PI * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)))
  return { lat, lng }
}
function mapTiles(lat: number, lng: number, width: number, height: number) {
  const center = latLngToPoint(lat, lng)
  const startX = center.x - width / 2
  const startY = center.y - height / 2
  const firstTileX = Math.floor(startX / tileSize)
  const firstTileY = Math.floor(startY / tileSize)
  const lastTileX = Math.floor((startX + width) / tileSize)
  const lastTileY = Math.floor((startY + height) / tileSize)
  const tiles: Array<{ x: number; y: number; left: number; top: number; url: string }> = []
  for (let x = firstTileX; x <= lastTileX; x += 1) for (let y = firstTileY; y <= lastTileY; y += 1) tiles.push({ x, y, left: x * tileSize - startX, top: y * tileSize - startY, url: `https://tile.openstreetmap.org/${mapZoom}/${x}/${y}.png` })
  return { tiles, startX, startY }
}

export function StoreProductsPage({ canManage }: { canManage: boolean }) {
  const [items, setItems] = useState<ProductCatalog[]>([])
  const [materialTypes, setMaterialTypes] = useState<ConsumableMaterialType[]>([])
  const [consumables, setConsumables] = useState<Consumable[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [draft, setDraft] = useState<ProductCatalog | null>(null)
  const [quoteOpen, setQuoteOpen] = useState(false)
  const [quoteStep, setQuoteStep] = useState(0)
  const [quoteInfo, setQuoteInfo] = useState<QuoteInfo>(defaultQuoteInfo())
  const [cartLines, setCartLines] = useState<CartLine[]>([])
  const [lineProduct, setLineProduct] = useState<ProductCatalog | null>(null)
  const [lineQuantity, setLineQuantity] = useState(1)
  const [lineMaterialType, setLineMaterialType] = useState('')
  const [lineConsumableId, setLineConsumableId] = useState(0)
  const [addressResults, setAddressResults] = useState<AddressResult[]>([])
  const [addressBusy, setAddressBusy] = useState(false)
  const [originPoint, setOriginPoint] = useState<OriginPoint>()
  const [routeBusy, setRouteBusy] = useState(false)
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
  const consumableById = useMemo(() => new Map(consumables.map(item => [item.id, item])), [consumables])
  const consumableOptions = useMemo(() => consumables.filter(item => item.active && item.stockGrams > 0).sort((a, b) => `${a.material} ${a.color} ${a.name}`.localeCompare(`${b.material} ${b.color} ${b.name}`)), [consumables])
  const lineConsumableOptions = useMemo(() => consumableOptions.filter(item => !lineMaterialType || item.material === lineMaterialType), [consumableOptions, lineMaterialType])
  const selectedConsumable = lineConsumableId ? consumableById.get(lineConsumableId) : undefined
  const mapData = useMemo(() => mapTiles(quoteInfo.latitude || defaultMapCenter.lat, quoteInfo.longitude || defaultMapCenter.lng, 640, 240), [quoteInfo.latitude, quoteInfo.longitude])
  const quoteProgress = ((quoteStep + 1) / quoteSteps.length) * 100

  const consumableLabel = (item: Consumable) => `${item.name} - ${item.material} - ${item.color}`
  function materialUnitCost(item: ProductCatalog, materialType = item.materialType, pricePerKg = priceByMaterial.get(materialType) ?? 0) {
    return item.filamentGrams > 0 && pricePerKg > 0 ? item.filamentGrams / 1000 * pricePerKg : item.materialCost
  }
  function unitProductionCost(item: ProductCatalog, materialType = item.materialType, pricePerKg?: number) {
    const material = materialUnitCost(item, materialType, pricePerKg)
    return material + material * clampPercent(item.maintenancePercent) / 100 + material * clampPercent(item.preparationPercent) / 100 + material * clampPercent(item.laborPercent) / 100 + material * clampPercent(item.wastePercent) / 100 + material * clampPercent(item.overheadPercent) / 100 + Math.max(0, item.packagingCost || 0)
  }
  function finalUnitPrice(item: ProductCatalog, materialType = item.materialType, pricePerKg?: number) { return unitProductionCost(item, materialType, pricePerKg) * Math.max(1.25, item.profitMultiplier) }
  function productPercentTotal(item: ProductCatalog) { return clampPercent(item.maintenancePercent) + clampPercent(item.preparationPercent) + clampPercent(item.laborPercent) + clampPercent(item.wastePercent) + clampPercent(item.overheadPercent) }

  const cartEstimate = useMemo(() => cartLines.reduce((sum, line) => {
    const product = productById.get(line.productId)
    const consumable = line.consumableId ? consumableById.get(line.consumableId) : undefined
    return sum + (product ? finalUnitPrice(product, line.materialType, consumable?.pricePerUnit) * line.quantity : 0)
  }, quoteInfo.transportCost), [cartLines, productById, consumableById, priceByMaterial, quoteInfo.transportCost])

  async function load() {
    setLoading(true); setError('')
    try {
      const [productItems, typeItems, consumableItems, config] = await Promise.all([api.products(true), api.materialTypes(true), api.consumables(), api.settings()])
      setItems(productItems.map(item => ({ ...blank, ...item }))); setMaterialTypes(typeItems); setConsumables(consumableItems); setSettings(config)
    } catch (reason) { setError((reason as Error).message) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])

  function edit(item?: ProductCatalog) {
    const baseProfit = Math.max(settings?.defaultProfitMultiplier ?? 1.4, marginToMultiplier(minProfitMargin))
    const next = item ? { ...blank, ...item, profitMultiplier: Math.max(item.profitMultiplier, marginToMultiplier(minProfitMargin)) } : { ...blank, materialType: materialOptions[0] ?? '', profitMultiplier: baseProfit, maintenancePercent: settings?.maintenancePercent ?? 6, preparationPercent: settings?.preparationPercent ?? 10, laborPercent: settings?.laborPercent ?? 20, wastePercent: settings?.wastePercent ?? 7, overheadPercent: settings?.overheadPercent ?? 5, packagingCost: settings?.packagingCost ?? 0 }
    setDraft(next); setError('')
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!draft) return
    if (!draft.name.trim()) { setError('El nombre del producto es obligatorio.'); return }
    if (!draft.materialType.trim()) { setError('Selecciona el tipo de material base.'); return }
    if (draft.filamentGrams <= 0) { setError('Indica los gramos de filamento por unidad.'); return }
    if (draft.productionMinutes <= 0) { setError('Indica el tiempo de produccion por unidad.'); return }
    if (draft.profitMultiplier < marginToMultiplier(minProfitMargin)) { setError('La ganancia final debe ser al menos 25%.'); return }
    setBusy(true); setError(''); setSuccess('')
    try { await api.saveStoreProduct({ ...draft, materialCost: materialUnitCost(draft), maintenancePercent: clampPercent(draft.maintenancePercent), preparationPercent: clampPercent(draft.preparationPercent), laborPercent: clampPercent(draft.laborPercent), wastePercent: clampPercent(draft.wastePercent), overheadPercent: clampPercent(draft.overheadPercent), packagingCost: Math.max(0, draft.packagingCost) }); setSuccess('Producto de tienda guardado.'); setDraft(null); await load() }
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

  function startQuote() { setQuoteOpen(true); setQuoteStep(0); setSuccess(''); setError('') }
  function updateQuoteInfo(next: Partial<QuoteInfo>) { setQuoteInfo(current => ({ ...current, ...next })) }
  function updateLocation(latitude: number, longitude: number, address = quoteInfo.address) { updateQuoteInfo({ latitude, longitude, address, routeKm: 0, routeMinutes: 0, transportCost: 0 }) }
  function setMapPin(event: MouseEvent<HTMLDivElement>) {
    const rect = event.currentTarget.getBoundingClientRect()
    const next = pointToLatLng(mapData.startX + event.clientX - rect.left, mapData.startY + event.clientY - rect.top)
    updateLocation(Number(next.lat.toFixed(6)), Number(next.lng.toFixed(6)))
  }

  async function searchAddress() {
    const query = quoteInfo.address.trim()
    if (!query) { setError('Escribe una direccion para buscarla en el mapa.'); return }
    setAddressBusy(true); setError('')
    try {
      const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&limit=5&addressdetails=1&q=${encodeURIComponent(query)}`, { headers: { Accept: 'application/json' } })
      if (!response.ok) throw new Error('No se pudo buscar la direccion.')
      const results = await response.json() as AddressResult[]
      setAddressResults(results)
      if (results[0]) updateLocation(Number(results[0].lat), Number(results[0].lon), results[0].display_name)
      else setError('No encontre esa direccion. Puedes mover el pin manualmente en el mapa.')
    } catch (reason) { setError((reason as Error).message) }
    finally { setAddressBusy(false) }
  }
  function useCurrentLocationForRoute() {
    if (!navigator.geolocation) { setError('Tu navegador no permite obtener ubicacion.'); return }
    setRouteBusy(true); setError('')
    navigator.geolocation.getCurrentPosition(
      position => { const origin = { latitude: position.coords.latitude, longitude: position.coords.longitude }; setOriginPoint(origin); void calculateRoute(origin) },
      () => { setRouteBusy(false); setError('No pude obtener tu ubicacion. Revisa los permisos del navegador.') },
      { enableHighAccuracy: true, timeout: 12000, maximumAge: 60000 }
    )
  }
  async function calculateRoute(origin = originPoint) {
    if (!origin) { useCurrentLocationForRoute(); return }
    setRouteBusy(true); setError('')
    try {
      const response = await fetch(`https://router.project-osrm.org/route/v1/driving/${origin.longitude},${origin.latitude};${quoteInfo.longitude},${quoteInfo.latitude}?overview=false&alternatives=false&steps=false`, { headers: { Accept: 'application/json' } })
      if (!response.ok) throw new Error('No se pudo calcular la ruta por recorrido.')
      const payload = await response.json() as { routes?: Array<{ distance: number; duration: number }> }
      const route = payload.routes?.[0]
      if (!route) throw new Error('No encontre una ruta disponible hasta esa ubicacion.')
      const routeKm = Number((route.distance / 1000).toFixed(2))
      const transportCost = Number((routeKm * deliveryCostPerKm).toFixed(2))
      updateQuoteInfo({ routeKm, routeMinutes: Number((route.duration / 60).toFixed(0)), transportCost })
      setSuccess(`Transporte calculado: ${routeKm} km por recorrido, ${transportCost.toFixed(2)} Bs.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setRouteBusy(false) }
  }

  function openLine(product: ProductCatalog) {
    if (!quoteOpen) { setError('Primero presiona Cotizar y registra los datos del cliente.'); return }
    const preferredType = product.materialType || materialOptions[0] || ''
    const preferred = consumableOptions.find(item => item.material === preferredType) ?? consumableOptions[0]
    setLineProduct(product); setLineQuantity(1); setLineMaterialType(preferred?.material ?? preferredType); setLineConsumableId(preferred?.id ?? 0)
  }

  function addLine(event: FormEvent) {
    event.preventDefault()
    if (!lineProduct) return
    if (lineQuantity <= 0) { setError('La cantidad debe ser mayor que cero.'); return }
    const consumable = consumableById.get(lineConsumableId)
    if (!consumable) { setError('Selecciona el material/color del inventario.'); return }
    setCartLines(current => {
      const index = current.findIndex(line => line.productId === lineProduct.id && line.consumableId === consumable.id)
      const nextLine = { productId: lineProduct.id, quantity: lineQuantity, materialType: consumable.material, materialLabel: consumableLabel(consumable), consumableId: consumable.id }
      if (index < 0) return [...current, nextLine]
      return current.map((line, i) => i === index ? { ...line, quantity: line.quantity + lineQuantity } : line)
    })
    setLineProduct(null); setLineQuantity(1); setLineMaterialType(''); setLineConsumableId(0); setError(''); setQuoteStep(2)
  }

  async function finishQuote() {
    if (!quoteInfo.customer.trim()) { setError('Escribe el nombre del cliente.'); setQuoteStep(0); return }
    if (!quoteInfo.phone.trim()) { setError('Escribe el telefono del cliente.'); setQuoteStep(0); return }
    if (!quoteInfo.description.trim()) { setError('Escribe una descripcion de la cotizacion.'); setQuoteStep(0); return }
    if (!quoteInfo.address.trim()) { setError('Escribe la direccion de entrega.'); setQuoteStep(0); return }
    if (cartLines.length === 0) { setError('Agrega al menos un producto al carrito.'); setQuoteStep(1); return }
    if (!await confirmDialog({ title: 'Terminar cotizacion', message: 'Se cerrara este carrito y pasara a Cotizaciones tienda para calcular, guardar o confirmar la venta.', highlight: `${quoteInfo.customer} - ${cartLines.length} productos`, confirmLabel: 'Si, terminar', variant: 'success' })) return
    const cart: StoreCart = { id: `${Date.now()}`, createdAt: new Date().toISOString(), info: quoteInfo, lines: cartLines }
    const current = JSON.parse(sessionStorage.getItem(cartKey) ?? '[]') as StoreCart[]
    sessionStorage.setItem(cartKey, JSON.stringify([cart, ...current].slice(0, 20)))
    window.dispatchEvent(new Event('store-cart-created'))
    setCartLines([]); setQuoteInfo(defaultQuoteInfo()); setQuoteOpen(false); setQuoteStep(0)
    setSuccess('Cotizacion de tienda terminada. Puedes verla en Cotizaciones tienda.')
  }

  return <>
    <PageHeader eyebrow="TIENDA" title="Productos de tienda" description="Administra productos y arma cotizaciones tipo carrito desde el catalogo." actions={<div className="summary-actions"><button className="secondary" onClick={startQuote}><ShoppingCart size={17} />Cotizar</button>{quoteOpen && <button className="danger-button" onClick={finishQuote}><Send size={17} />Terminar cotizacion</button>}{canManage && <button onClick={() => edit()}><Plus size={17} />Nuevo producto</button>}</div>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {quoteOpen && <section className="panel quote-search-panel">
      <div className="section-title"><div><p className="eyebrow">CARRITO DE TIENDA</p><h2>{quoteSteps[quoteStep].title}</h2></div><button className="icon ghost" onClick={() => setQuoteOpen(false)}><X size={18} /></button></div>
      <div className="wizard-shell"><div className="wizard-topline"><div><p className="eyebrow">PASO {quoteStep + 1} DE {quoteSteps.length}</p><h2>{quoteSteps[quoteStep].title}</h2><span>{quoteSteps[quoteStep].description}</span></div></div><div className="wizard-progress" aria-hidden="true"><span style={{ width: `${quoteProgress}%` }} /></div><div className="wizard-steps">{quoteSteps.map((step, index) => <button type="button" key={step.title} className={quoteStep === index ? 'active' : ''} onClick={() => setQuoteStep(index)}><span>{index + 1}</span>{step.title}</button>)}</div></div>
      {quoteStep === 0 && <div className="store-quote-step"><div className="form-grid two"><label>Nombre<input value={quoteInfo.customer} onChange={e => updateQuoteInfo({ customer: e.target.value })} /></label><label>Telefono<input inputMode="tel" value={quoteInfo.phone} onChange={e => updateQuoteInfo({ phone: e.target.value })} /></label><label className="span-2">Descripcion<input value={quoteInfo.description} onChange={e => updateQuoteInfo({ description: e.target.value })} /></label></div><div className="location-picker"><div className="location-header"><div><p className="eyebrow">ENTREGA</p><h3>Direccion y ubicacion</h3></div><span>{quoteInfo.latitude.toFixed(5)}, {quoteInfo.longitude.toFixed(5)}</span></div><div className="inline-form location-search"><label className="search-field"><Search size={17} /><input value={quoteInfo.address} onChange={e => updateQuoteInfo({ address: e.target.value })} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void searchAddress() } }} placeholder="Buscar direccion, zona o referencia" /></label><button type="button" className="secondary" disabled={addressBusy} onClick={searchAddress}>{addressBusy ? 'Buscando' : 'Buscar'}</button></div>{addressResults.length > 0 && <div className="address-results">{addressResults.map(item => <button type="button" key={`${item.lat}-${item.lon}`} onClick={() => updateLocation(Number(item.lat), Number(item.lon), item.display_name)}>{item.display_name}</button>)}</div>}<div className="mini-map" role="button" tabIndex={0} onClick={setMapPin} aria-label="Mapa para marcar la ubicacion de entrega">{mapData.tiles.map(tile => <img key={`${tile.x}-${tile.y}`} src={tile.url} alt="" style={{ left: tile.left, top: tile.top }} draggable={false} />)}<div className="map-pin"><MapPin size={30} fill="currentColor" /></div><span className="map-hint">Toca el mapa para mover el pin</span></div><div className="route-tools"><button type="button" className="secondary" disabled={routeBusy} onClick={useCurrentLocationForRoute}>Usar mi ubicacion y calcular ruta</button><button type="button" className="ghost" disabled={routeBusy || !originPoint} onClick={() => void calculateRoute()}>{routeBusy ? 'Calculando' : 'Recalcular ruta'}</button></div>{quoteInfo.routeKm > 0 && <div className="route-summary"><div><span>Recorrido</span><strong>{quoteInfo.routeKm.toFixed(2)} km</strong></div><div><span>Tiempo estimado</span><strong>{quoteInfo.routeMinutes} min</strong></div><div><span>Transporte</span><strong>{money(quoteInfo.transportCost, settings?.currencySymbol)}</strong></div><small>{deliveryCostPerKm.toFixed(2)} Bs por kilometro recorrido</small></div>}</div></div>}
      {quoteStep === 1 && <div className="store-quote-step">{cartLines.length === 0 ? <Empty>Usa el boton Agregar en los productos del catalogo.</Empty> : <div className="line-list">{cartLines.map((line, index) => { const product = productById.get(line.productId); const consumable = consumableById.get(line.consumableId); return <div className="line-item" key={`${line.productId}-${line.consumableId}`}><div><strong>{product?.name}</strong><small>{line.materialLabel || line.materialType} - {line.quantity} unid.</small></div><b>{money(product ? finalUnitPrice(product, line.materialType, consumable?.pricePerUnit) * line.quantity : 0, settings?.currencySymbol)}</b><button className="icon danger" onClick={() => setCartLines(current => current.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div> })}</div>}<p className="field-help">Al agregar cada producto se calcula con el material/color elegido y los porcentajes guardados en ese producto.</p></div>}
      {quoteStep === 2 && <div className="store-quote-step"><div className="breakdown"><div><dt>Cliente</dt><dd>{quoteInfo.customer || 'Pendiente'}</dd></div><div><dt>Telefono</dt><dd>{quoteInfo.phone || 'Pendiente'}</dd></div><div><dt>Direccion</dt><dd>{quoteInfo.address || 'Pendiente'}</dd></div><div><dt>Productos</dt><dd>{cartLines.length}</dd></div><div><dt>Transporte</dt><dd>{money(quoteInfo.transportCost, settings?.currencySymbol)}</dd></div><div className="subtotal"><dt>Estimado del carrito</dt><dd>{money(cartEstimate, settings?.currencySymbol)}</dd></div></div><div className="formula-note"><strong>Estimado del carrito: {money(cartEstimate, settings?.currencySymbol)}</strong><span>El precio final se recalcula en Cotizaciones tienda al asignar impresoras libres.</span></div></div>}
      <div className="wizard-actions"><button className="ghost" type="button" disabled={quoteStep === 0} onClick={() => setQuoteStep(current => Math.max(0, current - 1))}><ArrowLeft size={16} />Atras</button><span>Paso {quoteStep + 1} de {quoteSteps.length}</span>{quoteStep < quoteSteps.length - 1 ? <button type="button" onClick={() => setQuoteStep(current => Math.min(quoteSteps.length - 1, current + 1))}>Siguiente<ArrowRight size={16} /></button> : <button className="danger-button" type="button" onClick={finishQuote}><Send size={17} />Terminar cotizacion</button>}</div>
    </section>}

    <div className={`catalog-layout ${draft ? 'with-editor' : ''}`}>
      <section className="panel">
        {loading ? <Loading /> : items.length === 0 ? <Empty>No hay productos de tienda registrados.</Empty> : <div className="card-grid">{items.map(item => <article className={`catalog-card ${item.active ? '' : 'archived'}`} key={item.id}>
          <div className="catalog-icon"><PackagePlus size={20} /></div>
          <div className="catalog-main"><div><h3>{item.name}</h3>{!item.active && <span className="status warning">ARCHIVADO</span>}</div><p>{item.description || 'Sin descripcion'}</p><small>{item.materialType || 'Sin material'} - {weight(item.filamentGrams)} por unidad - {number(item.productionMinutes)} min - margen {multiplierToMargin(item.profitMultiplier).toFixed(2)}%</small><small>Costos {productPercentTotal(item).toFixed(2)}% + empaque {money(item.packagingCost, settings?.currencySymbol)} - Precio sugerido {money(finalUnitPrice(item), settings?.currencySymbol)}</small></div>
          <div className="card-actions">{quoteOpen && item.active && <button className="small" onClick={() => openLine(item)}><ShoppingCart size={15} />Agregar</button>}{canManage && <button className="ghost small" onClick={() => edit(item)}><Edit3 size={15} />Editar</button>}{canManage && item.active && <button className="ghost small danger-text" disabled={busy} onClick={() => archive(item)}><Archive size={15} />Archivar</button>}</div>
        </article>)}</div>}
      </section>
      {draft && <form className="panel editor" onSubmit={save}>
        <div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR PRODUCTO' : 'NUEVO PRODUCTO'}</p><h2>{draft.id ? draft.name : 'Producto de tienda'}</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}><X size={18} /></button></div>
        <label>Nombre<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Descripcion<textarea rows={3} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
        <div className="form-grid two"><label>Material base<select required value={draft.materialType} onChange={e => setDraft({ ...draft, materialType: e.target.value })}><option value="">Seleccionar material</option>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label><label>Filamento por unidad (g)<input type="number" min="0.01" step="0.01" value={draft.filamentGrams} onChange={e => setDraft({ ...draft, filamentGrams: Number(e.target.value) })} /></label><label>Tiempo por unidad (min)<input type="number" min="0.01" step="0.01" value={draft.productionMinutes} onChange={e => setDraft({ ...draft, productionMinutes: Number(e.target.value) })} /></label><label>Margen de ganancia final (%)<input type="number" min={minProfitMargin} step="0.01" value={multiplierToMargin(draft.profitMultiplier)} onChange={e => setDraft({ ...draft, profitMultiplier: marginToMultiplier(Number(e.target.value)) })} /></label></div>
        <div className="margin-shortcuts"><span>Margen rapido</span>{[25, 35, 50].map(value => <button key={value} type="button" className={Math.round(multiplierToMargin(draft.profitMultiplier)) === value ? 'active' : ''} onClick={() => setDraft({ ...draft, profitMultiplier: marginToMultiplier(value) })}>{value}%</button>)}</div>
        <div className="profit-percent-live"><div><span>Mantenimiento</span><strong>{clampPercent(draft.maintenancePercent).toFixed(2)}%</strong></div><div><span>Preparacion</span><strong>{clampPercent(draft.preparationPercent).toFixed(2)}%</strong></div><div><span>Salarios</span><strong>{clampPercent(draft.laborPercent).toFixed(2)}%</strong></div><div><span>Total</span><strong>{productPercentTotal(draft).toFixed(2)}%</strong></div><div className="profit-percent-meter" aria-hidden="true"><span style={{ width: `${Math.min(100, productPercentTotal(draft))}%` }} /></div></div>
        <div className="form-grid three compact-fields"><label>Mantenimiento (%)<input type="number" min="0" step="0.01" value={draft.maintenancePercent} onChange={e => setDraft({ ...draft, maintenancePercent: Number(e.target.value) })} /></label><label>Preparacion (%)<input type="number" min="0" step="0.01" value={draft.preparationPercent} onChange={e => setDraft({ ...draft, preparationPercent: Number(e.target.value) })} /></label><label>Salarios (%)<input type="number" min="0" step="0.01" value={draft.laborPercent} onChange={e => setDraft({ ...draft, laborPercent: Number(e.target.value) })} /></label><label>Merma/fallos (%)<input type="number" min="0" step="0.01" value={draft.wastePercent} onChange={e => setDraft({ ...draft, wastePercent: Number(e.target.value) })} /></label><label>Administracion (%)<input type="number" min="0" step="0.01" value={draft.overheadPercent} onChange={e => setDraft({ ...draft, overheadPercent: Number(e.target.value) })} /></label><label>Empaque por unidad<input type="number" min="0" step="0.01" value={draft.packagingCost} onChange={e => setDraft({ ...draft, packagingCost: Number(e.target.value) })} /></label></div>
        <div className="breakdown"><div><dt>Costo de produccion estimado</dt><dd>{money(unitProductionCost(draft), settings?.currencySymbol)}</dd></div><div><dt>Precio sugerido por unidad</dt><dd>{money(finalUnitPrice(draft), settings?.currencySymbol)}</dd></div></div>
        <p className="field-help">Estos porcentajes quedan guardados en el producto. Al cotizar, solo se elige material/color y cantidad; el sistema jala estos valores automaticamente.</p>
        <div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button disabled={busy}>{busy ? 'Guardando...' : 'Guardar producto'}</button></div>
      </form>}
    </div>
    {lineProduct && <div className="confirm-overlay"><form className="confirm-dialog store-product-modal" onSubmit={addLine}><button type="button" className="confirm-close icon ghost" onClick={() => setLineProduct(null)}><X size={18} /></button><div className="confirm-icon"><ShoppingCart size={24} /></div><p className="eyebrow">AGREGAR AL CARRITO</p><h2>{lineProduct.name}</h2>{lineProduct.description && <p>{lineProduct.description}</p>}<div className="form-grid two"><label>Cantidad<input type="number" min="1" step="1" value={lineQuantity} onChange={e => setLineQuantity(Number(e.target.value))} /></label><label>Tipo de material<select value={lineMaterialType} onChange={e => { const nextType = e.target.value; const first = consumableOptions.find(item => item.material === nextType); setLineMaterialType(nextType); setLineConsumableId(first?.id ?? 0) }}><option value="">Seleccionar tipo</option>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label><label className="span-2">Material/color del inventario<select value={lineConsumableId} onChange={e => setLineConsumableId(Number(e.target.value))}><option value="0">Seleccionar material</option>{lineConsumableOptions.map(item => <option key={item.id} value={item.id}>{consumableLabel(item)} - {money(item.pricePerUnit, settings?.currencySymbol)}/kg - {weight(item.stockGrams)}</option>)}</select></label></div>{lineConsumableOptions.length === 0 && <p className="error" role="alert">No hay materiales con stock disponible para ese tipo.</p>}<dl className="breakdown"><div><dt>Filamento por unidad</dt><dd>{weight(lineProduct.filamentGrams)}</dd></div><div><dt>Filamento total</dt><dd>{weight(lineProduct.filamentGrams * lineQuantity)}</dd></div><div><dt>Tiempo estimado</dt><dd>{number(lineProduct.productionMinutes * lineQuantity)} min</dd></div><div><dt>Porcentajes guardados</dt><dd>{productPercentTotal(lineProduct).toFixed(2)}%</dd></div><div><dt>Ganancia registrada</dt><dd>{multiplierToMargin(lineProduct.profitMultiplier).toFixed(2)}%</dd></div><div><dt>Material elegido</dt><dd>{selectedConsumable ? consumableLabel(selectedConsumable) : 'Pendiente'}</dd></div><div><dt>Precio estimado</dt><dd>{money(finalUnitPrice(lineProduct, selectedConsumable?.material ?? lineMaterialType, selectedConsumable?.pricePerUnit) * lineQuantity, settings?.currencySymbol)}</dd></div></dl><div className="confirm-actions"><button type="button" className="ghost" onClick={() => setLineProduct(null)}>Cancelar</button><button><Plus size={17} />Agregar al carrito</button></div></form></div>}
  </>
}
