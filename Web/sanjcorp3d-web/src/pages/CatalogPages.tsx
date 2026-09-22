import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { Archive, Boxes, Edit3, Filter, Plus, Printer as PrinterIcon, RefreshCw, Star, Trash2, Upload, X } from 'lucide-react'
import { api } from '../api'
import type { Consumable, ConsumableMaterialType, ExtraMaterial, ExtraMaterialCategory, Printer } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, Status, SuccessMessage, money, number, weight } from '../ui'
import { confirmDialog } from '../confirm'

const blankPrinter: Printer = { id: 0, name: '', buildX: 220, buildY: 220, buildZ: 250, nozzle: 0.4, speed: 60, powerWatts: 350, hourlyCost: 0, colorCount: 1, status: 'Disponible', isDefault: false, active: true }
const printerStatuses = ['Disponible', 'En uso', 'En mantenimiento', 'Fuera de uso']
const blankConsumable: Consumable = { id: 0, name: '', category: 'Filamento', material: 'PLA', color: '', pricePerUnit: 0, density: 1.24, isDefault: false, active: true, stockQuantity: 0, stockGrams: 0, lowStockGrams: 1000 }
const blankMaterial: ExtraMaterial = { id: 0, name: '', category: 'Acabado', unit: 'unidad', unitPrice: 0, active: true }

type MaterialTypeDraft = { id?: number; name: string }

