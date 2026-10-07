import { useEffect, useMemo, useState, type PointerEvent } from 'react'
import { ArrowLeft, ArrowRight, Calculator, CheckCircle2, Download, MapPin, Plus, RotateCcw, Save, Search, ShoppingBag, Trash2, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Consumable, ConsumableUsage, ExtraMaterial, MaterialUsage, Printer, ProductCatalog, QuoteCalculation, QuoteRequest, QuoteSummary } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

type BaseForm = { customer: string; customerPhone: string; projectName: string; address: string; latitude: number; longitude: number; printerId: number; hours: number; minutes: number; quantity: number; additionalManualCost: number; workExtraPercent: number; maintenancePercent: number; preparationPercent: number; laborPercent: number; wastePercent: number; overheadPercent: number; packagingCost: number; transportCost: number; profitMultiplier: number; notes: string }
const emptyForm: BaseForm = { customer: '', customerPhone: '', projectName: '', address: '', latitude: -17.7833, longitude: -63.1821, printerId: 0, hours: 0, minutes: 0, quantity: 1, additionalManualCost: 0, workExtraPercent: 0, maintenancePercent: 6, preparationPercent: 10, laborPercent: 20, wastePercent: 7, overheadPercent: 5, packagingCost: 0, transportCost: 0, profitMultiplier: 1.4, notes: '' }
type ProfitCostLine = { id: string; name: string; percent: number; fixed?: boolean }
const defaultProfitCosts: ProfitCostLine[] = [
  { id: 'salary', name: 'Costos de salarios', percent: 0, fixed: true },
  { id: 'debt', name: 'Costos de inversion o deudas', percent: 0, fixed: true },
  { id: 'manual', name: 'Costo manual', percent: 0, fixed: true },
  { id: 'work', name: 'Adicional del trabajo', percent: 0, fixed: true },
]
const clampPercent = (value: number) => Math.min(100, Math.max(0, Number.isFinite(value) ? Number(value.toFixed(2)) : 0))
const minProfitMargin = 25
const marginToMultiplier = (margin: number) => Number((1 + Math.max(minProfitMargin, Number.isFinite(margin) ? margin : minProfitMargin) / 100).toFixed(4))
const multiplierToMargin = (multiplier: number) => Number(Math.max(minProfitMargin, ((Number.isFinite(multiplier) ? multiplier : 1.25) - 1) * 100).toFixed(2))
type AddressResult = { display_name: string; lat: string; lon: string }
type RouteInfo = { kilometers: number; cost: number; durationMinutes: number }
type OriginPoint = { latitude: number; longitude: number }
type MapDragState = { pointerId: number; startClientX: number; startClientY: number; startPointX: number; startPointY: number; moved: boolean }
type MapDragOffset = { x: number; y: number }
const mapZoom = 14
const tileSize = 256
const defaultMapCenter = { lat: -17.7833, lng: -63.1821 }
const deliveryCostPerKm = 3.20
const quoteWizardSteps = [
  { title: 'Proyecto', description: 'Cliente, pieza, impresora, tiempo y entrega.' },
  { title: 'Consumibles', description: 'Filamentos o resinas que se usaran en la impresion.' },
  { title: 'Materiales', description: 'Materiales extra, acabados y observaciones.' },
  { title: 'Precio', description: 'Costos operativos, transporte y margen final.' },
] as const

