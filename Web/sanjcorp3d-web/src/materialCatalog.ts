import type { RetailColor } from './types'

export type MaterialCategory = 'Filament' | 'Resin'
export type MaterialBrand = {
  brand: string
  filamentTypes?: string[]
  resinTypes?: string[]
  colors: RetailColor[]
}

const color = (name: string, hex: string, brand: string): RetailColor => ({ name, hex, brand, materialType: 'Todos' })
const basic = (brand: string) => [
  color('Negro', '#151515', brand), color('Blanco', '#F4F4F0', brand), color('Gris', '#7D8285', brand),
  color('Rojo', '#D72638', brand), color('Azul', '#1769AA', brand), color('Verde', '#2E8B57', brand),
  color('Amarillo', '#F4D03F', brand), color('Naranja', '#F57C00', brand), color('Rosa', '#EE7FA8', brand),
  color('Morado', '#7652A5', brand), color('Marrón', '#795548', brand), color('Natural', '#E7D9BD', brand),
  color('Transparente', '#DDEBF2', brand), color('Dorado', '#C9A227', brand), color('Plateado', '#B7BDC3', brand),
]
const extended = (brand: string) => [...basic(brand), color('Azul cielo', '#55BDEB', brand), color('Azul marino', '#173F73', brand), color('Turquesa', '#27B8AE', brand), color('Verde lima', '#8BC34A', brand), color('Verde oliva', '#7A8531', brand), color('Beige', '#D9C6A5', brand), color('Crema', '#EFE1BC', brand), color('Magenta', '#D93691', brand), color('Vino', '#8E2430', brand)]

