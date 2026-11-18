import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  Activity, AlertTriangle, ArrowDownRight, ArrowUpRight, BarChart3, BriefcaseBusiness,
  CircleDollarSign, Download, Gauge, PackageCheck, RefreshCw, ShieldAlert, Target, TrendingUp,
} from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Report } from '../types'
import { Empty, ErrorMessage, Loading, PageHeader, firstDayOfMonth, formatDate, money, number, todayInput } from '../ui'

type DailyPoint = { date: string; revenue: number; profit: number; cost: number; operations: number }
type DistributionItem = { name: string; value: number; label: string }

const percent = (value: number) => `${number(value, 1)}%`
const ratio = (value: number, base: number) => base > 0 ? value / base * 100 : 0

export function ReportsPage() {
  const [from, setFrom] = useState(firstDayOfMonth())
  const [to, setTo] = useState(todayInput())
  const [data, setData] = useState<Report>()
  const [settings, setSettings] = useState<BusinessSettings>()
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const load = useCallback(() => {
    setLoading(true); setError('')
    Promise.all([api.report(from, to), api.settings()])
      .then(([report, configuration]) => { setData(report); setSettings(configuration) })
      .catch(reason => setError((reason as Error).message))
      .finally(() => setLoading(false))
  }, [from, to])
  useEffect(load, [load])

  const symbol = settings?.currencySymbol
  const analytics = useMemo(() => buildAnalytics(data), [data])

  return <div className="executive-report">
    <PageHeader eyebrow="INTELIGENCIA COMERCIAL" title="Centro de análisis financiero" description="Rendimiento, rentabilidad, riesgo operativo y composición del negocio en una sola vista ejecutiva." actions={<div className="button-group report-actions"><button className="secondary" onClick={() => api.exportQuotes({ from, to }).catch(reason => setError((reason as Error).message))}><Download size={16} />Cotizaciones</button><button className="secondary" onClick={() => api.exportSales(from, to).catch(reason => setError((reason as Error).message))}><Download size={16} />Ventas</button></div>} />
    <ErrorMessage error={error} />

    <section className="report-command-bar" aria-label="Período del reporte">
      <div><span className="live-signal"><i /> DATOS OPERATIVOS</span><strong>{from} <em>→</em> {to}</strong></div>
      <div className="report-date-controls"><label>Desde<input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label><label>Hasta<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label><button onClick={load} disabled={loading}><RefreshCw size={16} className={loading ? 'spin' : ''} />Actualizar</button></div>
    </section>

    {loading || !data ? <Loading label="Procesando indicadores del período…" /> : <>
      <section className="market-strip" aria-label="Resumen del período">
        <MarketTicker label="OPERACIONES" value={number(analytics.operations, 0)} detail={`${data.saleCount} fabricación · ${data.retailSaleCount} comercial`} tone="cyan" />
        <MarketTicker label="INGRESO TOTAL" value={money(analytics.revenue, symbol)} detail={`${percent(analytics.realization)} de la proyección`} tone="blue" />
        <MarketTicker label="GANANCIA NETA" value={money(analytics.profit, symbol)} detail={`Margen ${percent(analytics.margin)}`} tone={analytics.profit >= 0 ? 'green' : 'red'} />
        <MarketTicker label="RIESGO POR PÉRDIDAS" value={money(data.lossTotalCost, symbol)} detail={`${percent(analytics.lossImpact)} de la ganancia`} tone={analytics.lossImpact > 10 ? 'red' : 'amber'} />
      </section>

      <section className="financial-kpis">
        <FinancialKpi label="Margen operativo" value={percent(analytics.margin)} note="Ganancia sobre ingresos realizados" icon={<Gauge size={19} />} change={analytics.margin >= 25 ? 'Saludable' : analytics.margin > 0 ? 'Vigilar' : 'Crítico'} positive={analytics.margin >= 25} />
        <FinancialKpi label="Retorno sobre costo" value={percent(analytics.roi)} note="Ganancia generada por capital utilizado" icon={<TrendingUp size={19} />} change={analytics.roi >= 30 ? 'Alto' : 'Moderado'} positive={analytics.roi >= 30} />
        <FinancialKpi label="Conversión comercial" value={percent(analytics.conversion)} note="Ventas de fabricación por cotización" icon={<Target size={19} />} change={`${data.saleCount}/${data.quoteCount}`} positive={analytics.conversion >= 25} />
        <FinancialKpi label="Ticket promedio" value={money(analytics.averageTicket, symbol)} note="Ingreso medio por operación confirmada" icon={<BriefcaseBusiness size={19} />} change={`${analytics.operations} operaciones`} positive />
      </section>

      <section className="report-terminal-grid">
        <article className="panel performance-panel">
          <div className="terminal-heading"><div><span>RENDIMIENTO · SERIE TEMPORAL</span><h2>Ingresos y ganancia diaria</h2><p>Comportamiento combinado de fabricación y ventas comerciales.</p></div><div className="chart-key"><span><i className="revenue" />Ingresos</span><span><i className="profit" />Ganancia</span></div></div>
          <PerformanceChart points={analytics.daily} symbol={symbol} />
          <div className="performance-summary"><div><span>Costo ejecutado</span><strong>{money(analytics.cost, symbol)}</strong></div><div><span>Ingreso proyectado</span><strong>{money(data.projectedRevenue, symbol)}</strong></div><div><span>Brecha a objetivo</span><strong className={analytics.revenue >= data.projectedRevenue ? 'positive' : 'warning-text'}>{money(analytics.revenue - data.projectedRevenue, symbol)}</strong></div></div>
        </article>

        <aside className="panel intelligence-panel">
          <div className="terminal-heading"><div><span>LECTURA EJECUTIVA</span><h2>Diagnóstico del período</h2></div><Activity size={20} /></div>
          <Insight tone={analytics.margin >= 25 ? 'good' : 'watch'} icon={analytics.margin >= 25 ? <ArrowUpRight size={18} /> : <AlertTriangle size={18} />} title={analytics.margin >= 25 ? 'Rentabilidad sólida' : 'Margen bajo observación'} text={`El margen operativo se ubica en ${percent(analytics.margin)}. ${analytics.margin >= 25 ? 'Existe espacio para crecimiento controlado.' : 'Revisa precios, costos y porcentajes de ganancia.'}`} />
          <Insight tone={analytics.realization >= 70 ? 'good' : 'watch'} icon={<Target size={18} />} title="Cumplimiento de proyección" text={`Los ingresos confirmados representan ${percent(analytics.realization)} del valor proyectado en cotizaciones.`} />
          <Insight tone={analytics.lossImpact <= 5 ? 'good' : 'risk'} icon={<ShieldAlert size={18} />} title="Exposición por pérdidas" text={`${money(data.lossTotalCost, symbol)} comprometidos, equivalentes al ${percent(analytics.lossImpact)} de la ganancia real.`} />
          <Insight tone={analytics.conversion >= 25 ? 'good' : 'watch'} icon={<PackageCheck size={18} />} title="Eficiencia comercial" text={`${data.saleCount} ventas confirmadas de ${data.quoteCount} cotizaciones registradas.`} />
        </aside>
      </section>

      <section className="allocation-grid">
        <AllocationPanel eyebrow="PORTAFOLIO" title="Productos con mayor movimiento" items={data.productDistribution.map(item => ({ name: item.name, value: item.quantity, label: `${item.quantity} pieza(s)` }))} />
        <AllocationPanel eyebrow="CONSUMO" title="Uso de consumibles" items={data.consumableDistribution.map(item => ({ name: item.name, value: item.grams, label: `${number(item.grams)} g` }))} />
        <AllocationPanel eyebrow="ESTRUCTURA" title="Composición de costos" items={data.costDistribution.map(item => ({ name: item.name, value: item.value, label: money(item.value, symbol) }))} />
        <AllocationPanel eyebrow="CANAL COMERCIAL" title="Ventas por categoría" items={data.retailByCategory.map(item => ({ name: item.name, value: item.revenue, label: `${item.quantity} unid. · ${money(item.revenue, symbol)}` }))} />
      </section>

      <section className="panel top-gap report-table-panel"><div className="terminal-heading"><div><span>CONTROL DE RIESGO</span><h2>Pérdidas operativas</h2><p>{number(data.lossTotalGrams)} g desperdiciados · impacto estimado {money(data.lossTotalCost, symbol)}</p></div><AlertTriangle size={20} /></div>{data.lossDetails.length === 0 ? <Empty>No se registraron pérdidas en este período.</Empty> : <div className="table-wrap"><table><thead><tr><th>Fecha</th><th>Material</th><th>Motivo</th><th>Volumen</th><th>Impacto</th></tr></thead><tbody>{data.lossDetails.map(item => <tr key={item.id}><td>{formatDate(item.createdAtUtc)}</td><td><strong>{item.consumableName}</strong><small className="cell-note">{item.material} · {item.color}</small></td><td>{item.reason}</td><td><strong>{number(item.grams)} g</strong></td><td className="negative">{money(item.estimatedCost, symbol)}</td></tr>)}</tbody></table></div>}</section>

      <section className="panel top-gap report-table-panel"><div className="terminal-heading"><div><span>LIBRO DE OPERACIONES</span><h2>Ventas de fabricación</h2><p>Detalle financiero de cotizaciones convertidas en venta.</p></div><BarChart3 size={20} /></div>{data.sales.length === 0 ? <Empty>No hay ventas confirmadas en este período.</Empty> : <div className="table-wrap"><table><thead><tr><th>Código</th><th>Fecha</th><th>Cliente / proyecto</th><th>Costo</th><th>Venta</th><th>Ganancia</th><th>Margen</th></tr></thead><tbody>{data.sales.map(item => { const margin = ratio(item.profit, item.saleAmount); return <tr key={item.id}><td className="mono">{item.orderCode}</td><td>{formatDate(item.soldAtUtc)}</td><td><strong>{item.customer}</strong><small className="cell-note">{item.projectName}</small></td><td>{money(item.costTotal, symbol)}</td><td><strong>{money(item.saleAmount, symbol)}</strong></td><td className={item.profit >= 0 ? 'positive' : 'negative'}>{money(item.profit, symbol)}</td><td><span className={`market-pill ${margin >= 25 ? 'up' : 'watch'}`}>{percent(margin)}</span></td></tr> })}</tbody></table></div>}</section>

      <section className="panel top-gap report-table-panel"><div className="terminal-heading"><div><span>MERCADO COMERCIAL</span><h2>Impresoras, filamentos y resinas</h2><p>Operaciones directas de inventario comercial.</p></div><CircleDollarSign size={20} /></div>{data.retailSales.length === 0 ? <Empty>No hay ventas comerciales en este período.</Empty> : <div className="table-wrap"><table><thead><tr><th>Fecha</th><th>Categoría</th><th>Producto / cliente</th><th>Cantidad</th><th>Costo</th><th>Venta</th><th>Ganancia</th></tr></thead><tbody>{data.retailSales.map(item => <tr key={item.id}><td>{formatDate(item.soldAtUtc)}</td><td><span className="market-pill neutral">{item.category === 'Printer' ? 'Impresora' : item.category === 'Resin' ? 'Resina' : 'Filamento'}</span></td><td><strong>{item.productName}</strong><small className="cell-note">{item.customer || 'Mostrador'}</small></td><td>{item.quantity}</td><td>{money(item.totalCost, symbol)}</td><td><strong>{money(item.totalPrice, symbol)}</strong></td><td className="positive">{money(item.profit, symbol)}</td></tr>)}</tbody></table></div>}</section>
    </>}
  </div>
}

