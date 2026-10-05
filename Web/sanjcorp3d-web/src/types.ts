export type Profile = {
  id: string
  userName: string
  email?: string
  displayName: string
  profilePhotoUrl?: string
  isMakerOwner?: boolean
  twoFactorEnabled: boolean
  roles: string[]
  tenantId?: string
  tenantName?: string
  tenantKind?: string
  logoUrl?: string
  isSuperAdmin?: boolean
  workspaces?: Array<{ id: string; name: string; slug: string; kind: string; logoUrl?: string; active: boolean }>
}

export type Dashboard = { quotes: number; salesThisMonth: number; revenueThisMonth: number; lowStock: number }

export type Printer = {
  id: number; name: string; buildX: number; buildY: number; buildZ: number; nozzle: number
  speed: number; powerWatts: number; hourlyCost: number; colorCount: number; status: string; isDefault: boolean; active: boolean
}

export type Consumable = {
  id: number; name: string; category: string; material: string; color: string; pricePerUnit: number
  density: number; isDefault: boolean; active: boolean; stockQuantity: number; stockGrams: number; lowStockGrams: number
}

export type InventoryAlert = {
  id: number; name: string; category: string; material: string; color: string
  stockGrams: number; lowStockGrams: number; severity: 'out' | 'low'; message: string
}

export type ExtraMaterial = { id: number; name: string; category: string; unit: string; unitPrice: number; active: boolean }
export type ExtraMaterialCategory = { id: number; name: string; active: boolean }

export type BusinessSettings = {
  businessName: string; currencyName: string; currencySymbol: string; electricityPerKwh: number
  maintenancePerPrint: number; maintenancePercent: number; preparationPercent: number; laborPercent: number; wastePercent: number; overheadPercent: number; packagingCost: number; transportCost: number; defaultProfitMultiplier: number; taxPercent: number; roundTo: number; decimalPlaces: number
}

export type ConsumableUsage = { consumableId: number; grams: number }
export type MaterialUsage = { materialId: number; quantity: number }

export type QuoteRequest = {
  customer: string; customerPhone?: string; projectName: string; productName?: string; printerId: number; printHours: number; quantity: number
  additionalManualCost: number; profitMultiplier: number; notes: string
  consumables: ConsumableUsage[]; materials: MaterialUsage[]; maintenancePercent?: number; preparationPercent?: number; laborPercent?: number; wastePercent?: number; overheadPercent?: number; packagingCost?: number; transportCost?: number
}

export type QuoteCalculation = {
  totalWeight: number; materialCost: number; electricityCost: number; maintenanceCost: number
  preparationCost: number; laborCost: number; wasteCost: number; overheadCost: number; packagingCost: number; transportCost: number
  additionalCost: number; subtotal: number; profitAmount: number; taxAmount: number; recommendedPrice: number
}

export type QuoteSummary = {
  id: number; orderCode: string; createdAtUtc: string; customer: string; projectName: string; productName?: string
  printerName: string; totalWeight: number; costTotal: number; recommendedPrice: number; soldAtUtc?: string
}

export type QuoteDetail = {
  id: number; orderCode: string; createdAtUtc: string; customer: string; customerPhone?: string; projectName: string; productName?: string; printerName: string
  printHours: number; quantity: number; additionalManualCost: number; profitMultiplier: number; notes: string
  totalWeight: number; materialCost: number; electricityCost: number; machineCost: number; maintenanceCost: number
  laborCost: number; additionalCost: number; functionalSurcharge: number; subtotal: number; profitAmount: number
  taxAmount: number; recommendedPrice: number
  consumables: Array<{ id: number; legacyConsumableId: number; name: string; category: string; material: string; color: string; grams: number; pricePerUnit: number; density: number; lineCost: number }>
  materials: Array<{ id: number; legacyMaterialId: number; name: string; quantity: number; unitPrice: number; lineCost: number }>
  sale?: { id: number; soldAtUtc: string; saleAmount: number }
}

export type Report = {
  quoteCount: number; totalCost: number; projectedRevenue: number; projectedProfit: number
  saleCount: number; salesRevenue: number; salesProfit: number; sales: SaleSummary[]
  consumableDistribution: Array<{ name: string; grams: number }>; costDistribution: Array<{ name: string; value: number }>
  productDistribution: Array<{ name: string; quantity: number; revenue: number }>; lossDistribution: Array<{ name: string; grams: number }>
}

export type SaleSummary = {
  id: number; quoteId: number; orderCode: string; soldAtUtc: string; customer: string
  projectName: string; costTotal: number; saleAmount: number; profit: number
}

export type UserAccount = {
  id: string; userName: string; displayName: string; profilePhotoUrl?: string; email?: string; active: boolean; createdAtUtc: string
  lastLoginAtUtc?: string; twoFactorEnabled: boolean; role: string
}
export type Tenant = { id: string; name: string; slug: string; kind: string; logoUrl?: string; active: boolean; userCount: number }
export type ChatMessage = { id: number; tenantId: string; senderUserId: string; senderName: string; body: string; photoUrl?: string; createdAtUtc: string }
export type ProductCatalog = { id: number; name: string; description: string; materialType: string; filamentGrams: number; materialCost: number; productionMinutes: number; profitMultiplier: number; active: boolean }
export type StoreQuoteLine = { productId: number; quantity: number; materialType?: string }
export type StoreQuoteRequest = { customer: string; customerPhone?: string; projectName?: string; printerIds: number[]; notes?: string; lines: StoreQuoteLine[] }
export type StoreQuoteCalculation = { totalQuantity: number; printHours: number; materialCost: number; electricityCost: number; maintenanceCost: number; preparationCost: number; laborCost: number; wasteCost: number; overheadCost: number; packagingCost: number; transportCost: number; subtotal: number; profitAmount: number; taxAmount: number; recommendedPrice: number }
export type ConsumableMaterialType = { id: number; name: string; active: boolean }

export type TwoFactorSetup = { sharedKey: string; authenticatorUri: string }

export type PrintOrder = {
  id: number; quoteId: number; orderCode: string; customer: string; projectName: string; productName?: string
  printerId: number; printerName: string; printerStatus: string; status: 'Pendiente' | 'Imprimiendo' | 'Enfriamiento' | 'Terminado'
  createdAtUtc: string; startedAtUtc?: string; estimatedFinishedAtUtc?: string; finishedAtUtc?: string; coolingUntilUtc?: string; completedAtUtc?: string
  printHours: number; quantity: number; recommendedPrice: number
}
