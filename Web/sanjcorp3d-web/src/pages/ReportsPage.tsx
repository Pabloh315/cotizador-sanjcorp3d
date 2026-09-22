import { useCallback, useEffect, useState } from 'react'
import { BadgeDollarSign, CircleDollarSign, Download, FileText, PieChart, TrendingUp } from 'lucide-react'
import { api } from '../api'
import type { BusinessSettings, Report } from '../types'
import { Empty, ErrorMessage, Loading, Metric, PageHeader, firstDayOfMonth, formatDate, money, number, todayInput } from '../ui'

export function ReportsPage() {
  const [from, setFrom] = useState(firstDayOfMonth()); const [to, setTo] = useState(todayInput())
  const [data, setData] = useState<Report>(); const [settings, setSettings] = useState<BusinessSettings>(); const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  const [chartType, setChartType] = useState<'pie' | 'line'>('pie')
  const load = useCallback(() => { setLoading(true); setError(''); Promise.all([api.report(from, to), api.settings()]).then(([report, configuration]) => { setData(report); setSettings(configuration) }).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false)) }, [from, to])
  useEffect(load, [load])
  const symbol = settings?.currencySymbol

  return <>
    <PageHeader eyebrow="ANÁLISIS" title="Reportes y ventas" description="Resultados proyectados, ventas confirmadas y distribución de costos." actions={<div className="button-group"><div className="chart-toggle" role="group" aria-label="Tipo de gráfica"><button className={chartType === 'pie' ? 'active' : ''} onClick={() => setChartType('pie')}><PieChart size={16} />Pastel</button><button className={chartType === 'line' ? 'active' : ''} onClick={() => setChartType('line')}><TrendingUp size={16} />Líneas</button></div><button className="secondary" onClick={() => api.exportQuotes({ from, to }).catch(reason => setError((reason as Error).message))}><Download size={16} />Cotizaciones CSV</button><button className="secondary" onClick={() => api.exportSales(from, to).catch(reason => setError((reason as Error).message))}><Download size={16} />Ventas CSV</button></div>} />
    <ErrorMessage error={error} />
    <section className="panel filters-panel"><div className="filters"><label>Desde<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Hasta<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label><button onClick={load}>Actualizar reporte</button></div></section>
    {loading || !data ? <Loading /> : <>
      <section className="metrics report-metrics"><Metric label="Cotizaciones" value={data.quoteCount} icon={<FileText size={20} />} /><Metric label="Costo proyectado" value={money(data.totalCost, symbol)} icon={<CircleDollarSign size={20} />} /><Metric label="Ingreso proyectado" value={money(data.projectedRevenue, symbol)} icon={<TrendingUp size={20} />} /><Metric label="Ganancia proyectada" value={money(data.projectedProfit, symbol)} icon={<BadgeDollarSign size={20} />} /></section>
      <section className="metrics sales-metrics"><Metric label="Ventas confirmadas" value={data.saleCount} icon={<FileText size={20} />} /><Metric label="Ingresos reales" value={money(data.salesRevenue, symbol)} icon={<CircleDollarSign size={20} />} /><Metric label="Ganancia real" value={money(data.salesProfit, symbol)} icon={<TrendingUp size={20} />} /></section>
      <div className="report-grid top-gap">
        <section className="panel"><div className="section-title"><div><p className="eyebrow">PRODUCTOS</p><h2>Lo más vendido</h2></div></div><Chart items={data.productDistribution.map(x => ({ name: x.name, value: x.quantity, label: `${x.quantity} pieza(s)` }))} type={chartType} /></section>
        <section className="panel"><div className="section-title"><div><p className="eyebrow">CONSUMO</p><h2>Distribución por consumible</h2></div></div><Chart items={data.consumableDistribution.map(x => ({ name: x.name, value: x.grams, label: `${number(x.grams)} g` }))} type={chartType} /></section>
        <section className="panel"><div className="section-title"><div><p className="eyebrow">ESTRUCTURA</p><h2>Distribución de costos</h2></div></div><Chart items={data.costDistribution.map(x => ({ name: x.name, value: x.value, label: money(x.value, symbol) }))} type={chartType} /></section>
      </div>
      <section className="panel top-gap"><div className="section-title"><div><p className="eyebrow">PÉRDIDAS</p><h2>Productos fallidos</h2></div></div><Chart items={data.lossDistribution.map(x => ({ name: x.name, value: x.grams, label: `${number(x.grams)} g` }))} type={chartType} /></section>
      <section className="panel top-gap"><div className="section-title"><div><p className="eyebrow">VENTAS GENERADAS</p><h2>Detalle del período</h2></div></div>{data.sales.length === 0 ? <Empty>No hay ventas confirmadas en este período.</Empty> : <div className="table-wrap"><table><thead><tr><th>Código</th><th>Fecha</th><th>Cliente</th><th>Proyecto</th><th>Costo</th><th>Venta</th><th>Ganancia</th></tr></thead><tbody>{data.sales.map(item => <tr key={item.id}><td className="mono">{item.orderCode}</td><td>{formatDate(item.soldAtUtc)}</td><td>{item.customer}</td><td>{item.projectName}</td><td>{money(item.costTotal, symbol)}</td><td><strong>{money(item.saleAmount, symbol)}</strong></td><td className={item.profit >= 0 ? 'positive' : 'negative'}>{money(item.profit, symbol)}</td></tr>)}</tbody></table></div>}</section>
    </>}
  </>
}

function Chart({ items, type }: { items: Array<{ name: string; value: number; label: string }>; type: 'pie' | 'line' }) {
  const max = Math.max(...items.map(x => x.value), 0)
  if (!items.length || max <= 0) return <Empty>Sin valores para representar.</Empty>
  if (type === 'line') {
    const points = items.map((item, index) => `${items.length === 1 ? 140 : 20 + index * (260 / (items.length - 1))},${150 - item.value / max * 120}`).join(' ')
    return <div className="line-chart"><svg viewBox="0 0 280 170" role="img" aria-label="Gráfica de líneas"><line x1="20" y1="150" x2="270" y2="150" /><polyline points={points} />{items.map((item, index) => { const x = items.length === 1 ? 140 : 20 + index * (260 / (items.length - 1)); const y = 150 - item.value / max * 120; return <circle key={item.name} cx={x} cy={y} r="4"><title>{item.name}: {item.label}</title></circle> })}</svg><ChartLegend items={items} /></div>
  }
  let cursor = 0
  const colors = ['#62ddba', '#f0b875', '#8aa9ff', '#e8899a', '#b58cff', '#78c9e8']
  const stops = items.map((item, index) => { const start = cursor; cursor += item.value / items.reduce((sum, current) => sum + current.value, 0) * 100; return `${colors[index % colors.length]} ${start}% ${cursor}%` }).join(', ')
  return <div className="pie-chart"><div className="pie-visual" style={{ background: `conic-gradient(${stops})` }} aria-label="Gráfica circular" role="img" /><ChartLegend items={items} colors={colors} /></div>
}

function ChartLegend({ items, colors = ['#62ddba', '#f0b875', '#8aa9ff', '#e8899a', '#b58cff', '#78c9e8'] }: { items: Array<{ name: string; value: number; label: string }>; colors?: string[] }) {
  return <div className="chart-legend">{items.map((item, index) => <div key={item.name}><span style={{ background: colors[index % colors.length] }} /> <span>{item.name}</span><b>{item.label}</b></div>)}</div>
}



