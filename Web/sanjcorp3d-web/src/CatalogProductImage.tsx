import { useEffect, useState } from 'react'
import { colorsFor } from './materialCatalog'
import { printerCatalog, printerCatalogLabel } from './printerCatalog'

type Props = {
  kind: 'printer' | 'filament' | 'resin'
  brand: string
  model?: string
  material?: string
  colorName?: string
  colorHex?: string
  compact?: boolean
}

function brandAccent(brand: string) {
  let hash = 0
  for (const character of brand) hash = character.charCodeAt(0) + ((hash << 5) - hash)
  const hue = Math.abs(hash) % 360
  return `hsl(${hue} 62% 52%)`
}

export function CatalogProductImage({ kind, brand, model = '', material = '', colorName = '', colorHex, compact = false }: Props) {
  const catalogPrinter = printerCatalog.find(item => printerCatalogLabel(item).toLowerCase() === model.toLowerCase() || (item.brand === brand && item.model === model))
  const displayBrand = brand || catalogPrinter?.brand || 'Marca personalizada'
  const displayModel = catalogPrinter?.model || model
  const accent = kind === 'printer' ? brandAccent(displayBrand) : colorHex || colorsFor(displayBrand).find(item => item.name.toLowerCase() === colorName.toLowerCase())?.hex || '#7d8285'
  const resin = kind === 'resin'
  const technology = catalogPrinter?.technology ?? (kind === 'printer' ? 'FDM/FFF' : '')
  const title = kind === 'printer' ? `${displayBrand} ${displayModel}`.trim() : `${displayBrand} ${material} ${colorName}`.trim()
  const photoQuery = kind === 'printer'
    ? `${displayBrand} ${displayModel} 3D printer official product`
    : `${displayBrand} ${material} ${colorName} ${resin ? '3D printer resin bottle' : '3D printing filament spool'} official product`
  const photoUrl = `https://tse2.mm.bing.net/th?q=${encodeURIComponent(photoQuery)}&w=640&h=480&c=7&rs=1&p=0`
  const [photoFailed, setPhotoFailed] = useState(false)
  useEffect(() => setPhotoFailed(false), [photoUrl])

  return <figure className={`catalog-product-image ${compact ? 'compact' : ''}`} aria-label={`Imagen automática de ${title}`} title={title}>
    {!photoFailed && <img src={photoUrl} alt={`${title} — fotografía referencial del producto`} loading="lazy" referrerPolicy="no-referrer" onError={() => setPhotoFailed(true)} />}
    {photoFailed && <svg viewBox="0 0 180 128" role="img" aria-label={`Imagen de respaldo para ${title}`}>
      <rect width="180" height="128" rx="12" fill="#0b1714" />
      <circle cx="152" cy="20" r="34" fill={accent} opacity=".14" />
      {kind === 'printer' && technology.includes('Resina') && <>
        <path d="M63 25h55l9 26v49H54V51z" fill={accent} opacity=".82" />
        <path d="M66 29h49l6 21H60z" fill="#dce8e4" opacity=".72" />
        <rect x="59" y="55" width="63" height="39" rx="4" fill="#12241f" />
        <rect x="70" y="63" width="41" height="4" rx="2" fill={accent} />
        <rect x="48" y="98" width="84" height="9" rx="3" fill="#263d36" />
      </>}
      {kind === 'printer' && !technology.includes('Resina') && <>
        <path d="M42 24h96v82H42z" fill="none" stroke={accent} strokeWidth="8" strokeLinejoin="round" />
        <path d="M51 31h78v62H51z" fill="#11221d" opacity=".9" />
        <path d="M56 87h68l10 12H46z" fill="#314940" />
        <path d="M65 42h51M90 42v25" stroke="#b9cac4" strokeWidth="4" strokeLinecap="round" />
        <rect x="82" y="62" width="16" height="12" rx="3" fill={accent} />
        <path d="M90 74l-5 8h10z" fill="#d9e7e2" />
      </>}
      {kind === 'filament' && <>
        <circle cx="90" cy="66" r="46" fill={accent} stroke="#d8e5e0" strokeWidth="5" />
        <circle cx="90" cy="66" r="20" fill="#10201b" stroke="#d8e5e0" strokeWidth="5" />
        <path d="M47 50c25-15 61-15 86 0M45 66c28-14 62-14 90 0M49 82c25-12 57-12 82 0" fill="none" stroke="#fff" strokeOpacity=".28" strokeWidth="4" />
        <circle cx="90" cy="66" r="6" fill={accent} />
      </>}
      {resin && <>
        <rect x="70" y="20" width="40" height="14" rx="4" fill="#c9d8d3" />
        <path d="M63 38q0-8 8-8h38q8 0 8 8l8 64q1 8-8 8H63q-9 0-8-8z" fill={accent} />
        <rect x="61" y="59" width="58" height="34" rx="4" fill="#e7efec" opacity=".9" />
        <path d="M72 71h36M72 80h24" stroke={accent} strokeWidth="4" strokeLinecap="round" />
      </>}
    </svg>}
    {!compact && <figcaption><strong>{displayBrand}</strong><span>{kind === 'printer' ? displayModel : [material, colorName].filter(Boolean).join(' · ')}</span></figcaption>}
  </figure>
}
