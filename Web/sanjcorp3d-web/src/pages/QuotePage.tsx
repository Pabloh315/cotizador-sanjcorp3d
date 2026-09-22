import { useEffect, useMemo, useState } from 'react'
import { Calculator, CheckCircle2, Download, Plus, RotateCcw, Save, Search, ShoppingBag, Trash2, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Consumable, ConsumableUsage, ExtraMaterial, MaterialUsage, Printer, ProductCatalog, QuoteCalculation, QuoteRequest, QuoteSummary } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

type BaseForm = { customer: string; customerPhone: string; projectName: string; printerId: number; hours: number; minutes: number; quantity: number; additionalManualCost: number; workExtraPercent: number; profitMultiplier: number; notes: string }
const emptyForm: BaseForm = { customer: '', customerPhone: '', projectName: '', printerId: 0, hours: 0, minutes: 0, quantity: 1, additionalManualCost: 0, workExtraPercent: 0, profitMultiplier: 3, notes: '' }
type ProfitCostLine = { id: string; name: string; percent: number; fixed?: boolean }
const defaultProfitCosts: ProfitCostLine[] = [
  { id: 'salary', name: 'Costos de salarios', percent: 0, fixed: true },
  { id: 'debt', name: 'Costos de inversi?n o deudas', percent: 0, fixed: true },
  { id: 'manual', name: 'Costo manual', percent: 0, fixed: true },
  { id: 'work', name: 'Adicional del trabajo', percent: 0, fixed: true },
]
const clampPercent = (value: number) => Math.min(100, Math.max(0, Number.isFinite(value) ? Number(value.toFixed(2)) : 0))

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

  useEffect(() => {
    Promise.all([api.printers(), api.consumables(), api.materials(), api.settings(), api.products()])
      .then(([printerItems, consumableItems, materialItems, configuration, productItems]) => {
        const availablePrinters = printerItems.filter(x => x.active)
        setPrinters(availablePrinters); setConsumables(consumableItems); setMaterials(materialItems); setSettings(configuration); setProducts(productItems)
        setForm(current => ({ ...current, printerId: availablePrinters[0]?.id ?? 0, profitMultiplier: configuration.defaultProfitMultiplier }))
        setSelectedConsumable(consumableItems.find(x => x.isDefault)?.id ?? consumableItems[0]?.id ?? 0)
        setSelectedMaterial(materialItems[0]?.id ?? 0)
      })
      .catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
  }, [])

  const consumableById = useMemo(() => new Map(consumables.map(x => [x.id, x])), [consumables])
  const materialById = useMemo(() => new Map(materials.map(x => [x.id, x])), [materials])
  const selectedPrinter = useMemo(() => printers.find(x => x.id === form.printerId), [printers, form.printerId])
  const profitPercentTotal = useMemo(() => profitCosts.reduce((sum, line) => sum + clampPercent(line.percent), 0), [profitCosts])
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

  function invalidate() { setCalculation(undefined); setSaved(undefined); setSold(false); setSuccess('') }
  function update<K extends keyof BaseForm>(key: K, value: BaseForm[K]) { setForm(current => ({ ...current, [key]: value })); invalidate() }
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
    if ((item.stockGrams ?? item.stockQuantity * 1000) <= 0) { setError(`No hay existencia de ${item.name} · ${item.material} · ${item.color}. Actualiza su inventario primero.`); return }
    setConsumableLines(current => [...current, { consumableId: item.id, grams }]); setGrams(0); setError(''); invalidate()
  }

  function addMaterial() {
    if (!materialById.has(selectedMaterial) || materialQuantity <= 0) { setError('Selecciona un material e indica una cantidad mayor que cero.'); return }
    setMaterialLines(current => [...current, { materialId: selectedMaterial, quantity: materialQuantity }]); setMaterialQuantity(1); setError(''); invalidate()
  }

  function request(): QuoteRequest {
    return { customer: form.customer, customerPhone: form.customerPhone, projectName: form.projectName, productName: selectedProduct, printerId: form.printerId, printHours: form.hours + form.minutes / 60, quantity: form.quantity, additionalManualCost: profitAdditionalCost, profitMultiplier: form.profitMultiplier, notes: form.notes, consumables: consumableLines, materials: materialLines }
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
      setSaved({ ...result, recommendedPrice: finalPrice }); setCalculation(totals); setSuccess(`Cotización ${result.orderCode} guardada correctamente.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function confirmSale() {
    if (!saved) return
    if (!await confirmDialog({ title: 'Confirmar venta', message: 'Se registrará la venta y se descontará el filamento del inventario.', highlight: `${saved.orderCode} · ${money(saved.recommendedPrice, settings?.currencySymbol)}`, confirmLabel: 'Sí, confirmar', variant: 'success' })) return
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
      setForm({ customer: source.customer, customerPhone: source.customerPhone ?? '', projectName: source.projectName, printerId: printer?.id ?? printers[0]?.id ?? 0, hours: Math.floor(totalMinutes / 60), minutes: totalMinutes % 60, quantity: source.quantity, additionalManualCost: 0, workExtraPercent: 0, profitMultiplier: source.profitMultiplier, notes: source.notes })
      setSelectedProduct(source.productName ?? '')
      setConsumableLines(source.consumables.filter(line => consumableById.has(line.legacyConsumableId)).map(line => ({ consumableId: line.legacyConsumableId, grams: line.grams })))
      setMaterialLines(source.materials.filter(line => materialById.has(line.legacyMaterialId)).map(line => ({ materialId: line.legacyMaterialId, quantity: line.quantity })))
      setCalculation({ totalWeight: source.totalWeight, materialCost: source.materialCost, electricityCost: source.electricityCost, maintenanceCost: source.maintenanceCost, additionalCost: source.additionalCost, subtotal: source.subtotal, profitAmount: source.profitAmount, taxAmount: source.taxAmount, recommendedPrice: source.recommendedPrice })
      setCustomPrice(source.recommendedPrice); setSaved(undefined); setSold(false); setSearchOpen(false); setProfitCosts(defaultProfitCosts)
      setSuccess(`Datos de ${source.orderCode} cargados como una nueva cotización. Puedes modificarlos antes de guardar.`)
    } catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  function clear() {
    setForm({ ...emptyForm, printerId: printers[0]?.id ?? 0, profitMultiplier: settings?.defaultProfitMultiplier ?? 3 })
    setProfitCosts(defaultProfitCosts); setNewProfitCostName(''); setNewProfitCostPercent(0)
    setConsumableLines([]); setMaterialLines([]); setCalculation(undefined); setSaved(undefined); setSold(false); setSelectedProduct(''); setCustomPrice(0); setError(''); setSuccess('')
  }

  if (loading) return <Loading label="Cargando catálogos…" />
  if (!canWrite) return <><PageHeader eyebrow="COTIZADOR" title="Nueva cotización" description="Cálculo de costos y precio recomendado." /><div className="panel locked-panel"><h2>Acceso de consulta</h2><p>Tu rol puede revisar el historial, pero no crear cotizaciones. Solicita al administrador el rol Ventas si necesitas esta función.</p></div></>

  return <>
    <PageHeader eyebrow="COTIZADOR 3D" title="Nueva cotización" description="Registra los datos de impresión, combina consumibles y obtén el precio con la fórmula original." actions={<button className="secondary" onClick={() => { setSearchOpen(value => !value); if (!searchOpen && searchResults.length === 0) void searchExisting() }}><Search size={17} />Buscar cotización o venta</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    {searchOpen && <section className="panel quote-search-panel"><div className="section-title"><div><p className="eyebrow">REUTILIZAR DATOS</p><h2>Buscar cotizaciones y ventas</h2></div><button className="icon ghost" onClick={() => setSearchOpen(false)}><X size={18} /></button></div><div className="inline-form"><label className="search-field"><Search size={17} /><input value={searchText} onChange={e => setSearchText(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); void searchExisting() } }} placeholder="Código, cliente o producto…" /></label><button onClick={searchExisting} disabled={searching}>{searching ? 'Buscando…' : 'Buscar'}</button></div>{searching ? <Loading label="Buscando…" /> : searchResults.length === 0 ? <Empty>No se encontraron registros.</Empty> : <div className="line-list quote-search-results">{searchResults.map(item => <div className="line-item" key={item.id}><div><strong>{item.customer} · {item.productName || item.projectName}</strong><small>{item.orderCode} · {item.soldAtUtc ? 'Venta confirmada' : 'Cotización pendiente'} · {money(item.recommendedPrice, settings?.currencySymbol)}</small></div><button onClick={() => loadExisting(item.id)}>Cargar como nueva</button></div>)}</div>}</section>}
    {printers.length === 0 ? <div className="alert error">Registra al menos una impresora disponible antes de cotizar.</div> : null}
    {consumables.length === 0 ? <div className="alert error">Necesitas al menos un consumible activo antes de cotizar.</div> : null}
    <div className="quote-layout">
      <div className="form-stack">
        <section className="panel">
          <div className="section-title"><div><span className="step">01</span><h2>Proyecto e impresión</h2></div></div>
          <div className="form-grid two">
            <label>Cliente<input value={form.customer} onChange={e => update('customer', e.target.value)} placeholder="Nombre del cliente" /></label>
            <label>Celular del cliente <small>(solo al guardar)</small><input value={form.customerPhone} onChange={e => update('customerPhone', e.target.value)} placeholder="70000000" inputMode="tel" /></label>
            <label>Pieza o proyecto<input value={form.projectName} onChange={e => update('projectName', e.target.value)} placeholder="Ej. Soporte personalizado" /></label>
            <label>Producto a la venta<select value={selectedProduct} onChange={e => { setSelectedProduct(e.target.value); invalidate() }}><option value="">Producto personalizado…</option>{products.map(item => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>
            <label className="span-2">Impresora<select value={form.printerId} onChange={e => update('printerId', Number(e.target.value))}>{printers.map(x => <option key={x.id} value={x.id}>{x.name} · {x.status} · {x.buildX}x{x.buildY}x{x.buildZ} mm · {x.colorCount ?? 1} color{(x.colorCount ?? 1) === 1 ? '' : 'es'}</option>)}</select></label>
            <label>Horas<input type="number" min="0" step="1" value={form.hours} onChange={e => update('hours', Number(e.target.value))} /></label>
            <label>Minutos<input type="number" min="0" max="59" step="1" value={form.minutes} onChange={e => update('minutes', Number(e.target.value))} /></label>
            <label>Cantidad de piezas<input type="number" min="1" step="1" value={form.quantity} onChange={e => update('quantity', Number(e.target.value))} /></label>
          </div>
        </section>

        <section className="panel">
          <div className="section-title"><div><span className="step">02</span><h2>Filamentos y resinas</h2></div></div>
          <div className="inline-form consumable-picker">
            <label>Consumible<select value={selectedConsumable} onChange={e => setSelectedConsumable(Number(e.target.value))}>{consumables.map(x => <option key={x.id} value={x.id}>{x.name} · {x.material} · {x.color} · {weight(x.stockGrams ?? x.stockQuantity * 1000)} disp.</option>)}</select></label>
            <label>Gramos<input list="common-grams" type="number" min="0.01" step="0.01" value={grams} onChange={e => setGrams(Number(e.target.value))} /></label><datalist id="common-grams"><option value="50" /><option value="100" /><option value="150" /><option value="200" /><option value="250" /><option value="300" /><option value="500" /><option value="750" /><option value="1000" /></datalist>
            <button type="button" onClick={addConsumable}><Plus size={17} />Agregar</button>
          </div>
          {consumableLines.length === 0 ? <Empty>Agrega al menos un filamento o resina.</Empty> : <div className="line-list">{consumableLines.map((line, index) => {
            const item = consumableById.get(line.consumableId)
            return <div className="line-item" key={`${line.consumableId}-${index}`}><span className="color-dot" style={{ background: item?.color.toLowerCase() }} /><div><strong>{item?.name} · {item?.material}</strong><small>{item?.category} · {item?.color}</small></div><b>{number(line.grams)} g</b><button className="icon danger" aria-label="Quitar" onClick={() => { setConsumableLines(current => current.filter((_, i) => i !== index)); invalidate() }}><Trash2 size={16} /></button></div>
          })}</div>}
        </section>

        <section className="panel">
          <div className="section-title"><div><span className="step">03</span><h2>Materiales adicionales</h2></div></div>
          <div className="inline-form material-picker">
            <label>Material<select value={selectedMaterial} onChange={e => setSelectedMaterial(Number(e.target.value))}><option value={0}>Seleccionar…</option>{materials.map(x => <option key={x.id} value={x.id}>{x.name} · {money(x.unitPrice, settings?.currencySymbol)}/{x.unit}</option>)}</select></label>
            <label>Cantidad<input type="number" min="0.01" step="0.01" value={materialQuantity} onChange={e => setMaterialQuantity(Number(e.target.value))} /></label>
            <button type="button" className="secondary" onClick={addMaterial}><Plus size={17} />Agregar</button>
          </div>
          {materialLines.length > 0 && <div className="line-list">{materialLines.map((line, index) => { const item = materialById.get(line.materialId); return <div className="line-item" key={`${line.materialId}-${index}`}><div><strong>{item?.name}</strong><small>{item?.category} · {item?.unit}</small></div><b>{number(line.quantity)} × {money(item?.unitPrice ?? 0, settings?.currencySymbol)}</b><button className="icon danger" aria-label="Quitar" onClick={() => { setMaterialLines(current => current.filter((_, i) => i !== index)); invalidate() }}><Trash2 size={16} /></button></div> })}</div>}
          <div className="form-grid two compact-fields">
            <label className="span-2">Notas<textarea rows={3} value={form.notes} onChange={e => update('notes', e.target.value)} placeholder="Acabados, entrega, observaciones…" /></label>
          </div>
        </section>

        <section className="panel">
          <div className="section-title"><div><span className="step">04</span><h2>Ganancias</h2></div></div>
          <div className="form-grid two">
            <label>Multiplicador de ganancia<input type="number" min="1" step="0.01" value={form.profitMultiplier} onChange={e => update('profitMultiplier', Number(e.target.value))} /></label>
            <label>Total de costos porcentuales<input type="number" value={profitPercentTotal.toFixed(2)} disabled /></label>
          </div>
          <div className="profit-costs">
            {profitCosts.map(line => <div className="profit-cost-row" key={line.id}><label>{line.name}<input type="number" min="0" max="100" step="0.01" value={line.percent} onChange={e => updateProfitCost(line.id, Number(e.target.value))} /></label><span>%</span>{!line.fixed && <button className="icon ghost danger-text" type="button" aria-label="Quitar" onClick={() => removeProfitCost(line.id)}><Trash2 size={16} /></button>}</div>)}
          </div>
          <div className="inline-form profit-cost-add">
            <label>Otro costo<input value={newProfitCostName} onChange={e => setNewProfitCostName(e.target.value)} placeholder="Ej. Transporte, comisiones, alquiler" /></label>
            <label>Porcentaje<input type="number" min="0" max="100" step="0.01" value={newProfitCostPercent} onChange={e => setNewProfitCostPercent(clampPercent(Number(e.target.value)))} /></label>
            <button type="button" className="secondary" onClick={addProfitCost}><Plus size={17} />Agregar costo</button>
          </div>
          <div className="formula-note"><strong>Costos porcentuales estimados: {money(profitAdditionalCost, settings?.currencySymbol)}</strong><span>Se calculan sobre el costo base estimado antes del multiplicador de ganancia.</span></div>
        </section>
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
            <div><dt>Ganancia</dt><dd>{money(calculation.profitAmount, settings?.currencySymbol)}</dd></div>
            <div><dt>Impuesto</dt><dd>{money(calculation.taxAmount, settings?.currencySymbol)}</dd></div>
          </dl>
        </>}
        {saved && <div className="saved-ticket"><CheckCircle2 size={19} /><div><small>Código guardado</small><strong>{saved.orderCode}</strong></div><StatusSale sold={sold} /><button className="icon ghost" title="Descargar comprobante PDF" onClick={() => api.downloadVoucher(saved.id).catch(reason => setError((reason as Error).message))}><Download size={16} /></button></div>}
        <div className="summary-actions">
          <button className="secondary" disabled={busy || printers.length === 0 || consumables.length === 0} onClick={calculate}><Calculator size={17} />Calcular precio</button>
          <button disabled={busy || printers.length === 0 || consumables.length === 0} onClick={save}><Save size={17} />Guardar cotización</button>
          {saved && !sold && <button className="sale-button" disabled={busy} onClick={confirmSale}><ShoppingBag size={17} />Confirmar venta</button>}
          <button className="ghost" disabled={busy} onClick={clear}><RotateCcw size={16} />Limpiar</button>
        </div>
      </aside>
    </div>
  </>
}

function StatusSale({ sold }: { sold: boolean }) { return <span className={`status ${sold ? 'ok' : 'warning'}`}>{sold ? 'VENDIDA' : 'PENDIENTE'}</span> }





