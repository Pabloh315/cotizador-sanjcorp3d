import { ImageOff } from 'lucide-react'
import type { CSSProperties } from 'react'
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

export function CatalogProductImage({ kind, brand, model = '', material = '', colorName = '', colorHex = '#7d8285', compact = false }: Props) {
  const catalogPrinter = printerCatalog.find(item => printerCatalogLabel(item).toLowerCase() === model.toLowerCase() || (item.brand === brand && item.model === model))
  const displayBrand = brand || catalogPrinter?.brand || 'Marca personalizada'
  const displayModel = catalogPrinter?.model || model
  const title = kind === 'printer' ? `${displayBrand} ${displayModel}`.trim() : `${displayBrand} ${material} ${colorName}`.trim()
  const photoUrl = verifiedProductPhoto(kind, displayBrand, kind === 'printer' ? displayModel : material, colorName)
  const pendingStyle = { '--product-color': colorHex } as CSSProperties

  return <figure className={`catalog-product-image ${compact ? 'compact' : ''} ${photoUrl ? '' : 'pending'}`} aria-label={photoUrl ? `Fotografía verificada de ${title}` : `Fotografía pendiente de verificar para ${title}`} title={title}>
    {photoUrl
      ? <img src={photoUrl} alt={`${title} — fotografía verificada del producto`} loading="lazy" referrerPolicy="no-referrer" />
      : <div className="photo-pending" style={pendingStyle}><ImageOff size={compact ? 18 : 28} /><span>{compact ? 'Sin foto' : 'Foto real pendiente de verificar'}</span></div>}
    {!compact && <figcaption><strong>{displayBrand}</strong><span>{kind === 'printer' ? displayModel : [material, colorName].filter(Boolean).join(' · ')}</span></figcaption>}
  </figure>
}
