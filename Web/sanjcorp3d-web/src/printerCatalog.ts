export type PrinterTechnology = 'FDM/FFF' | 'Resina MSLA/SLA' | 'SLS' | 'Industrial'
export type PrinterCatalogEntry = { brand: string; model: string; technology: PrinterTechnology }

const entries = (brand: string, technology: PrinterTechnology, models: string[]) => models.map(model => ({ brand, model, technology }))

/** Catálogo comercial extensible. Los formularios siempre permiten escribir marcas y modelos nuevos. */
export const printerCatalog: PrinterCatalogEntry[] = [
  ...entries('Bambu Lab', 'FDM/FFF', ['H2D', 'H2D Pro', 'H2S', 'P2S', 'X1 Carbon', 'X1E', 'P1S', 'P1P', 'A1', 'A1 mini']),
  ...entries('Creality', 'FDM/FFF', ['K3', 'K2 Plus', 'K2 Pro', 'K2', 'K2 SE', 'K1 Max', 'K1C 2025', 'K1', 'SPARKX i7', 'SPARKX i7 Nano', 'Ender-5 Max', 'Ender-3 V3 Mega', 'Ender-3 V3 Plus', 'Ender-3 V3 KE', 'Ender-3 V3 SE', 'Ender-3 V3', 'Creality Hi', 'CR-30']),
  ...entries('Creality', 'Resina MSLA/SLA', ['HALOT-X1', 'HALOT-X1 Neo', 'HALOT-MAGE S', 'HALOT-MAGE PRO', 'HALOT-ONE PRO']),
  ...entries('Original Prusa', 'FDM/FFF', ['CORE One+ (Gen 2)', 'CORE One L+', 'XL+', 'MK4S', 'MINI+', 'Pro HT90', 'Pro AFS']),
  ...entries('Original Prusa', 'Resina MSLA/SLA', ['SL1S SPEED', 'Pro SLX']),
  ...entries('Anycubic', 'FDM/FFF', ['Kobra X', 'Kobra X Combo', 'Kobra S1 Max', 'Kobra S1 Max Combo', 'Kobra S1', 'Kobra S1 Combo', 'Kobra 3 V2', 'Kobra 3 Combo', 'Kobra 2 Max', 'Kobra 2 Pro', 'Kobra 2 Neo']),
  ...entries('Anycubic', 'Resina MSLA/SLA', ['Photon Mono M7 Max', 'Photon Mono M7 Pro', 'Photon Mono M7', 'Photon Mono 4 Ultra', 'Photon Mono 4', 'Photon D2']),
  ...entries('ELEGOO', 'FDM/FFF', ['Centauri 2', 'Centauri 2 Combo', 'Centauri Carbon 2 Combo', 'Centauri Carbon', 'OrangeStorm Giga', 'Neptune 4 Max', 'Neptune 4 Plus', 'Neptune 4 Pro', 'Neptune 4']),
  ...entries('ELEGOO', 'Resina MSLA/SLA', ['Jupiter 2', 'Saturn 4 Ultra 16K', 'Saturn 4 Ultra', 'Saturn 4', 'Mars 5 Ultra', 'Mars 5', 'Mars 4 Ultra']),
  ...entries('QIDI Tech', 'FDM/FFF', ['Q3D', 'Plus5', 'Max4', 'Q2', 'Q2C', 'Plus4', 'Q1 Pro', 'X-Max 3', 'X-Plus 3', 'X-Smart 3']),
  ...entries('Flashforge', 'FDM/FFF', ['CJ270', 'Creator 5 Pro', 'Creator 5', 'AD5X', 'Adventurer 5M Pro', 'Adventurer 5M', 'Guider 3 Ultra', 'Guider 3 Plus', 'Creator 4', 'Finder 3']),
  ...entries('Phrozen', 'Resina MSLA/SLA', ['Sonic Mighty Revo MAX', 'Sonic Mighty Revo 16K', 'Sonic Mighty Revo', 'Sonic Mighty 12K', 'Sonic Mini 8K S', 'Sonic Mini 8K', 'Sonic Mega 8K V2', 'Sonic Mega 8K S']),
  ...entries('Formlabs', 'Resina MSLA/SLA', ['Form 4L', 'Form 4', 'Form 4B', 'Form 3L', 'Form 3BL', 'Form 3+', 'Form 3B+']),
  ...entries('Formlabs', 'SLS', ['Fuse 1+ 30W', 'Fuse 1']),
  ...entries('Raise3D', 'FDM/FFF', ['Pro3 Plus HS', 'Pro3 HS', 'Pro3 Plus', 'Pro3', 'E2CF', 'E2']),
  ...entries('Raise3D', 'Industrial', ['RMF500', 'Metalfuse Forge1']),
  ...entries('UltiMaker', 'FDM/FFF', ['S8', 'Factor 4', 'S7', 'S5', 'S3', '2+ Connect', 'Method XL', 'Method X', 'Sketch Sprint']),
  ...entries('Snapmaker', 'FDM/FFF', ['U1', 'Artisan', 'J1s IDEX', 'Ray', '2.0 A350T', '2.0 A250T']),
  ...entries('Sovol', 'FDM/FFF', ['SV08 Max', 'SV08', 'SV07 Plus', 'SV07', 'SV06 ACE', 'SV06 Plus ACE', 'SV06 Plus', 'SV06', 'SV04 IDEX']),
  ...entries('FLSUN', 'FDM/FFF', ['S1 Pro', 'S1', 'T1 Pro', 'T1', 'V400', 'Super Racer']),
  ...entries('AnkerMake', 'FDM/FFF', ['M5C', 'M5']),
  ...entries('Artillery', 'FDM/FFF', ['Sidewinder X4 Plus', 'Sidewinder X4 Pro', 'Sidewinder X3 Plus', 'Sidewinder X3 Pro', 'Genius Pro', 'Hornet']),
  ...entries('MINGDA', 'FDM/FFF', ['MD-1000D', 'MD-600D', 'MD-400D', 'Magician Pro 2', 'Magician X2']),
  ...entries('Tronxy', 'FDM/FFF', ['VEHO 1000-16', 'VEHO 800', 'VEHO 600 Pro V2', 'X5SA-500 Pro', 'X5SA Pro', 'Moore 3 Pro']),
  ...entries('Kingroon', 'FDM/FFF', ['KLP1', 'KP3S Pro V2', 'KP5L', 'KP3S 3.0']),
  ...entries('GEEETECH', 'FDM/FFF', ['Thunder', 'Mizar S', 'Mizar Pro', 'A30M', 'A20T', 'A10M']),
  ...entries('LulzBot', 'FDM/FFF', ['TAZ Pro Long Bed', 'TAZ Pro S', 'TAZ Workhorse+', 'SideKick 747', 'Mini 3']),
  ...entries('BCN3D', 'FDM/FFF', ['Omega I60', 'Epsilon W50 SC', 'Epsilon W27 SC', 'Sigma D25']),
  ...entries('Zortrax', 'FDM/FFF', ['M300 Dual', 'M300 Plus', 'M200 Plus', 'Endureal', 'Inventure']),
  ...entries('Zortrax', 'Resina MSLA/SLA', ['Inkspire 2', 'Inkspire']),
  ...entries('Craftbot', 'FDM/FFF', ['Flow IDEX XL', 'Flow IDEX', 'Flow XL', 'Plus Pro', 'Pro XL']),
  ...entries('Dremel', 'FDM/FFF', ['DigiLab 3D45', 'DigiLab 3D40 Flex', 'DigiLab 3D20']),
  ...entries('Monoprice', 'FDM/FFF', ['Voxel', 'Maker Select Plus', 'Mini Delta V2', 'Select Mini V2']),
  ...entries('Peopoly', 'Resina MSLA/SLA', ['Phenom Forge', 'Phenom XXL V2', 'Phenom Prime', 'Forge', 'Moai 200']),
  ...entries('Peopoly', 'FDM/FFF', ['Magneto X']),
  ...entries('Uniformation', 'Resina MSLA/SLA', ['GK3 Ultra', 'GK3 Pro', 'GK3', 'GKtwo', 'GKone']),
  ...entries('HeyGears', 'Resina MSLA/SLA', ['Reflex 2 Pro', 'Reflex 2', 'Reflex RS Turbo', 'Reflex RS', 'UltraCraft Reflex']),
  ...entries('Asiga', 'Industrial', ['MAX 2', 'MAX X', 'PRO 4K45', 'PRO 4K65', 'Ultra']),
  ...entries('Markforged', 'Industrial', ['FX10', 'FX20', 'X7', 'X3', 'Onyx Pro', 'Mark Two']),
  ...entries('Stratasys', 'Industrial', ['F170', 'F370CR', 'F770', 'F900', 'J35 Pro', 'J55 Prime', 'J850 Prime', 'Origin Two']),
  ...entries('3D Systems', 'Industrial', ['Figure 4', 'Figure 4 Modular', 'ProJet MJP 2500 Plus', 'ProJet 7000 HD', 'SLS 380', 'EXT 1270 Titan Pellet']),
  ...entries('Voron Design', 'FDM/FFF', ['Voron 0.2', 'Voron Trident', 'Voron 2.4', 'Voron Switchwire', 'Voron Legacy']),
  ...entries('Rat Rig', 'FDM/FFF', ['V-Core 4.1', 'V-Core 4', 'V-Minion', 'V-Cast IDEX']),
]

export const printerBrands = [...new Set(printerCatalog.map(item => item.brand))].sort((a, b) => a.localeCompare(b))
export const printerTechnologies = [...new Set(printerCatalog.map(item => item.technology))]
export const printerModelsFor = (brand: string, technology = '') => printerCatalog.filter(item => (!brand || item.brand === brand) && (!technology || item.technology === technology))
export const printerCatalogLabel = (item: PrinterCatalogEntry) => `${item.brand} ${item.model}`