/** Catálogo referencial y extensible. Los campos de la UI conservan entrada libre para nuevas líneas y colores. */
export const materialBrands: MaterialBrand[] = [
  { brand: 'Bambu Lab', filamentTypes: ['PLA Basic', 'PLA Matte', 'PLA Tough+', 'PLA Silk+', 'PLA Metal', 'PLA Marble', 'PLA Sparkle', 'PLA Galaxy', 'PLA Wood', 'PLA Aero', 'PLA-CF', 'PETG HF', 'PETG Basic', 'PETG Translucent', 'PETG-CF', 'ABS', 'ASA', 'ASA Aero', 'TPU 85A', 'TPU 90A', 'TPU 95A HF', 'TPU for AMS', 'PA6-CF', 'PA6-GF', 'PAHT-CF', 'PPA-CF', 'PPA-GF', 'PC', 'PC FR', 'PVA', 'Support for PLA/PETG', 'Support for PA/PET'], colors: extended('Bambu Lab') },
  { brand: 'eSUN', filamentTypes: ['ePLA-Basic', 'ePLA+', 'ePLA+ HS', 'ePLA-HF', 'ePLA-ST', 'ePLA-Lite', 'ePLA-Matte', 'ePLA-Silk', 'ePLA-Magic', 'ePLA-Metal', 'ePLA-Wood', 'ePLA-CF', 'ePETG Basic', 'ePETG+ HS', 'ePETG-CF', 'eABS', 'ABS+', 'eASA', 'eASA-CF', 'eTPU-95A', 'eTPU-HS', 'eTPU-LW', 'ePA', 'ePA-CF', 'ePAHT-CF', 'ePC', 'ePC-CF', 'HIPS', 'PVA'], resinTypes: ['Standard Resin', 'Standard Resin 2.0', 'Water Washable Resin', 'Water Washable Resin 2.0', 'ABS-Like Resin', 'ABS-Like Resin 3.0', 'Hard-Tough Resin', 'Flexible Resin', 'High Temperature Resin', 'Dental Model Resin', 'Dental Castable Resin', 'Jewelry Castable Resin'], colors: extended('eSUN') },
  { brand: 'SUNLU', filamentTypes: ['PLA', 'PLA+', 'PLA 2.0', 'PLA Meta', 'High Speed PLA', 'Silk PLA', 'Silk Rainbow PLA', 'Matte PLA', 'Marble PLA', 'Wood PLA', 'Glow PLA', 'PETG', 'High Speed PETG', 'PETG-CF', 'ABS', 'ABS+', 'ASA', 'TPU', 'TPU 95A', 'PA', 'PA-CF', 'PC', 'PVA'], resinTypes: ['Standard Resin', 'Standard Resin Plus', 'ABS-Like Resin', 'ABS-Like Resin Pro', 'Water-Washable Resin', 'Toughness Resin', 'High Temperature Resin', 'Nylon-Like Resin', 'Plant-Based Resin', 'Castable Resin'], colors: extended('SUNLU') },
  { brand: 'Polymaker', filamentTypes: ['PolyLite PLA', 'PolyLite PLA Pro', 'PolyTerra PLA', 'Panchroma PLA Basic', 'Panchroma PLA Matte', 'Panchroma PLA Satin', 'Panchroma PLA Silk', 'Panchroma PLA Dual', 'Panchroma PLA Gradient', 'PolyLite PETG', 'PolyLite ABS', 'PolyLite ASA', 'PolyMax PLA', 'PolyMax PETG', 'PolyMax PC', 'PolyFlex TPU90', 'PolyFlex TPU95', 'PolyMide CoPA', 'PolyMide PA6-CF', 'PolyMide PA6-GF', 'Fiberon PET-CF17', 'Fiberon PA6-CF20', 'PolyDissolve S1'], colors: extended('Polymaker') },
  { brand: 'Creality', filamentTypes: ['Hyper PLA', 'Hyper PLA RFID', 'Hyper PLA-CF', 'Hyper PETG', 'Ender PLA', 'CR-PLA', 'CR-PLA Matte', 'CR-Silk PLA', 'Rainbow PLA', 'Wood PLA', 'PETG', 'PETG-CF', 'ABS', 'ABS-CF', 'ASA', 'TPU', 'PA', 'PA-CF', 'PPA-CF'], resinTypes: ['Standard Plus Resin', 'Standard Resin 2.0', 'ABS-Like Resin Plus', 'Water-Washable Resin Plus', 'High Precision Resin', 'Tough Resin', 'High Temperature Resin', 'Dental Model Resin', 'Castable Resin'], colors: basic('Creality') },
  { brand: 'ELEGOO', filamentTypes: ['PLA', 'PLA+', 'Rapid PLA+', 'Rapid PLA+ 2.0', 'Matte PLA', 'Silk PLA', 'Dual Color Silk PLA', 'Tri-Color Silk PLA', 'Galaxy PLA', 'Wood PLA', 'Rapid PETG', 'PETG Pro', 'ASA', 'ABS', 'TPU 95A', 'PA-CF'], resinTypes: ['Standard Resin', 'Standard Resin 2.0', 'Standard Resin 3.0', 'ABS-Like Resin', 'ABS-Like Resin 2.0', 'ABS-Like Resin 3.0', 'Water-Washable Resin', 'Water-Washable Resin 2.0', 'Plant-Based Resin', '8K Standard Resin', 'Tough Resin', 'Flexible Resin', 'High Temperature Resin', 'Dental Model Resin'], colors: extended('ELEGOO') },
  { brand: 'Anycubic', filamentTypes: ['PLA Basic', 'PLA+', 'High Speed PLA', 'High Speed PLA+', 'Matte PLA', 'Silk PLA', 'Dual Color Silk PLA', 'Metallic PLA', 'Wood PLA', 'PETG', 'High Speed PETG', 'ABS', 'ASA', 'TPU', 'TPU 95A', 'PA6-CF'], resinTypes: ['Standard Resin', 'Standard Resin+', 'Standard Resin V2', 'ABS-Like Resin Pro 2', 'ABS-Like Resin V2', 'Water-Wash Resin 2.0', 'High Clear Resin', 'Tough Resin 2.0', 'Plant-Based Resin', 'DLP Craftsman Resin', 'High Speed Resin 2.0', 'Flexible Tough Resin', 'Castable Resin'], colors: extended('Anycubic') },
  { brand: 'Overture', filamentTypes: ['PLA', 'PLA Professional', 'Matte PLA', 'Silk PLA', 'Super PLA+', 'PETG', 'ABS', 'ASA', 'TPU', 'Easy Nylon', 'Rock PLA'], colors: extended('Overture') },
  { brand: 'HATCHBOX', filamentTypes: ['PLA', 'Matte PLA', 'Silk PLA', 'PETG', 'ABS', 'ASA', 'TPU', 'Wood PLA', 'Metallic PLA', 'Glow PLA'], colors: extended('HATCHBOX') },
  { brand: 'Prusament', filamentTypes: ['PLA', 'PETG', 'ASA', 'PC Blend', 'PC Blend Carbon Fiber', 'PA11 Carbon Fiber', 'PETG Tungsten', 'PVB', 'TPU 95A'], resinTypes: ['Model Resin', 'Tough Resin', 'Flexible Resin', 'BioBased Resin'], colors: extended('Prusament') },
  { brand: 'Fiberlogy', filamentTypes: ['Easy PLA', 'Easy PET-G', 'FiberSilk', 'Matte PLA', 'ABS', 'ASA', 'FiberFlex 30D', 'FiberFlex 40D', 'PA12+CF15', 'CPE HT', 'PCTG', 'BVOH'], colors: extended('Fiberlogy') },
  { brand: 'Spectrum', filamentTypes: ['PLA Premium', 'PLA Pro', 'PLA Silk', 'PLA Matte', 'PET-G Premium', 'PET-G HT100', 'ABS GP450', 'ASA 275', 'S-Flex 90A', 'PA6 Low Warp CF15S', 'PC 275'], colors: extended('Spectrum') },
  { brand: 'colorFabb', filamentTypes: ['PLA/PHA', 'LW-PLA', 'nGen', 'XT', 'HT', 'VarioShore TPU', 'WoodFill', 'BronzeFill', 'CopperFill', 'SteelFill', 'PA-CF Low Warp'], colors: extended('colorFabb') },
  { brand: 'FormFutura', filamentTypes: ['EasyFil PLA', 'ReForm rPLA', 'ApolloX ASA', 'TitanX ABS', 'HDglass PETG', 'Python Flex', 'CarbonFil', 'StoneFil', 'MetalFil', 'AquaSolve PVA'], resinTypes: ['Spectrum LCD Resin', 'Engineering LCD Resin', 'Castable LCD Resin'], colors: extended('FormFutura') },
  { brand: 'MatterHackers', filamentTypes: ['Build Series PLA', 'Build Series PETG', 'PRO Series PLA', 'PRO Series ABS', 'NylonX', 'NylonG', 'Quantum PLA', 'MH Build TPU'], colors: extended('MatterHackers') },
  { brand: 'Flashforge', filamentTypes: ['PLA', 'PLA Pro', 'HS PLA', 'PETG', 'ABS Pro', 'ASA', 'TPU 95A', 'PA-CF', 'PET-CF'], resinTypes: ['Standard Resin', 'Water Washable Resin', 'ABS-Like Resin', 'Tough Resin'], colors: basic('Flashforge') },
  { brand: 'Sovol', filamentTypes: ['PLA', 'PLA+', 'Silk PLA', 'PETG', 'ABS', 'ASA', 'TPU'], resinTypes: ['Standard Resin', 'ABS-Like Resin', 'Water-Washable Resin'], colors: basic('Sovol') },
  { brand: 'GEEETECH', filamentTypes: ['PLA', 'Silk PLA', 'Rainbow PLA', 'PETG', 'ABS', 'TPU', 'Wood PLA'], resinTypes: ['Standard Resin', 'Water Washable Resin'], colors: extended('GEEETECH') },
  { brand: 'AMOLEN', filamentTypes: ['Silk PLA', 'Dual Color Silk PLA', 'Tri-Color Silk PLA', 'Rainbow PLA', 'Glow PLA', 'Wood PLA', 'Marble PLA', 'Metal PLA'], colors: extended('AMOLEN') },
  { brand: 'Proto-pasta', filamentTypes: ['HTPLA', 'Matte Fiber HTPLA', 'Composite PLA', 'Conductive PLA', 'Stainless Steel PLA', 'Magnetic Iron PLA', 'Carbon Fiber PLA'], colors: extended('Proto-pasta') },
  { brand: 'Recreus', filamentTypes: ['Filaflex 60A', 'Filaflex 70A', 'Filaflex 82A', 'Filaflex 95A', 'PLA-LW', 'PP3D', 'PETG'], colors: extended('Recreus') },
  { brand: 'NinjaTek', filamentTypes: ['NinjaFlex 85A', 'Cheetah 95A', 'Armadillo 75D', 'Eel Conductive TPU', 'Chinchilla 75A'], colors: basic('NinjaTek') },
  { brand: 'Raise3D', filamentTypes: ['Premium PLA', 'Premium PETG', 'Premium ABS', 'Premium ASA', 'Industrial PA12 CF+', 'Industrial PET CF', 'Industrial PPA CF', 'Hyper Core ABS CF'], colors: basic('Raise3D') },
  { brand: 'Siraya Tech', resinTypes: ['Fast ABS-Like', 'Build', 'Blu Tough', 'Tenacious Flexible', 'Sculpt High Temperature', 'Simple Water-Washable', 'Cast Castable', 'Craft Ultra Clear'], colors: basic('Siraya Tech') },
  { brand: 'Phrozen', resinTypes: ['Aqua Gray 8K', 'Aqua Hyperfine', 'Aqua 4K', 'Water-Washable', 'TR250-LV High Temp', 'Onyx Impact Plus', 'Protowhite Rigid', 'Castable W40'], colors: basic('Phrozen') },
  { brand: 'RESIONE', resinTypes: ['M58 Tough', 'K Tough', 'TH72 Flexible-Tough', 'F69 Flexible', 'G217 ABS-Like', 'Anti-Impact', 'High Detail', 'Water-Washable'], colors: basic('RESIONE') },
  { brand: 'JAMG HE', resinTypes: ['Standard Resin', 'ABS-Like Resin', 'Water-Washable Resin', 'Tough Resin', 'High Precision Resin', 'Castable Resin'], colors: basic('JAMG HE') },
  { brand: 'AmeraLabs', resinTypes: ['TGM-7', 'AMD-3 LED', 'XV-N Black', 'DMD-21', 'Benzene'], colors: basic('AmeraLabs') },
  { brand: 'Liqcreate', resinTypes: ['Premium Model', 'Premium Tough', 'Strong-X', 'Tough-X', 'Flexible-X', 'Clear Impact', 'Wax Castable', 'Bio-Med Clear'], colors: basic('Liqcreate') },
  { brand: 'Formlabs', resinTypes: ['General Purpose Resin', 'Clear Resin', 'Tough 2000 Resin', 'Durable Resin', 'Flexible 80A Resin', 'High Temp Resin', 'Castable Wax Resin', 'Dental Model Resin'], colors: basic('Formlabs') },
]

export const brandsFor = (category: MaterialCategory) => materialBrands.filter(item => category === 'Filament' ? item.filamentTypes?.length : item.resinTypes?.length)
export const typesFor = (brand: string, category: MaterialCategory) => {
  const entry = materialBrands.find(item => item.brand.toLowerCase() === brand.trim().toLowerCase())
  return category === 'Filament' ? entry?.filamentTypes ?? [] : entry?.resinTypes ?? []
}
export const colorsFor = (brand: string) => materialBrands.find(item => item.brand.toLowerCase() === brand.trim().toLowerCase())?.colors ?? []
