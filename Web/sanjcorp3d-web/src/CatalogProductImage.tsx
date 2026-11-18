import { useEffect, useState, type CSSProperties } from 'react'
import { colorsFor } from './materialCatalog'
import { printerCatalog, printerCatalogLabel } from './printerCatalog'
import { verifiedProductPhoto } from './verifiedProductPhotos'

type Props = {
  kind: 'printer' | 'filament' | 'resin'
  brand: string
  model?: string
  material?: string
  colorName?: string
  colorHex?: string
  compact?: boolean
}

const photoPrinterBrands = new Set(['bambu lab', 'creality', 'elegoo'])

export function CatalogProductImage({ kind, brand, model = '', material = '', colorName = '', colorHex = '', compact = false }: Props) {
  const catalogPrinter = printerCatalog.find(item => printerCatalogLabel(item).toLowerCase() === model.toLowerCase() || (item.brand === brand && item.model === model))
  const displayBrand = brand || catalogPrinter?.brand || 'Marca personalizada'
  const displayModel = catalogPrinter?.model || model
  const title = kind === 'printer' ? `${displayBrand} ${displayModel}`.trim() : `${displayBrand} ${material} ${colorName}`.trim()
  const catalogColor = colorsFor(displayBrand).find(item => item.name.toLowerCase() === colorName.trim().toLowerCase())?.hex
  const productColor = colorHex || catalogColor || '#7d8285'
  const candidatePhoto = kind === 'printer' && photoPrinterBrands.has(displayBrand.trim().toLowerCase())
    ? verifiedProductPhoto('printer', displayBrand, displayModel)
    : undefined
  const [photoFailed, setPhotoFailed] = useState(false)
  useEffect(() => setPhotoFailed(false), [candidatePhoto])
  const photoUrl = photoFailed ? undefined : candidatePhoto
  const genericStyle = { '--product-color': productColor } as CSSProperties
  const initials = displayBrand.split(/\s+/).filter(Boolean).map(part => part[0]).join('').slice(0, 2).toUpperCase()

  return <figure className={`catalog-product-image ${compact ? 'compact' : ''} ${photoUrl ? 'verified-photo' : `generic ${kind}`}`} aria-label={photoUrl ? `Fotografía de ${title}` : `Representación de ${title}`} title={title}>
    {photoUrl
      ? <img src={photoUrl} alt={`${title} — fotografía del producto`} loading="lazy" referrerPolicy="no-referrer" onError={() => setPhotoFailed(true)} />
      : kind === 'printer'
        ? <div className="generic-product generic-printer" style={genericStyle} role="img" aria-label={`Impresora 3D genérica ${displayBrand}`}><div className="printer-shell"><div className="printer-window"><i /><span /></div><b>{initials || '3D'}</b></div></div>
        : kind === 'filament'
          ? <div className="generic-product generic-filament" style={genericStyle} role="img" aria-label={`Carrete de filamento ${colorName}`}><div className="filament-spool"><i /><span /></div></div>
          : <div className="generic-product generic-resin" style={genericStyle} role="img" aria-label={`Botella de resina ${colorName}`}><div className="resin-bottle"><i /><span /></div></div>}
    {!compact && <figcaption><strong>{displayBrand}</strong><span>{kind === 'printer' ? displayModel : [material, colorName].filter(Boolean).join(' · ')}</span></figcaption>}
  </figure>
}