function buildAnalytics(data?: Report) {
  if (!data) return { revenue: 0, profit: 0, cost: 0, operations: 0, margin: 0, roi: 0, conversion: 0, averageTicket: 0, lossImpact: 0, realization: 0, daily: [] as DailyPoint[] }
  const revenue = data.salesRevenue + data.retailRevenue
  const profit = data.salesProfit + data.retailProfit
  const cost = revenue - profit
  const operations = data.saleCount + data.retailSaleCount
  const byDate = new Map<string, DailyPoint>()
  const add = (dateValue: string, itemRevenue: number, itemProfit: number) => {
    const date = dateValue.slice(0, 10)
    const current = byDate.get(date) ?? { date, revenue: 0, profit: 0, cost: 0, operations: 0 }
    current.revenue += itemRevenue; current.profit += itemProfit; current.cost += itemRevenue - itemProfit; current.operations += 1
    byDate.set(date, current)
  }
  data.sales.forEach(item => add(item.soldAtUtc, item.saleAmount, item.profit))
  data.retailSales.forEach(item => add(item.soldAtUtc, item.totalPrice, item.profit))
  return {
    revenue, profit, cost, operations,
    margin: ratio(profit, revenue), roi: ratio(profit, cost), conversion: ratio(data.saleCount, data.quoteCount),
    averageTicket: operations > 0 ? revenue / operations : 0,
    lossImpact: profit > 0 ? ratio(data.lossTotalCost, profit) : data.lossTotalCost > 0 ? 100 : 0,
    realization: data.projectedRevenue > 0 ? ratio(revenue, data.projectedRevenue) : revenue > 0 ? 100 : 0,
    daily: [...byDate.values()].sort((a, b) => a.date.localeCompare(b.date)),
  }
}