export function PrintersPage({ canManage = false, canFavorite = false }: { canManage?: boolean; canFavorite?: boolean }) {
  const [items, setItems] = useState<Printer[]>([])
  const [includeArchived, setIncludeArchived] = useState(false)
  const [draft, setDraft] = useState<Printer | null>(null)
  const [error, setError] = useState(''); const [success, setSuccess] = useState(''); const [loading, setLoading] = useState(true)
  const load = useCallback(() => { setLoading(true); api.printers(includeArchived).then(setItems).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false)) }, [includeArchived])
  useEffect(load, [load])

  async function save(event: FormEvent) {
    event.preventDefault(); if (!draft) return; setError(''); setSuccess('')
    try { await api.savePrinter({ ...draft, colorCount: Math.max(1, Number(draft.colorCount) || 1) }); setSuccess(draft.id ? 'Impresora actualizada.' : 'Impresora agregada.'); setDraft(null); load() }
    catch (reason) { setError((reason as Error).message) }
  }
  async function archive(item: Printer) { if (!await confirmDialog({ title: 'Archivar impresora', message: 'La impresora dejará de estar disponible, pero el historial se conservará.', highlight: item.name, confirmLabel: 'Sí, archivar', variant: 'danger' })) return; try { await api.archivePrinter(item.id); setDraft(null); load() } catch (reason) { setError((reason as Error).message) } }
  async function favorite(item: Printer) { setError(''); try { await api.favoritePrinter(item.id, !item.isDefault); setItems(current => current.map(x => x.id === item.id ? { ...x, isDefault: !item.isDefault } : x)); setSuccess(item.isDefault ? 'Impresora retirada de favoritas.' : 'Impresora agregada a favoritas.') } catch (reason) { setError((reason as Error).message) } }

  return <>
    <PageHeader eyebrow="CATALOGO GLOBAL" title="Impresoras" description={canManage ? 'Catalogo compartido con todas las cuentas.' : 'Elige las impresoras favoritas que usara tu espacio.'} actions={canManage && <button onClick={() => setDraft({ ...blankPrinter })}><Plus size={17} />Nueva impresora</button>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <div className={`catalog-layout ${draft ? 'with-editor' : ''}`}>
      <section className="panel">
        <div className="toolbar"><label className="check"><input type="checkbox" checked={includeArchived} onChange={e => setIncludeArchived(e.target.checked)} />Mostrar archivadas</label><button className="icon ghost" onClick={load} aria-label="Actualizar"><RefreshCw size={16} /></button></div>
        {loading ? <Loading /> : items.length === 0 ? <Empty>No hay impresoras registradas.</Empty> : <div className="card-grid">{items.map(item => <article className={`catalog-card ${!item.active ? 'archived' : ''}`} key={item.id}>
          <div className="catalog-icon"><PrinterIcon size={22} /></div><div className="catalog-main"><div><h3>{item.name}</h3>{item.isDefault && <span className="preferred">FAVORITA</span>}<span className={`machine-status ${item.status.toLowerCase().replaceAll(' ', '-')}`}>{item.status}</span></div><p>{number(item.buildX, 0)} x {number(item.buildY, 0)} x {number(item.buildZ, 0)} mm</p><small>Boquilla {number(item.nozzle)} mm · {number(item.speed)} mm/s · {number(item.powerWatts, 0)} W · {item.colorCount ?? 1} color{(item.colorCount ?? 1) === 1 ? '' : 'es'}</small></div><Status active={item.active} />
          <div className="card-actions">{canFavorite && item.active && <button className={`ghost small ${item.isDefault ? 'favorite-active' : ''}`} disabled={item.status !== 'Disponible' && !item.isDefault} title={item.status === 'Disponible' || item.isDefault ? 'Elegir favorita' : 'Solo las máquinas disponibles pueden ser favoritas'} onClick={() => favorite(item)}><Star size={15} fill={item.isDefault ? 'currentColor' : 'none'} />{item.isDefault ? 'Favorita' : 'Elegir'}</button>}{canManage && <><button className="ghost small" onClick={() => setDraft({ ...item, colorCount: item.colorCount ?? 1 })}><Edit3 size={15} />Editar</button>{item.active && <button className="ghost small danger-text" onClick={() => archive(item)}><Archive size={15} />Archivar</button>}</>}</div>
        </article>)}</div>}
      </section>
      {draft && <form className="panel editor" onSubmit={save}>
        <div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR' : 'NUEVO EQUIPO'}</p><h2>{draft.id ? draft.name : 'Impresora'}</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}>x</button></div>
        <label>Nombre<input required maxLength={100} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Estado<select value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value })}>{printerStatuses.map(status => <option key={status} value={status}>{status}</option>)}</select></label>
        <div className="form-grid three"><label>Ancho X (mm)<input type="number" min="1" step="0.1" value={draft.buildX} onChange={e => setDraft({ ...draft, buildX: Number(e.target.value) })} /></label><label>Fondo Y (mm)<input type="number" min="1" step="0.1" value={draft.buildY} onChange={e => setDraft({ ...draft, buildY: Number(e.target.value) })} /></label><label>Alto Z (mm)<input type="number" min="1" step="0.1" value={draft.buildZ} onChange={e => setDraft({ ...draft, buildZ: Number(e.target.value) })} /></label></div>
        <div className="form-grid two"><label>Boquilla (mm)<input type="number" min="0.1" step="0.1" value={draft.nozzle} onChange={e => setDraft({ ...draft, nozzle: Number(e.target.value) })} /></label><label>Velocidad (mm/s)<input type="number" min="1" step="1" value={draft.speed} onChange={e => setDraft({ ...draft, speed: Number(e.target.value) })} /></label><label>Potencia (W)<input type="number" min="0" step="1" value={draft.powerWatts} onChange={e => setDraft({ ...draft, powerWatts: Number(e.target.value) })} /></label><label>Costo/hora referencial<input type="number" min="0" step="0.01" value={draft.hourlyCost} onChange={e => setDraft({ ...draft, hourlyCost: Number(e.target.value) })} /></label><label>Cantidad de colores<input type="number" min="1" step="1" value={draft.colorCount ?? 1} onChange={e => setDraft({ ...draft, colorCount: Math.max(1, Number(e.target.value) || 1) })} /></label></div>
        <p className="field-help">Las impresoras favoritas se eligen desde cada espacio de trabajo.</p>
        {draft.id > 0 && <label className="check"><input type="checkbox" checked={draft.active} onChange={e => setDraft({ ...draft, active: e.target.checked })} />Activa</label>}
        <div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button>Guardar</button></div>
      </form>}
    </div>
  </>
}

