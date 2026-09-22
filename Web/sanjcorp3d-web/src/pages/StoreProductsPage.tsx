import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Archive, Edit3, PackagePlus, Plus, X } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Consumable, ConsumableMaterialType, ProductCatalog } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

const blank: ProductCatalog = { id: 0, name: '', description: '', materialType: '', filamentGrams: 0, materialCost: 0, productionMinutes: 0, profitMultiplier: 3, active: true }

export function StoreProductsPage({ canManage }: { canManage: boolean }) {
  const [items, setItems] = useState<ProductCatalog[]>([])
  const [materialTypes, setMaterialTypes] = useState<ConsumableMaterialType[]>([])
  const [consumables, setConsumables] = useState<Consumable[]>([])
  const [settings, setSettings] = useState<BusinessSettings>()
  const [draft, setDraft] = useState<ProductCatalog | null>(null)
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

  function unitProductionCost(item: ProductCatalog) {
    const materialPrice = priceByMaterial.get(item.materialType) ?? 0
    const calculatedMaterial = item.filamentGrams > 0 && materialPrice > 0 ? item.filamentGrams / 1000 * materialPrice : item.materialCost
    return calculatedMaterial
  }
  function finalUnitPrice(item: ProductCatalog) { return unitProductionCost(item) * Math.max(1, item.profitMultiplier) }

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
    const next = item ? { ...item } : { ...blank, materialType: materialOptions[0] ?? '', profitMultiplier: settings?.defaultProfitMultiplier ?? 3 }
    setDraft(next); setError('')
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!draft) return
    if (!draft.name.trim()) { setError('El nombre del producto es obligatorio.'); return }
    if (!draft.materialType.trim()) { setError('Selecciona el tipo de material.'); return }
    if (draft.filamentGrams <= 0) { setError('Indica los gramos de filamento por unidad.'); return }
    if (draft.productionMinutes <= 0) { setError('Indica el tiempo de producción por unidad.'); return }
    if (draft.profitMultiplier < 1) { setError('El multiplicador debe ser igual o mayor que 1.'); return }
    setBusy(true); setError(''); setSuccess('')
    try { await api.saveStoreProduct({ ...draft, materialCost: unitProductionCost(draft) }); setSuccess('Producto de tienda guardado.'); setDraft(null); await load() }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  async function archive(item: ProductCatalog) {
    if (!await confirmDialog({ title: 'Archivar producto', message: 'El producto dejará de estar disponible para nuevas cotizaciones.', highlight: item.name, confirmLabel: 'Sí, archivar', variant: 'danger' })) return
    setBusy(true); setError('')
    try { await api.archiveProduct(item.id); setSuccess('Producto archivado.'); await load() }
    catch (reason) { setError((reason as Error).message) }
    finally { setBusy(false) }
  }

  return <>
    <PageHeader eyebrow="TIENDA" title="Productos de tienda" description="Administra productos establecidos con material, filamento requerido, tiempo y ganancia por unidad." actions={canManage ? <button onClick={() => edit()}><Plus size={17} />Nuevo producto</button> : undefined} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <div className={`catalog-layout ${draft ? 'with-editor' : ''}`}>
      <section className="panel">
        {loading ? <Loading /> : items.length === 0 ? <Empty>No hay productos de tienda registrados.</Empty> : <div className="card-grid">{items.map(item => <article className={`catalog-card ${item.active ? '' : 'archived'}`} key={item.id}>
          <div className="catalog-icon"><PackagePlus size={20} /></div>
          <div className="catalog-main"><div><h3>{item.name}</h3>{!item.active && <span className="status warning">ARCHIVADO</span>}</div><p>{item.description || 'Sin descripción'}</p><small>{item.materialType || 'Sin material'} · {weight(item.filamentGrams)} por unidad · {number(item.productionMinutes)} min</small><small>Costo producción {money(unitProductionCost(item), settings?.currencySymbol)} · Precio sugerido {money(finalUnitPrice(item), settings?.currencySymbol)}</small></div>
          {canManage && <div className="card-actions"><button className="ghost small" onClick={() => edit(item)}><Edit3 size={15} />Editar</button>{item.active && <button className="ghost small danger-text" disabled={busy} onClick={() => archive(item)}><Archive size={15} />Archivar</button>}</div>}
        </article>)}</div>}
      </section>
      {draft && <form className="panel editor" onSubmit={save}>
        <div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR PRODUCTO' : 'NUEVO PRODUCTO'}</p><h2>{draft.id ? draft.name : 'Producto de tienda'}</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}><X size={18} /></button></div>
        <label>Nombre<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Descripción<textarea rows={3} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
        <div className="form-grid two">
          <label>Tipo de material<select required value={draft.materialType} onChange={e => setDraft({ ...draft, materialType: e.target.value })}><option value="">Seleccionar material</option>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label>
          <label>Filamento por unidad (g)<input type="number" min="0.01" step="0.01" value={draft.filamentGrams} onChange={e => setDraft({ ...draft, filamentGrams: Number(e.target.value) })} /></label>
          <label>Tiempo por unidad (min)<input type="number" min="0.01" step="0.01" value={draft.productionMinutes} onChange={e => setDraft({ ...draft, productionMinutes: Number(e.target.value) })} /></label>
          <label>Multiplicador de ganancia<input type="number" min="1" step="0.01" value={draft.profitMultiplier} onChange={e => setDraft({ ...draft, profitMultiplier: Number(e.target.value) })} /></label>
        </div>
        <div className="breakdown"><div><dt>Costo de producción estimado</dt><dd>{money(unitProductionCost(draft), settings?.currencySymbol)}</dd></div><div><dt>Precio sugerido por unidad</dt><dd>{money(finalUnitPrice(draft), settings?.currencySymbol)}</dd></div></div>
        <p className="field-help">El costo usa el precio promedio por kg de los filamentos activos del tipo seleccionado. Si no hay precio registrado, conserva el costo anterior guardado.</p>
        <div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button disabled={busy}>{busy ? 'Guardando...' : 'Guardar producto'}</button></div>
      </form>}
    </div>
  </>
}