function MarketTicker({ label, value, detail, tone }: { label: string; value: string; detail: string; tone: 'cyan' | 'blue' | 'green' | 'amber' | 'red' }) {
  return <article className={`market-ticker ${tone}`}><span>{label}</span><strong>{value}</strong><small>{tone === 'red' ? <ArrowDownRight size={13} /> : <ArrowUpRight size={13} />}{detail}</small></article>
}

function FinancialKpi({ label, value, note, icon, change, positive }: { label: string; value: string; note: string; icon: ReactNode; change: string; positive: boolean }) {
  return <article className="financial-kpi"><div className="financial-kpi-head"><span>{icon}{label}</span><em className={positive ? 'positive' : 'warning-text'}>{change}</em></div><strong>{value}</strong><p>{note}</p><i className={positive ? 'good' : 'watch'} /></article>
}

function Insight({ tone, icon, title, text }: { tone: 'good' | 'watch' | 'risk'; icon: ReactNode; title: string; text: string }) {
  return <div className={`report-insight ${tone}`}><span>{icon}</span><div><strong>{title}</strong><p>{text}</p></div></div>
}

function PerformanceChart({ points, symbol = 'Bs' }: { points: DailyPoint[]; symbol?: string }) {
  if (!points.length) return <Empty>Confirma ventas para generar la serie financiera.</Empty>
  const width = 820; const height = 260; const left = 55; const right = 20; const top = 20; const bottom = 38
  const max = Math.max(...points.flatMap(point => [point.revenue, point.profit]), 1)
  const min = Math.min(...points.flatMap(point => [point.revenue, point.profit]), 0)
  const span = Math.max(max - min, 1)
  const x = (index: number) => points.length === 1 ? (left + width - right) / 2 : left + index * ((width - left - right) / (points.length - 1))
  const y = (value: number) => top + (height - top - bottom) * (1 - (value - min) / span)
  const revenue = points.map((point, index) => `${x(index)},${y(point.revenue)}`).join(' ')
  const profit = points.map((point, index) => `${x(index)},${y(point.profit)}`).join(' ')
  const baseline = y(0)
  const area = `${x(0)},${baseline} ${revenue} ${x(points.length - 1)},${baseline}`
  const labelIndexes = [...new Set([0, Math.floor((points.length - 1) / 2), points.length - 1])]
  return <div className="performance-chart"><svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Serie temporal de ingresos y ganancias">
    <defs><linearGradient id="revenueArea" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#32d9f5" stopOpacity=".28" /><stop offset="1" stopColor="#32d9f5" stopOpacity="0" /></linearGradient></defs>
    {[0, .25, .5, .75, 1].map(step => { const value = min + span * step; return <g key={step}><line x1={left} x2={width - right} y1={y(value)} y2={y(value)} /><text x={left - 8} y={y(value) + 4} textAnchor="end">{compactMoney(value, symbol)}</text></g> })}
    {min < 0 && <line className="zero-line" x1={left} x2={width - right} y1={baseline} y2={baseline} />}
    <polygon points={area} fill="url(#revenueArea)" /><polyline className="revenue-line" points={revenue} /><polyline className="profit-line" points={profit} />
    {points.map((point, index) => <g key={point.date}><circle className="revenue-point" cx={x(index)} cy={y(point.revenue)} r="3"><title>{point.date}: {money(point.revenue, symbol)}</title></circle><circle className="profit-point" cx={x(index)} cy={y(point.profit)} r="3"><title>{point.date}: {money(point.profit, symbol)} de ganancia</title></circle></g>)}
    {labelIndexes.map(index => <text key={index} className="date-label" x={x(index)} y={height - 12} textAnchor={index === 0 ? 'start' : index === points.length - 1 ? 'end' : 'middle'}>{new Date(`${points[index].date}T12:00:00`).toLocaleDateString('es-BO', { day: '2-digit', month: 'short' })}</text>)}
  </svg></div>
}

function compactMoney(value: number, symbol: string) {
  if (Math.abs(value) >= 1_000_000) return `${symbol} ${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `${symbol} ${(value / 1_000).toFixed(1)}k`
  return `${symbol} ${value.toFixed(0)}`
}

function AllocationPanel({ eyebrow, title, items }: { eyebrow: string; title: string; items: DistributionItem[] }) {
  const total = items.reduce((sum, item) => sum + item.value, 0)
  const max = Math.max(...items.map(item => item.value), 0)
  const topItems = [...items].sort((a, b) => b.value - a.value).slice(0, 6)
  return <article className="panel allocation-panel"><div className="terminal-heading"><div><span>{eyebrow}</span><h2>{title}</h2></div><strong>{number(total)}</strong></div>{!topItems.length || max <= 0 ? <Empty>Sin valores para representar.</Empty> : <div className="allocation-bars">{topItems.map((item, index) => <div key={item.name}><div><span><i>{String(index + 1).padStart(2, '0')}</i>{item.name}</span><strong>{item.label}</strong></div><progress max={max} value={item.value} /><small>{percent(item.value / total * 100)} del total</small></div>)}</div>}</article>
}