function latLngToPoint(lat: number, lng: number, zoom = mapZoom) {
  const scale = tileSize * 2 ** zoom
  const sinLat = Math.sin(lat * Math.PI / 180)
  return {
    x: (lng + 180) / 360 * scale,
    y: (0.5 - Math.log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * scale,
  }
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
  for (let x = firstTileX; x <= lastTileX; x += 1) {
    for (let y = firstTileY; y <= lastTileY; y += 1) {
      tiles.push({ x, y, left: x * tileSize - startX, top: y * tileSize - startY, url: `https://tile.openstreetmap.org/${mapZoom}/${x}/${y}.png` })
    }
  }
  return { tiles, startX, startY }
}

export function QuotePage({ canWrite }: { canWrite: boolean }) {
  const [printers, setPrinters] = useState<Printer[]>([])
  const [consumables, setConsumables] = useState<Consumable[]>([])
  const [materials, setMaterials] = useState<ExtraMaterial[]>([])
  const [products, setProducts] = useState<ProductCatalog[]>([])
  const [selectedProduct, setSelectedProduct] = useState('')
  const [customPrice, setCustomPrice] = useState(0)
  const [settings, setSettings] = useState<BusinessSettings>()
  const [form, setForm] = useState<BaseForm>(emptyForm)
  const [consumableLines, setConsumableLines] = useState<ConsumableUsage[]>([])
  const [materialLines, setMaterialLines] = useState<MaterialUsage[]>([])
  const [selectedConsumable, setSelectedConsumable] = useState(0)
  const [grams, setGrams] = useState(0)
  const [selectedMaterial, setSelectedMaterial] = useState(0)
  const [materialQuantity, setMaterialQuantity] = useState(1)
  const [calculation, setCalculation] = useState<QuoteCalculation>()
  const [saved, setSaved] = useState<QuoteSummary>()
  const [sold, setSold] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [searchOpen, setSearchOpen] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [searchResults, setSearchResults] = useState<QuoteSummary[]>([])
  const [searching, setSearching] = useState(false)
  const [profitCosts, setProfitCosts] = useState<ProfitCostLine[]>(defaultProfitCosts)
  const [newProfitCostName, setNewProfitCostName] = useState('')
  const [newProfitCostPercent, setNewProfitCostPercent] = useState(0)
  const [addressResults, setAddressResults] = useState<AddressResult[]>([])
  const [addressBusy, setAddressBusy] = useState(false)
  const [originPoint, setOriginPoint] = useState<OriginPoint>()
  const [routeInfo, setRouteInfo] = useState<RouteInfo>()
  const [routeBusy, setRouteBusy] = useState(false)
  const [mapDrag, setMapDrag] = useState<MapDragState | null>(null)
  const [mapOffset, setMapOffset] = useState<MapDragOffset>({ x: 0, y: 0 })
  const [wizardStep, setWizardStep] = useState(0)

  useEffect(() => {
    Promise.all([api.printers(), api.consumables(), api.materials(), api.settings(), api.products()])
      .then(([printerItems, consumableItems, materialItems, configuration, productItems]) => {
        const availablePrinters = printerItems.filter(x => x.active)
        setPrinters(availablePrinters); setConsumables(consumableItems); setMaterials(materialItems); setSettings(configuration); setProducts(productItems)
        setForm(current => ({ ...current, printerId: availablePrinters[0]?.id ?? 0, profitMultiplier: Math.max(configuration.defaultProfitMultiplier, marginToMultiplier(minProfitMargin)), maintenancePercent: configuration.maintenancePercent, preparationPercent: configuration.preparationPercent, laborPercent: configuration.laborPercent, wastePercent: configuration.wastePercent, overheadPercent: configuration.overheadPercent, packagingCost: configuration.packagingCost, transportCost: 0 }))
        setSelectedConsumable(consumableItems.find(x => x.isDefault)?.id ?? consumableItems[0]?.id ?? 0)
        setSelectedMaterial(materialItems[0]?.id ?? 0)
      })
      .catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
  }, [])

  const consumableById = useMemo(() => new Map(consumables.map(x => [x.id, x])), [consumables])
  const materialById = useMemo(() => new Map(materials.map(x => [x.id, x])), [materials])
  const selectedPrinter = useMemo(() => printers.find(x => x.id === form.printerId), [printers, form.printerId])
  const profitMarginPercent = multiplierToMargin(form.profitMultiplier)
  const appliedConfigPercentTotal = clampPercent(form.maintenancePercent) + clampPercent(form.preparationPercent) + clampPercent(form.laborPercent) + clampPercent(form.wastePercent) + clampPercent(form.overheadPercent)
  const profitPercentTotal = useMemo(() => profitCosts.reduce((sum, line) => sum + clampPercent(line.percent), 0), [profitCosts])
  const pendingProfitPercent = clampPercent(newProfitCostPercent)
  const liveProfitPercentTotal = appliedConfigPercentTotal + profitPercentTotal + pendingProfitPercent
  const percentFill = Math.min(100, liveProfitPercentTotal)
  const estimatedBaseCost = useMemo(() => {
    if (!settings || !selectedPrinter) return 0
    const pieces = Math.max(1, Number(form.quantity || 1))
    const consumableCost = consumableLines.reduce((sum, line) => {
      const item = consumableById.get(line.consumableId)
      if (!item) return sum
      const unitCost = item.category.toLowerCase() === 'resina' ? line.grams / item.density / 1000 * item.pricePerUnit : line.grams / 1000 * item.pricePerUnit
      return sum + unitCost * pieces
    }, 0)
    const materialCost = materialLines.reduce((sum, line) => {
      const item = materialById.get(line.materialId)
      return sum + (item ? item.unitPrice * line.quantity : 0)
    }, 0)
    const printHours = form.hours + form.minutes / 60
    const electricity = selectedPrinter.powerWatts / 1000 * printHours * settings.electricityPerKwh * pieces
    const maintenance = settings.maintenancePerPrint * pieces
    return Math.max(0, consumableCost + materialCost + electricity + maintenance)
  }, [settings, selectedPrinter, form.quantity, form.hours, form.minutes, consumableLines, materialLines, consumableById, materialById])
  const profitAdditionalCost = Number((estimatedBaseCost * profitPercentTotal / 100).toFixed(2))
  const mapData = useMemo(() => mapTiles(form.latitude || defaultMapCenter.lat, form.longitude || defaultMapCenter.lng, 640, 260), [form.latitude, form.longitude])
  const wizardProgress = ((wizardStep + 1) / quoteWizardSteps.length) * 100

  function invalidate() { setCalculation(undefined); setSaved(undefined); setSold(false); setSuccess('') }
  function update<K extends keyof BaseForm>(key: K, value: BaseForm[K]) { setForm(current => ({ ...current, [key]: value })); invalidate() }
  function updateLocation(latitude: number, longitude: number, address = form.address) { setForm(current => ({ ...current, latitude, longitude, address })); setRouteInfo(undefined); invalidate() }
  function updateProfitMargin(percent: number) { update('profitMultiplier', marginToMultiplier(percent)) }
  function updateProfitCost(id: string, percent: number) { setProfitCosts(current => current.map(line => line.id === id ? { ...line, percent: clampPercent(percent) } : line)); invalidate() }
  function removeProfitCost(id: string) { setProfitCosts(current => current.filter(line => line.id !== id || line.fixed)); invalidate() }
  function addProfitCost() {
    const name = newProfitCostName.trim()
    if (!name) { setError('Escribe el nombre del costo adicional.'); return }
    setProfitCosts(current => [...current, { id: `extra-${Date.now()}`, name, percent: clampPercent(newProfitCostPercent) }])
    setNewProfitCostName(''); setNewProfitCostPercent(0); setError(''); invalidate()
  }

  function addConsumable() {
    const item = consumableById.get(selectedConsumable)
    if (!item || grams <= 0) { setError('Selecciona un consumible e indica gramos mayores que cero.'); return }
    if ((item.stockGrams ?? item.stockQuantity * 1000) <= 0) { setError(`No hay existencia de ${item.name} - ${item.material} - ${item.color}. Actualiza su inventario primero.`); return }
    setConsumableLines(current => [...current, { consumableId: item.id, grams }]); setGrams(0); setError(''); invalidate()
  }

  function addMaterial() {
    if (!materialById.has(selectedMaterial) || materialQuantity <= 0) { setError('Selecciona un material e indica una cantidad mayor que cero.'); return }
    setMaterialLines(current => [...current, { materialId: selectedMaterial, quantity: materialQuantity }]); setMaterialQuantity(1); setError(''); invalidate()
  }

  async function searchAddress() {
    const query = form.address.trim()
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
      position => {
        const origin = { latitude: position.coords.latitude, longitude: position.coords.longitude }
        setOriginPoint(origin)
        void calculateRoute(origin)
      },
      () => { setRouteBusy(false); setError('No pude obtener tu ubicacion. Revisa los permisos del navegador.') },
      { enableHighAccuracy: true, timeout: 12000, maximumAge: 60000 }
    )
  }

  async function calculateRoute(origin = originPoint) {
    if (!origin) { useCurrentLocationForRoute(); return }
    setRouteBusy(true); setError('')
    try {
      const url = `https://router.project-osrm.org/route/v1/driving/${origin.longitude},${origin.latitude};${form.longitude},${form.latitude}?overview=false&alternatives=false&steps=false`
      const response = await fetch(url, { headers: { Accept: 'application/json' } })
      if (!response.ok) throw new Error('No se pudo calcular la ruta por recorrido.')
      const payload = await response.json() as { routes?: Array<{ distance: number; duration: number }> }
      const route = payload.routes?.[0]
      if (!route) throw new Error('No encontre una ruta disponible hasta esa ubicacion.')
      const kilometers = Number((route.distance / 1000).toFixed(2))
      const cost = Number((kilometers * deliveryCostPerKm).toFixed(2))
      const durationMinutes = Number((route.duration / 60).toFixed(0))
      setRouteInfo({ kilometers, cost, durationMinutes })
      update('transportCost', cost)
      setSuccess(`Transporte calculado: ${kilometers} km por recorrido, ${cost.toFixed(2)} Bs.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setRouteBusy(false) }
  }
  function moveMapPin(clientX: number, clientY: number, target: HTMLDivElement) {
    const rect = target.getBoundingClientRect()
    const x = mapData.startX + clientX - rect.left
    const y = mapData.startY + clientY - rect.top
    const next = pointToLatLng(x, y)
    updateLocation(Number(next.lat.toFixed(6)), Number(next.lng.toFixed(6)))
  }
  function startMapDrag(event: PointerEvent<HTMLDivElement>) {
    event.preventDefault()
    const center = latLngToPoint(form.latitude || defaultMapCenter.lat, form.longitude || defaultMapCenter.lng)
    event.currentTarget.setPointerCapture(event.pointerId)
    setMapDrag({ pointerId: event.pointerId, startClientX: event.clientX, startClientY: event.clientY, startPointX: center.x, startPointY: center.y, moved: false })
  }
  function dragMap(event: PointerEvent<HTMLDivElement>) {
    if (!mapDrag || mapDrag.pointerId !== event.pointerId) return
    const dx = event.clientX - mapDrag.startClientX
    const dy = event.clientY - mapDrag.startClientY
    const moved = mapDrag.moved || Math.abs(dx) > 3 || Math.abs(dy) > 3
    if (!moved) return
    setMapOffset({ x: dx, y: dy })
    setMapDrag(current => current && current.pointerId === event.pointerId ? { ...current, moved: true } : current)
  }
  function finishMapDrag(event: PointerEvent<HTMLDivElement>) {
    if (!mapDrag || mapDrag.pointerId !== event.pointerId) return
    if (mapDrag.moved) {
      const dx = event.clientX - mapDrag.startClientX
      const dy = event.clientY - mapDrag.startClientY
      const next = pointToLatLng(mapDrag.startPointX - dx, mapDrag.startPointY - dy)
      updateLocation(Number(next.lat.toFixed(6)), Number(next.lng.toFixed(6)))
    } else {
      moveMapPin(event.clientX, event.clientY, event.currentTarget)
    }
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId)
    setMapDrag(null)
    setMapOffset({ x: 0, y: 0 })
  }

  function request(): QuoteRequest {
    const locationNotes = form.address.trim() ? `Direccion: ${form.address.trim()}\nUbicacion: ${form.latitude.toFixed(6)}, ${form.longitude.toFixed(6)}\nMapa: https://www.openstreetmap.org/?mlat=${form.latitude.toFixed(6)}&mlon=${form.longitude.toFixed(6)}#map=17/${form.latitude.toFixed(6)}/${form.longitude.toFixed(6)}` : ''
    const quoteNotes = [locationNotes, form.notes].filter(Boolean).join('\n\n')
    return { customer: form.customer, customerPhone: form.customerPhone, projectName: form.projectName, productName: selectedProduct, printerId: form.printerId, printHours: form.hours + form.minutes / 60, quantity: form.quantity, additionalManualCost: profitAdditionalCost, profitMultiplier: Math.max(form.profitMultiplier, marginToMultiplier(minProfitMargin)), notes: quoteNotes, consumables: consumableLines, materials: materialLines, maintenancePercent: form.maintenancePercent, preparationPercent: form.preparationPercent, laborPercent: form.laborPercent, wastePercent: form.wastePercent, overheadPercent: form.overheadPercent, packagingCost: form.packagingCost, transportCost: form.transportCost }
  }
  async function calculate() {
    setBusy(true); setError(''); setSuccess('')
    try { setCalculation(await api.calculateQuote(request())) }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function save() {
    if (!form.customerPhone.trim()) { setError('Escribe el celular del cliente antes de guardar.'); return }
    setBusy(true); setError(''); setSuccess('')
    try {
      const payload = request()
      const [result, totals] = await Promise.all([api.createQuote(payload), api.calculateQuote(payload)])
      const finalPrice = customPrice > 0 ? (await api.updateQuotePrice(result.id, customPrice)).recommendedPrice : result.recommendedPrice
      setSaved({ ...result, recommendedPrice: finalPrice }); setCalculation(totals); setSuccess(`Cotizacion ${result.orderCode} guardada correctamente.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function confirmSale() {
    if (!saved) return
    if (!await confirmDialog({ title: 'Confirmar venta', message: 'Se registrar la venta y se descontar el filamento del inventario.', highlight: `${saved.orderCode} - ${money(saved.recommendedPrice, settings?.currencySymbol)}`, confirmLabel: 'S, confirmar', variant: 'success' })) return
    setBusy(true); setError('')
    try { await api.confirmSale(saved.id); setSold(true); setSuccess('Venta confirmada y filamento descontado del inventario.'); window.dispatchEvent(new Event('inventory-changed')) }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function searchExisting() {
    setSearching(true); setError('')
    try { setSearchResults(await api.quotes({ search: searchText })) }
    catch (reason) { setError((reason as Error).message) }
    finally { setSearching(false) }
  }

  async function loadExisting(id: number) {
    setBusy(true); setError('')
    try {
      const source = await api.quote(id)
      const totalMinutes = Math.round(source.printHours * 60)
      const printer = printers.find(item => item.name === source.printerName)
      setForm({ customer: source.customer, customerPhone: source.customerPhone ?? '', projectName: source.projectName, address: '', latitude: defaultMapCenter.lat, longitude: defaultMapCenter.lng, printerId: printer?.id ?? printers[0]?.id ?? 0, hours: Math.floor(totalMinutes / 60), minutes: totalMinutes % 60, quantity: source.quantity, additionalManualCost: 0, workExtraPercent: 0, maintenancePercent: settings?.maintenancePercent ?? 6, preparationPercent: settings?.preparationPercent ?? 10, laborPercent: settings?.laborPercent ?? 20, wastePercent: settings?.wastePercent ?? 7, overheadPercent: settings?.overheadPercent ?? 5, packagingCost: settings?.packagingCost ?? 0, transportCost: 0, profitMultiplier: Math.max(source.profitMultiplier, marginToMultiplier(minProfitMargin)), notes: source.notes })
      setSelectedProduct(source.productName ?? '')
      setConsumableLines(source.consumables.filter(line => consumableById.has(line.legacyConsumableId)).map(line => ({ consumableId: line.legacyConsumableId, grams: line.grams })))
      setMaterialLines(source.materials.filter(line => materialById.has(line.legacyMaterialId)).map(line => ({ materialId: line.legacyMaterialId, quantity: line.quantity })))
      setCalculation({ totalWeight: source.totalWeight, materialCost: source.materialCost, electricityCost: source.electricityCost, maintenanceCost: source.maintenanceCost, preparationCost: 0, laborCost: source.laborCost, wasteCost: 0, overheadCost: source.functionalSurcharge, packagingCost: 0, transportCost: 0, additionalCost: source.additionalCost, subtotal: source.subtotal, profitAmount: source.profitAmount, taxAmount: source.taxAmount, recommendedPrice: source.recommendedPrice })
      setCustomPrice(source.recommendedPrice); setSaved(undefined); setSold(false); setSearchOpen(false); setProfitCosts(defaultProfitCosts)
      setWizardStep(0)
      setSuccess(`Datos de ${source.orderCode} cargados como una nueva cotizacion. Puedes modificarlos antes de guardar.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  function clear() {
    setForm({ ...emptyForm, printerId: printers[0]?.id ?? 0, profitMultiplier: Math.max(settings?.defaultProfitMultiplier ?? 1.4, marginToMultiplier(minProfitMargin)), maintenancePercent: settings?.maintenancePercent ?? 6, preparationPercent: settings?.preparationPercent ?? 10, laborPercent: settings?.laborPercent ?? 20, wastePercent: settings?.wastePercent ?? 7, overheadPercent: settings?.overheadPercent ?? 5, packagingCost: settings?.packagingCost ?? 0, transportCost: 0 })
    setProfitCosts(defaultProfitCosts); setNewProfitCostName(''); setNewProfitCostPercent(0)
    setConsumableLines([]); setMaterialLines([]); setCalculation(undefined); setSaved(undefined); setSold(false); setSelectedProduct(''); setCustomPrice(0); setError(''); setSuccess('')
    setWizardStep(0)
  }

  if (loading) return <Loading label="Cargando catalogos" />
  if (!canWrite) return <><PageHeader eyebrow="COTIZADOR" title="Nueva cotizacion" description="Clculo de costos y precio recomendado." /><div className="panel locked-panel"><h2>Acceso de consulta</h2><p>Tu rol puede revisar el historial, pero no crear cotizaciones. Solicita al administrador el rol Ventas si necesitas esta funcin.</p></div></>

  return <>
    <PageHeader eyebrow="COTIZADOR 3D" title="Nueva cotizacion" description="Registra los datos de impresin, combina consumibles y obtn el precio con la frmula original." actions={<button className="secondary" onClick={() => { setSearchOpen(value => !value); if (!searchOpen && searchResults.length === 0) void searchExisting() }}><Search size={17} />Buscar cotizacion o venta</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {searchOpen && <section className="panel quote-search-panel"><div className="section-title"><div><p className="eyebrow">REUTILIZAR DATOS</p><h2>Buscar cotizaciones y ventas</h2></div><button className="icon ghost" onClick={() => setSearchOpen(false)}><X size={18} /></button></div><div className="inline-form"><label className="search-field"><Search size={17} /><input value={searchText} onChange={e => setSearchText(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void searchExisting() } }} placeholder="Codigo, cliente o producto" /></label><button onClick={searchExisting} disabled={searching}>{searching ? 'Buscando' : 'Buscar'}</button></div>{searching ? <Loading label="Buscando" /> : searchResults.length === 0 ? <Empty>No se encontraron registros.</Empty> : <div className="line-list quote-search-results">{searchResults.map(item => <div className="line-item" key={item.id}><div><strong>{item.customer} - {item.productName || item.projectName}</strong><small>{item.orderCode} - {item.soldAtUtc ? 'Venta confirmada' : 'Cotizacion pendiente'} - {money(item.recommendedPrice, settings?.currencySymbol)}</small></div><button onClick={() => loadExisting(item.id)}>Cargar como nueva</button></div>)}</div>}</section>}
    {printers.length === 0 ? <div className="alert error">Registra al menos una impresora disponible antes de cotizar.</div> : null}
    {consumables.length === 0 ? <div className="alert error">Necesitas al menos un consumible activo antes de cotizar.</div> : null}
    <div className="quote-layout">
      <div className="form-stack">
        <div className="wizard-shell">
          <div className="wizard-topline">
            <div>
              <p className="eyebrow">PASO {wizardStep + 1} DE {quoteWizardSteps.length}</p>
              <h2>{quoteWizardSteps[wizardStep].title}</h2>
              <span>{quoteWizardSteps[wizardStep].description}</span>
            </div>
          </div>
          <div className="wizard-progress" aria-hidden="true"><span style={{ width: `${wizardProgress}%` }} /></div>
          <div className="wizard-steps" aria-label="Pasos del cotizador personalizado">
            {quoteWizardSteps.map((step, index) => <button type="button" key={step.title} className={index === wizardStep ? 'active' : ''} onClick={() => setWizardStep(index)}><span>{index + 1}</span>{step.title}</button>)}
          </div>
        </div>

        {wizardStep === 0 && <section className="panel">
          <div className="section-title"><div><span className="step">01</span><h2>Proyecto e impresin</h2></div></div>
          <div className="form-grid two">
            <label>Cliente<input value={form.customer} onChange={e => update('customer', e.target.value)} placeholder="Nombre del cliente" /></label>
            <label>Celular del cliente <small>(solo al guardar)</small><input value={form.customerPhone} onChange={e => update('customerPhone', e.target.value)} placeholder="70000000" inputMode="tel" /></label>
            <label>Pieza o proyecto<input value={form.projectName} onChange={e => update('projectName', e.target.value)} placeholder="Ej. Soporte personalizado" /></label>
            <label>Producto a la venta<select value={selectedProduct} onChange={e => { setSelectedProduct(e.target.value); invalidate() }}><option value="">Producto personalizado</option>{products.map(item => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>
            <label className="span-2">Impresora<select value={form.printerId} onChange={e => update('printerId', Number(e.target.value))}>{printers.map(x => <option key={x.id} value={x.id}>{x.name} - {x.status} - {x.buildX}x{x.buildY}x{x.buildZ} mm - {x.colorCount ?? 1} color{(x.colorCount ?? 1) === 1 ? '' : 'es'}</option>)}</select></label>
            <label>Horas<input type="number" min="0" step="1" value={form.hours} onChange={e => update('hours', Number(e.target.value))} /></label>
            <label>Minutos<input type="number" min="0" max="59" step="1" value={form.minutes} onChange={e => update('minutes', Number(e.target.value))} /></label>
            <label>Cantidad de piezas<input type="number" min="1" step="1" value={form.quantity} onChange={e => update('quantity', Number(e.target.value))} /></label>
          </div>
          <div className="location-picker">
            <div className="location-header"><div><p className="eyebrow">ENTREGA</p><h3>Direccion y ubicacion</h3></div><span>{form.latitude.toFixed(5)}, {form.longitude.toFixed(5)}</span></div>
            <div className="inline-form location-search">
              <label className="search-field"><Search size={17} /><input value={form.address} onChange={e => update('address', e.target.value)} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void searchAddress() } }} placeholder="Buscar direccion, zona o referencia" /></label>
              <button type="button" className="secondary" disabled={addressBusy} onClick={searchAddress}>{addressBusy ? 'Buscando' : 'Buscar'}</button>
            </div>
            {addressResults.length > 0 && <div className="address-results">{addressResults.map(item => <button type="button" key={`${item.lat}-${item.lon}`} onClick={() => updateLocation(Number(item.lat), Number(item.lon), item.display_name)}>{item.display_name}</button>)}</div>}
            <div className={`mini-map ${mapDrag ? 'dragging' : ''}`} role="button" tabIndex={0} onPointerDown={startMapDrag} onPointerMove={dragMap} onPointerUp={finishMapDrag} onPointerCancel={finishMapDrag} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault() } }} aria-label="Mapa para marcar la ubicacion de entrega">
              {mapData.tiles.map(tile => <img key={`${tile.x}-${tile.y}`} src={tile.url} alt="" style={{ left: tile.left, top: tile.top, transform: `translate(${mapOffset.x}px, ${mapOffset.y}px)` }} draggable={false} />)}
              <div className="map-pin"><MapPin size={30} fill="currentColor" /></div>
              <span className="map-hint">Manten clic y arrastra el mapa para panear; suelta para fijar</span>
            </div>
            <div className="route-tools">
              <button type="button" className="secondary" disabled={routeBusy} onClick={useCurrentLocationForRoute}>Usar mi ubicacion y calcular ruta</button>
              <button type="button" className="ghost" disabled={routeBusy || !originPoint} onClick={() => void calculateRoute()}>{routeBusy ? 'Calculando' : 'Recalcular ruta'}</button>
            </div>
            {routeInfo && <div className="route-summary"><div><span>Recorrido</span><strong>{routeInfo.kilometers.toFixed(2)} km</strong></div><div><span>Tiempo estimado</span><strong>{routeInfo.durationMinutes} min</strong></div><div><span>Transporte</span><strong>{money(routeInfo.cost, settings?.currencySymbol)}</strong></div><small>{deliveryCostPerKm.toFixed(2)} Bs por kilometro recorrido</small></div>}
          </div>
        </section>}

        {wizardStep === 1 && <section className="panel">
          <div className="section-title"><div><span className="step">02</span><h2>Filamentos y resinas</h2></div></div>
          <div className="inline-form consumable-picker">
            <label>Consumible<select value={selectedConsumable} onChange={e => setSelectedConsumable(Number(e.target.value))}>{consumables.map(x => <option key={x.id} value={x.id}>{x.name} - {x.material} - {x.color} - {weight(x.stockGrams ?? x.stockQuantity * 1000)} disp.</option>)}</select></label>
            <label>Gramos<input list="common-grams" type="number" min="0.01" step="0.01" value={grams} onChange={e => setGrams(Number(e.target.value))} /></label><datalist id="common-grams"><option value="50" /><option value="100" /><option value="150" /><option value="200" /><option value="250" /><option value="300" /><option value="500" /><option value="750" /><option value="1000" /></datalist>
            <button type="button" onClick={addConsumable}><Plus size={17} />Agregar</button>
          </div>
          {consumableLines.length === 0 ? <Empty>Agrega al menos un filamento o resina.</Empty> : <div className="line-list">{consumableLines.map((line, index) => {
            const item = consumableById.get(line.consumableId)
            return <div className="line-item" key={`${line.consumableId}-${index}`}><span className="color-dot" style={{ background: item?.color.toLowerCase() }} /><div><strong>{item?.name} - {item?.material}</strong><small>{item?.category} - {item?.color}</small></div><b>{number(line.grams)} g</b><button className="icon danger" aria-label="Quitar" onClick={() => { setConsumableLines(current => current.filter((_, i) => i !== index)); invalidate() }}><Trash2 size={16} /></button></div>
          })}</div>}
        </section>}

        {wizardStep === 2 && <section className="panel">
          <div className="section-title"><div><span className="step">03</span><h2>Materiales adicionales</h2></div></div>
          <div className="inline-form material-picker">
            <label>Material<select value={selectedMaterial} onChange={e => setSelectedMaterial(Number(e.target.value))}><option value={0}>Seleccionar</option>{materials.map(x => <option key={x.id} value={x.id}>{x.name} - {money(x.unitPrice, settings?.currencySymbol)}/{x.unit}</option>)}</select></label>
            <label>Cantidad<input type="number" min="0.01" step="0.01" value={materialQuantity} onChange={e => setMaterialQuantity(Number(e.target.value))} /></label>
            <button type="button" className="secondary" onClick={addMaterial}><Plus size={17} />Agregar</button>
          </div>
          {materialLines.length > 0 && <div className="line-list">{materialLines.map((line, index) => { const item = materialById.get(line.materialId); return <div className="line-item" key={`${line.materialId}-${index}`}><div><strong>{item?.name}</strong><small>{item?.category} - {item?.unit}</small></div><b>{number(line.quantity)}  {money(item?.unitPrice ?? 0, settings?.currencySymbol)}</b><button className="icon danger" aria-label="Quitar" onClick={() => { setMaterialLines(current => current.filter((_, i) => i !== index)); invalidate() }}><Trash2 size={16} /></button></div> })}</div>}
          <div className="form-grid two compact-fields">
            <label className="span-2">Notas<textarea rows={3} value={form.notes} onChange={e => update('notes', e.target.value)} placeholder="Acabados, entrega, observaciones" /></label>
          </div>
        </section>}

        {wizardStep === 3 && <section className="panel">
          <div className="section-title"><div><span className="step">04</span><h2>Ganancias</h2></div></div>
          <div className="form-grid two">
            <label>Margen de ganancia final (%)<input type="number" min={minProfitMargin} step="0.01" value={profitMarginPercent} onChange={e => updateProfitMargin(Number(e.target.value))} /></label>
            <label>Total de porcentajes aplicados<input type="number" value={liveProfitPercentTotal.toFixed(2)} disabled /></label>
          </div>
          <div className="margin-shortcuts" aria-label="Margenes rapidos"><span>Margen rapido</span>{[25, 35, 50].map(value => <button key={value} type="button" className={Math.round(profitMarginPercent) === value ? 'active' : ''} onClick={() => updateProfitMargin(value)}>{value}%</button>)}</div>
          <div className="form-grid three compact-fields"><label>Mantenimiento (%)<input type="number" min="0" step="0.01" value={form.maintenancePercent} onChange={e => update('maintenancePercent', Number(e.target.value))} /></label><label>Preparacin (%)<input type="number" min="0" step="0.01" value={form.preparationPercent} onChange={e => update('preparationPercent', Number(e.target.value))} /></label><label>Salarios (%)<input type="number" min="0" step="0.01" value={form.laborPercent} onChange={e => update('laborPercent', Number(e.target.value))} /></label><label>Merma/fallos (%)<input type="number" min="0" step="0.01" value={form.wastePercent} onChange={e => update('wastePercent', Number(e.target.value))} /></label><label>Administracin/energa extra (%)<input type="number" min="0" step="0.01" value={form.overheadPercent} onChange={e => update('overheadPercent', Number(e.target.value))} /></label><label>Empaque por pieza<input type="number" min="0" step="0.01" value={form.packagingCost} onChange={e => update('packagingCost', Number(e.target.value))} /></label><label>Transporte opcional<input type="number" min="0" step="0.01" value={form.transportCost} onChange={e => update('transportCost', Number(e.target.value))} /></label></div>
          <div className="profit-percent-live">
            <div><span>Base configurada</span><strong>{appliedConfigPercentTotal.toFixed(2)}%</strong></div>
            <div><span>Adicionales</span><strong>{profitPercentTotal.toFixed(2)}%</strong></div>
            <div><span>Por agregar</span><strong>{pendingProfitPercent.toFixed(2)}%</strong></div>
            <div><span>Total en vivo</span><strong>{liveProfitPercentTotal.toFixed(2)}%</strong></div>
            <div className="profit-percent-meter" aria-hidden="true"><span style={{ width: `${percentFill}%` }} /></div>
          </div>
          <div className="profit-costs">
            {profitCosts.map(line => <div className="profit-cost-row" key={line.id}><label>{line.name}<input type="number" min="0" max="100" step="0.01" value={line.percent} onChange={e => updateProfitCost(line.id, Number(e.target.value))} /></label><span>%</span>{!line.fixed && <button className="icon ghost danger-text" type="button" aria-label="Quitar" onClick={() => removeProfitCost(line.id)}><Trash2 size={16} /></button>}</div>)}
          </div>
          <div className="inline-form profit-cost-add">
            <label>Otro costo<input value={newProfitCostName} onChange={e => setNewProfitCostName(e.target.value)} placeholder="Ej. Transporte, comisiones, alquiler" /></label>
            <label>Porcentaje<input type="number" min="0" max="100" step="0.01" value={newProfitCostPercent} onChange={e => setNewProfitCostPercent(clampPercent(Number(e.target.value)))} /></label>
            <button type="button" className="secondary" onClick={addProfitCost}><Plus size={17} />Agregar costo</button>
          </div>
          <div className="formula-note"><strong>Costos adicionales por porcentaje: {money(profitAdditionalCost, settings?.currencySymbol)}</strong><span>La ganancia real se aplica despues de cubrir produccion y costos operativos.</span></div>
        </section>}

        <div className="wizard-actions">
          <button className="ghost" type="button" disabled={wizardStep === 0} onClick={() => setWizardStep(current => Math.max(0, current - 1))}><ArrowLeft size={16} />Atras</button>
          <span>Paso {wizardStep + 1} de {quoteWizardSteps.length}</span>
          {wizardStep < quoteWizardSteps.length - 1
            ? <button type="button" onClick={() => setWizardStep(current => Math.min(quoteWizardSteps.length - 1, current + 1))}>Siguiente<ArrowRight size={16} /></button>
            : <button className="secondary" type="button" disabled={busy || printers.length === 0 || consumables.length === 0} onClick={calculate}><Calculator size={17} />Calcular precio</button>}
        </div>
      </div>

      <aside className="quote-summary panel">
        <p className="eyebrow">RESULTADO</p><h2>Precio recomendado</h2>
        {!calculation ? <div className="summary-placeholder"><Calculator size={34} /><p>Completa los datos y calcula para ver el desglose.</p></div> : <>
          <div className="price-hero"><small>Total sugerido</small><strong>{money(customPrice > 0 ? customPrice : calculation.recommendedPrice, settings?.currencySymbol)}</strong><span>{number(calculation.totalWeight)} g totales</span><label>Editar precio final<input type="number" min="0.01" step="0.01" value={customPrice || calculation.recommendedPrice} onChange={e => setCustomPrice(Math.max(0, Number(e.target.value)))} /></label></div>
          <dl className="breakdown">
            <div><dt>Consumibles</dt><dd>{money(calculation.materialCost, settings?.currencySymbol)}</dd></div>
            <div><dt>Electricidad</dt><dd>{money(calculation.electricityCost, settings?.currencySymbol)}</dd></div>
            <div><dt>Mantenimiento</dt><dd>{money(calculation.maintenanceCost, settings?.currencySymbol)}</dd></div>
            <div><dt>Adicionales</dt><dd>{money(calculation.additionalCost, settings?.currencySymbol)}</dd></div>
            <div><dt>Costos porcentuales</dt><dd>{money(profitAdditionalCost, settings?.currencySymbol)}</dd></div>
            <div className="subtotal"><dt>Costo total</dt><dd>{money(calculation.subtotal, settings?.currencySymbol)}</dd></div>
            <div><dt>Ganancia real ({profitMarginPercent.toFixed(2)}%)</dt><dd>{money(calculation.profitAmount, settings?.currencySymbol)}</dd></div>
            <div><dt>Impuesto</dt><dd>{money(calculation.taxAmount, settings?.currencySymbol)}</dd></div>
          </dl>
        </>}
        {saved && <div className="saved-ticket"><CheckCircle2 size={19} /><div><small>Codigo guardado</small><strong>{saved.orderCode}</strong></div><StatusSale sold={sold} /><button className="icon ghost" title="Descargar comprobante PDF" onClick={() => api.downloadVoucher(saved.id).catch(reason => setError((reason as Error).message))}><Download size={16} /></button></div>}
        <div className="summary-actions">
          <button className="secondary" disabled={busy || printers.length === 0 || consumables.length === 0} onClick={calculate}><Calculator size={17} />Calcular precio</button>
          <button disabled={busy || printers.length === 0 || consumables.length === 0} onClick={save}><Save size={17} />Guardar cotizacion</button>
          {saved && !sold && <button className="sale-button" disabled={busy} onClick={confirmSale}><ShoppingBag size={17} />Confirmar venta</button>}
          <button className="ghost" disabled={busy} onClick={clear}><RotateCcw size={16} />Limpiar</button>
        </div>
      </aside>
    </div>
  </>
}

function StatusSale({ sold }: { sold: boolean }) { return <span className={`status ${sold ? 'ok' : 'warning'}`}>{sold ? 'VENDIDA' : 'PENDIENTE'}</span> }