export function ConsumablesPage({ canEdit, currency = 'Bs' }: { canEdit: boolean; currency?: string }) {
  const [items, setItems] = useState<Consumable[]>([])
  const [materialTypes, setMaterialTypes] = useState<ConsumableMaterialType[]>([])
  const [includeArchived, setIncludeArchived] = useState(false)
  const [category, setCategory] = useState('Todos')
  const [filterOpen, setFilterOpen] = useState(false)
  const [filterBrand, setFilterBrand] = useState('')
  const [filterMaterial, setFilterMaterial] = useState('')
  const [draft, setDraft] = useState<Consumable | null>(null)
  const [materialTypeDraft, setMaterialTypeDraft] = useState<MaterialTypeDraft | null>(null)
  const [stockKg, setStockKg] = useState(0)
  const [stockRemainder, setStockRemainder] = useState(0)
  const [entry, setEntry] = useState<Consumable | null>(null)
  const [entryKg, setEntryKg] = useState(0)
  const [entryGrams, setEntryGrams] = useState(0)
  const [entryPrice, setEntryPrice] = useState(0)
  const [loss, setLoss] = useState<Consumable | null>(null); const [lossGrams, setLossGrams] = useState(0); const [lossReason, setLossReason] = useState('Producto fallido')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(true)

  const load = useCallback(() => {
    setLoading(true)
    Promise.all([api.consumables(includeArchived), api.materialTypes(includeArchived)])
      .then(([consumableItems, typeItems]) => { setItems(consumableItems); setMaterialTypes(typeItems); setError('') })
      .catch(reason => setError((reason as Error).message))
      .finally(() => setLoading(false))
  }, [includeArchived])
  useEffect(load, [load])

  const brands = useMemo(() => [...new Set(items.map(item => item.name).filter(Boolean))].sort((a, b) => a.localeCompare(b)), [items])
  const materialOptions = useMemo(() => [...new Set(['PLA', 'PETG', 'ABS', 'ASA', 'TPU', 'Resina', ...materialTypes.filter(x => x.active).map(x => x.name), ...items.map(item => item.material).filter(Boolean)])].sort((a, b) => a.localeCompare(b)), [items, materialTypes])
  const materialsByType = useMemo(() => materialOptions.filter(value => items.some(item => (category === 'Todos' || item.category.toLowerCase() === category.toLowerCase()) && item.material === value) || materialTypes.some(type => type.name === value)), [items, category, materialOptions, materialTypes])
  const visible = useMemo(() => items.filter(item => (category === 'Todos' || item.category.toLowerCase() === category.toLowerCase()) && (!filterBrand || item.name === filterBrand) && (!filterMaterial || item.material === filterMaterial)), [items, category, filterBrand, filterMaterial])

  function currentGrams(item: Consumable) { return Number(item.stockGrams ?? (item.stockQuantity ?? 0) * 1000) }
  function edit(item: Consumable) { const total = Math.max(0, currentGrams(item)); setDraft({ ...item, stockGrams: total, lowStockGrams: item.lowStockGrams ?? 1000 }); setStockKg(Math.floor(total / 1000)); setStockRemainder(Number((total % 1000).toFixed(2))); setError('') }
  function receive(item: Consumable) { setEntry(item); setEntryKg(0); setEntryGrams(0); setEntryPrice(item.pricePerUnit); setError('') }
  function reportLoss(item: Consumable) { setLoss(item); setLossGrams(0); setLossReason('Producto fallido'); setError('') }

  async function saveMaterialType(event: FormEvent) {
    event.preventDefault(); if (!materialTypeDraft) return; setError(''); setSuccess('')
    try {
      if (materialTypeDraft.id) await api.updateMaterialType(materialTypeDraft.id, materialTypeDraft.name)
      else await api.createMaterialType(materialTypeDraft.name)
      setSuccess(materialTypeDraft.id ? 'Tipo de material actualizado.' : 'Tipo de material agregado.')
      setMaterialTypeDraft(null); load()
    } catch (reason) { setError((reason as Error).message) }
  }
  async function archiveMaterialType(item: ConsumableMaterialType) { if (!await confirmDialog({ title: 'Archivar tipo de material', message: 'Este tipo dejará de aparecer como opción activa.', highlight: item.name, confirmLabel: 'Sí, archivar', variant: 'danger' })) return; try { await api.archiveMaterialType(item.id); setMaterialTypeDraft(null); load() } catch (reason) { setError((reason as Error).message) } }
  async function saveEntry(event: FormEvent) { event.preventDefault(); if (!entry || entryPrice < 0 || entryKg * 1000 + entryGrams <= 0) { setError('Indica cantidad y precio válidos para el ingreso.'); return } try { await api.addStock(entry.id, entryKg, entryGrams, entryPrice); setError(''); setSuccess('Ingreso de lote registrado.'); setEntry(null); load(); window.dispatchEvent(new Event('inventory-changed')) } catch (reason) { setError((reason as Error).message) } }
  async function saveLoss(event: FormEvent) { event.preventDefault(); if (!loss || lossGrams <= 0) { setError('Indica los gramos perdidos.'); return } try { await api.registerLoss(loss.id, lossGrams, lossReason); setError(''); setSuccess('Perdida registrada y descontada del inventario.'); setLoss(null); load(); window.dispatchEvent(new Event('inventory-changed')) } catch (reason) { setError((reason as Error).message) } }

  function updateWeight(kilos: number, remainder: number) { if (!draft) return; const safeKg = Math.max(0, Number.isFinite(kilos) ? kilos : 0); const safeRemainder = Math.min(999.99, Math.max(0, Number.isFinite(remainder) ? remainder : 0)); setStockKg(safeKg); setStockRemainder(safeRemainder); setDraft({ ...draft, stockGrams: safeKg * 1000 + safeRemainder, stockQuantity: Math.floor((safeKg * 1000 + safeRemainder) / 1000) }) }
  async function save(event: FormEvent) { event.preventDefault(); if (!draft) return; try { const total = Math.max(0, stockKg * 1000 + stockRemainder); await api.saveConsumable({ ...draft, stockGrams: total, stockQuantity: Math.floor(total / 1000) }); setSuccess(draft.id ? 'Consumible actualizado.' : 'Consumible agregado.'); setError(''); setDraft(null); load(); window.dispatchEvent(new Event('inventory-changed')) } catch (reason) { setError((reason as Error).message) } }
  async function archive(item: Consumable) { if (!await confirmDialog({ title: 'Archivar consumible', message: 'El consumible dejará de estar disponible para nuevas cotizaciones.', highlight: `${item.name} · ${item.material} · ${item.color}`, confirmLabel: 'Sí, archivar', variant: 'danger' })) return; try { await api.archiveConsumable(item.id); setDraft(null); load(); window.dispatchEvent(new Event('inventory-changed')) } catch (reason) { setError((reason as Error).message) } }

  return <>
    <PageHeader eyebrow="INVENTARIO" title="Filamentos y resinas" description="Registra existencias en kilos y gramos; las ventas descontaran el peso utilizado." actions={canEdit && <div className="button-group"><button onClick={() => setMaterialTypeDraft({ name: '' })}><Plus size={17} />Tipo de material</button><button onClick={() => edit({ ...blankConsumable, material: materialOptions[0] ?? 'PLA' })}><Plus size={17} />Nuevo consumible</button></div>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <div className={`catalog-layout ${draft || materialTypeDraft ? 'with-editor' : ''}`}>
      <section className="panel">
        <div className="toolbar"><div className="segmented">{['Todos', 'Filamento', 'Resina'].map(x => <button key={x} className={category === x ? 'active' : ''} onClick={() => { setCategory(x); setFilterMaterial('') }}>{x}</button>)}<button className={`filter-button ${filterBrand || filterMaterial ? 'active' : ''}`} onClick={() => setFilterOpen(true)} title="Filtrar consumibles"><Filter size={15} />Filtro{filterBrand || filterMaterial ? ' activo' : ''}</button></div><label className="check"><input type="checkbox" checked={includeArchived} onChange={e => setIncludeArchived(e.target.checked)} />Mostrar archivados</label></div>
        {canEdit && <div className="material-type-strip">{materialTypes.length === 0 ? <span className="muted">Sin tipos de material registrados.</span> : materialTypes.map(type => <button key={type.id} className={`ghost small ${type.active ? '' : 'danger-text'}`} onClick={() => setMaterialTypeDraft({ id: type.id, name: type.name })}>{type.name}</button>)}</div>}
        {filterOpen && <div className="filter-popover panel"><div className="section-title"><div><p className="eyebrow">FILTRO DE INVENTARIO</p><h3>Mostrar consumibles</h3></div><button className="icon ghost" onClick={() => setFilterOpen(false)}><X size={17} /></button></div><div className="form-grid two"><label>Marca<select value={filterBrand} onChange={e => setFilterBrand(e.target.value)}><option value="">Todas las marcas</option>{brands.map(value => <option key={value} value={value}>{value}</option>)}</select></label><label>Tipo / material<select value={filterMaterial} onChange={e => setFilterMaterial(e.target.value)}><option value="">Todos los tipos</option>{materialsByType.map(value => <option key={value} value={value}>{value}</option>)}</select></label></div><div className="editor-actions"><button type="button" className="ghost" onClick={() => { setFilterBrand(''); setFilterMaterial('') }}>Limpiar</button><button type="button" onClick={() => setFilterOpen(false)}>Aplicar filtro</button></div></div>}
        {loading ? <Loading /> : visible.length === 0 ? <Empty>No hay consumibles en esta categoria.</Empty> : <div className="table-wrap"><table><thead><tr><th>Consumible</th><th>Tipo / color</th><th>Precio</th><th>Existencia</th><th>Estado</th>{canEdit && <th>Acciones</th>}</tr></thead><tbody>{visible.map(item => { const total = currentGrams(item); const threshold = Number(item.lowStockGrams ?? 1000); const low = total <= threshold; return <tr key={item.id} className={!item.active ? 'archived-row' : ''}><td><strong>{item.name}</strong>{item.isDefault && <span className="preferred inline">PREDET.</span>}</td><td>{item.material} · {item.color}<small className="cell-note">{item.category} · densidad {number(item.density)}</small></td><td>{money(item.pricePerUnit, currency)} / kg</td><td><strong className={low ? 'low-stock' : ''}>{weight(total)}</strong><small className="cell-note">Alerta: {weight(threshold)}</small></td><td><Status active={item.active && !low} trueLabel="DISPONIBLE" falseLabel={!item.active ? 'ARCHIVADO' : total <= 0 ? 'AGOTADO' : 'STOCK BAJO'} /></td>{canEdit && <td><div className="row-actions"><button className="icon ghost" title="Registrar ingreso" onClick={() => receive(item)}><Upload size={15} /></button><button className="icon ghost" title="Registrar pérdida" onClick={() => reportLoss(item)}><Trash2 size={15} /></button><button className="icon ghost" title="Editar" onClick={() => edit(item)}><Edit3 size={15} /></button>{item.active && <button className="icon ghost danger-text" title="Archivar" onClick={() => archive(item)}><Archive size={15} /></button>}</div></td>}</tr> })}</tbody></table></div>}
      </section>
      {materialTypeDraft && <form className="panel editor" onSubmit={saveMaterialType}><div className="section-title"><div><p className="eyebrow">TIPO DE MATERIAL</p><h2>{materialTypeDraft.id ? materialTypeDraft.name : 'Nuevo tipo'}</h2></div><button type="button" className="icon ghost" onClick={() => setMaterialTypeDraft(null)}>x</button></div><label>Nombre<input required value={materialTypeDraft.name} onChange={e => setMaterialTypeDraft({ ...materialTypeDraft, name: e.target.value })} placeholder="PLA, PETG, ABS..." /></label><div className="editor-actions"><button type="button" className="ghost" onClick={() => setMaterialTypeDraft(null)}>Cancelar</button>{materialTypeDraft.id && <button type="button" className="ghost danger-text" onClick={() => archiveMaterialType({ id: materialTypeDraft.id!, name: materialTypeDraft.name, active: true })}>Archivar</button>}<button>Guardar</button></div></form>}
      {draft && <form className="panel editor" onSubmit={save}><div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR' : 'NUEVO'}</p><h2>Consumible</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}>x</button></div><label>Nombre<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} placeholder="Ej. eSUN" /></label><div className="form-grid two"><label>Categoria<select value={draft.category} onChange={e => setDraft({ ...draft, category: e.target.value })}><option>Filamento</option><option>Resina</option></select></label><label>Material / tipo<select required value={draft.material} onChange={e => setDraft({ ...draft, material: e.target.value })}><option value="">Seleccionar tipo</option>{materialOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label><label>Color<input required value={draft.color} onChange={e => setDraft({ ...draft, color: e.target.value })} /></label><label>Precio por kg<input type="number" min="0" step="0.01" value={draft.pricePerUnit} onChange={e => setDraft({ ...draft, pricePerUnit: Number(e.target.value) })} /></label><label>Densidad<input type="number" min="0.01" step="0.01" value={draft.density} onChange={e => setDraft({ ...draft, density: Number(e.target.value) })} /></label></div><p className="field-help">La existencia se guarda como peso total. Puedes introducir kilos completos y gramos adicionales.</p><div className="form-grid two"><label>Kilos<input type="number" min="0" step="1" value={stockKg} onChange={e => updateWeight(Number(e.target.value), stockRemainder)} /></label><label>Gramos adicionales<input type="number" min="0" max="999.99" step="0.01" value={stockRemainder} onChange={e => updateWeight(stockKg, Number(e.target.value))} /><span className="field-help">Usa un valor entre 0 y 999,99 g.</span></label><label>Alerta cuando queden menos de (g)<input type="number" min="0" step="0.01" value={Number.isFinite(draft.lowStockGrams) ? draft.lowStockGrams : 0} onChange={e => { const value = Number(e.target.value); setDraft({ ...draft, lowStockGrams: Number.isFinite(value) ? Math.max(0, value) : 0 }) }} /><span className="field-help">Ejemplo: 100 g avisa cuando queden 100 g o menos.</span></label></div><label className="check"><input type="checkbox" checked={draft.isDefault} onChange={e => setDraft({ ...draft, isDefault: e.target.checked })} />Consumible predeterminado</label>{draft.id > 0 && <label className="check"><input type="checkbox" checked={draft.active} onChange={e => setDraft({ ...draft, active: e.target.checked })} />Activo</label>}<div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button>Guardar</button></div></form>}
      {entry && <form className="panel editor" onSubmit={saveEntry}><div className="section-title"><div><p className="eyebrow">NUEVO LOTE</p><h2>Ingreso de {entry.name}</h2></div><button type="button" className="icon ghost" onClick={() => setEntry(null)}>x</button></div><p className="field-help">El lote queda separado para calcular automaticamente el precio por antiguedad.</p><div className="form-grid two"><label>Kilos<input type="number" min="0" step="0.01" value={entryKg} onChange={e => setEntryKg(Math.max(0, Number(e.target.value)))} /></label><label>Gramos adicionales<input type="number" min="0" max="999.99" step="0.01" value={entryGrams} onChange={e => setEntryGrams(Math.max(0, Number(e.target.value)))} /></label></div><label>Precio por kg<input required type="number" min="0" step="0.01" value={entryPrice} onChange={e => setEntryPrice(Math.max(0, Number(e.target.value)))} /></label><div className="editor-actions"><button type="button" className="ghost" onClick={() => setEntry(null)}>Cancelar</button><button><Upload size={16} />Registrar ingreso</button></div></form>}
      {loss && <form className="panel editor" onSubmit={saveLoss}><div className="section-title"><div><p className="eyebrow">PÉRDIDA DE MATERIAL</p><h2>{loss.name}</h2></div><button type="button" className="icon ghost" onClick={() => setLoss(null)}>x</button></div><p className="field-help">Registra gramos usados en una impresión fallida para descontarlos y analizarlos.</p><label>Gramos perdidos<input required type="number" min="0.01" step="0.01" value={lossGrams} onChange={e => setLossGrams(Math.max(0, Number(e.target.value)))} /></label><label>Motivo<input value={lossReason} onChange={e => setLossReason(e.target.value)} /></label><div className="editor-actions"><button type="button" className="ghost" onClick={() => setLoss(null)}>Cancelar</button><button><Trash2 size={16} />Registrar pérdida</button></div></form>}
    </div>
  </>
}

export function MaterialsPage({ canEdit, currency = 'Bs' }: { canEdit: boolean; currency?: string }) {
  const [items, setItems] = useState<ExtraMaterial[]>([])
  const [categories, setCategories] = useState<ExtraMaterialCategory[]>([])
  const [includeArchived, setIncludeArchived] = useState(false)
  const [draft, setDraft] = useState<ExtraMaterial | null>(null)
  const [categoryDraft, setCategoryDraft] = useState<MaterialTypeDraft | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(true)
  const load = useCallback(() => {
    setLoading(true)
    Promise.all([api.materials(includeArchived), api.extraMaterialCategories(includeArchived)])
      .then(([materialItems, categoryItems]) => { setItems(materialItems); setCategories(categoryItems); setError('') })
      .catch(reason => setError((reason as Error).message))
      .finally(() => setLoading(false))
  }, [includeArchived])
  useEffect(load, [load])

  const categoryOptions = useMemo(() => [...new Set(['Acabado', 'Ensamble', 'Empaque', 'Pintura', ...categories.filter(x => x.active).map(x => x.name), ...items.map(item => item.category).filter(Boolean)])].sort((a, b) => a.localeCompare(b)), [categories, items])

  async function save(event: FormEvent) {
    event.preventDefault(); if (!draft) return
    try { await api.saveMaterial(draft); setSuccess(draft.id ? 'Material actualizado.' : 'Material agregado.'); setError(''); setDraft(null); load() }
    catch (reason) { setError((reason as Error).message) }
  }
  async function archive(item: ExtraMaterial) {
    if (!await confirmDialog({ title: 'Archivar material', message: 'El material dejará de estar disponible para nuevas cotizaciones.', highlight: item.name, confirmLabel: 'Sí, archivar', variant: 'danger' })) return
    try { await api.archiveMaterial(item.id); setDraft(null); load() }
    catch (reason) { setError((reason as Error).message) }
  }
  async function saveCategory(event: FormEvent) {
    event.preventDefault(); if (!categoryDraft) return; setError(''); setSuccess('')
    try {
      if (categoryDraft.id) await api.updateExtraMaterialCategory(categoryDraft.id, categoryDraft.name)
      else await api.createExtraMaterialCategory(categoryDraft.name)
      setSuccess(categoryDraft.id ? 'Categoria actualizada.' : 'Categoria agregada.')
      setCategoryDraft(null); load()
    } catch (reason) { setError((reason as Error).message) }
  }
  async function archiveCategory(item: ExtraMaterialCategory) {
    if (!await confirmDialog({ title: 'Archivar categoría', message: 'La categoría dejará de aparecer como opción activa.', highlight: item.name, confirmLabel: 'Sí, archivar', variant: 'danger' })) return
    try { await api.archiveExtraMaterialCategory(item.id); setCategoryDraft(null); load() }
    catch (reason) { setError((reason as Error).message) }
  }

  return <>
    <PageHeader eyebrow="CATALOGO" title="Materiales adicionales" description="Imanes, tornillos, pegamento, pintura, empaque y otros costos por unidad." actions={canEdit && <div className="button-group"><button onClick={() => setCategoryDraft({ name: '' })}><Plus size={17} />Categoria</button><button onClick={() => setDraft({ ...blankMaterial, category: categoryOptions[0] ?? 'Acabado' })}><Plus size={17} />Nuevo material</button></div>} />
    <ErrorMessage error={error} /><SuccessMessage message={success} />
    <div className={`catalog-layout ${draft || categoryDraft ? 'with-editor' : ''}`}>
      <section className="panel">
        <div className="toolbar"><label className="check"><input type="checkbox" checked={includeArchived} onChange={e => setIncludeArchived(e.target.checked)} />Mostrar archivados</label></div>
        {canEdit && <div className="material-type-strip">{categories.length === 0 ? <span className="muted">Sin categorias registradas.</span> : categories.map(category => <button key={category.id} className={`ghost small ${category.active ? '' : 'danger-text'}`} onClick={() => setCategoryDraft({ id: category.id, name: category.name })}>{category.name}</button>)}</div>}
        {loading ? <Loading /> : items.length === 0 ? <Empty>No hay materiales registrados.</Empty> : <div className="card-grid">{items.map(item => <article className={`catalog-card ${!item.active ? 'archived' : ''}`} key={item.id}><div className="catalog-icon"><Boxes size={22} /></div><div className="catalog-main"><h3>{item.name}</h3><p>{item.category}</p><small>{money(item.unitPrice, currency)} / {item.unit}</small></div><Status active={item.active} />{canEdit && <div className="card-actions"><button className="ghost small" onClick={() => setDraft({ ...item })}><Edit3 size={15} />Editar</button>{item.active && <button className="ghost small danger-text" onClick={() => archive(item)}><Archive size={15} />Archivar</button>}</div>}</article>)}</div>}
      </section>
      {categoryDraft && <form className="panel editor" onSubmit={saveCategory}><div className="section-title"><div><p className="eyebrow">CATEGORIA</p><h2>{categoryDraft.id ? categoryDraft.name : 'Nueva categoria'}</h2></div><button type="button" className="icon ghost" onClick={() => setCategoryDraft(null)}>x</button></div><label>Nombre<input required value={categoryDraft.name} onChange={e => setCategoryDraft({ ...categoryDraft, name: e.target.value })} placeholder="Acabado, empaque, ensamble..." /></label><div className="editor-actions"><button type="button" className="ghost" onClick={() => setCategoryDraft(null)}>Cancelar</button>{categoryDraft.id && <button type="button" className="ghost danger-text" onClick={() => archiveCategory({ id: categoryDraft.id!, name: categoryDraft.name, active: true })}>Archivar</button>}<button>Guardar</button></div></form>}
      {draft && <form className="panel editor" onSubmit={save}><div className="section-title"><div><p className="eyebrow">{draft.id ? 'EDITAR' : 'NUEVO'}</p><h2>Material adicional</h2></div><button type="button" className="icon ghost" onClick={() => setDraft(null)}>x</button></div><label>Nombre<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label><label>Categoria<select required value={draft.category} onChange={e => setDraft({ ...draft, category: e.target.value })}><option value="">Seleccionar categoria</option>{categoryOptions.map(value => <option key={value} value={value}>{value}</option>)}</select></label><div className="form-grid two"><label>Unidad<input required value={draft.unit} onChange={e => setDraft({ ...draft, unit: e.target.value })} /></label><label>Precio unitario<input type="number" min="0" step="0.01" value={draft.unitPrice} onChange={e => setDraft({ ...draft, unitPrice: Number(e.target.value) })} /></label></div>{draft.id > 0 && <label className="check"><input type="checkbox" checked={draft.active} onChange={e => setDraft({ ...draft, active: e.target.checked })} />Activo</label>}<div className="editor-actions"><button type="button" className="ghost" onClick={() => setDraft(null)}>Cancelar</button><button>Guardar</button></div></form>}
    </div>
  </>
}

