import type {
  BusinessSettings, Consumable, Dashboard, ExtraMaterial, ExtraMaterialCategory, Printer, Profile, QuoteCalculation,
  QuoteDetail, QuoteRequest, QuoteSummary, Report, TwoFactorSetup, UserAccount, InventoryAlert, Tenant, ChatMessage, ProductCatalog, ConsumableMaterialType, PrintOrder, StoreQuoteRequest, StoreQuoteCalculation,
} from './types'

export type { Dashboard, Profile } from './types'

type ApiError = Error & { status: number; requiresTwoFactor?: boolean }
type ApiErrorBody = {
  message?: string
  title?: string
  detail?: string
  requiresTwoFactor?: boolean
  errors?: Record<string, string[] | string>
}

const API_BASE_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/+$/, '')
const apiUrl = (path: string) => `${API_BASE_URL}${path}`

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const tenant = sessionStorage.getItem('sanjcorp.tenant')
  const response = await fetch(apiUrl(path), {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(tenant ? { 'X-Tenant-Id': tenant } : {}), ...options?.headers },
    ...options,
  })
  if (!response.ok) {
    const body = await readErrorBody(response)
    const error = new Error(getErrorMessage(response, body)) as ApiError
    error.status = response.status
    error.requiresTwoFactor = body.requiresTwoFactor
    throw error
  }
  return response.status === 204 ? undefined as T : response.json()
}

async function readErrorBody(response: Response): Promise<ApiErrorBody> {
  const text = await response.text().catch(() => '')
  if (!text) return {}

  try {
    return JSON.parse(text) as ApiErrorBody
  } catch {
    return { message: text }
  }
}

function getErrorMessage(response: Response, body: ApiErrorBody) {
  const validationMessage = body.errors
    ? Object.values(body.errors).flat().filter(Boolean).join(' ')
    : ''

  return body.message
    ?? validationMessage
    ?? body.detail
    ?? body.title
    ?? (response.status >= 500 ? 'El servidor no pudo completar la operacion. Revisa los registros de la API.' : 'No se pudo completar la operacion.')
}

function query(values: Record<string, string | number | boolean | undefined>) {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value))
  })
  const text = params.toString()
  return text ? `?${text}` : ''
}

async function download(path: string, fallbackName: string) {
  const tenant = sessionStorage.getItem('sanjcorp.tenant')
  const response = await fetch(apiUrl(path), { credentials: 'include', headers: tenant ? { 'X-Tenant-Id': tenant } : {} })
  if (!response.ok) throw new Error('No se pudo descargar el archivo.')
  const disposition = response.headers.get('content-disposition') ?? ''
  const fileName = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i)?.[1] ?? fallbackName
  const url = URL.createObjectURL(await response.blob())
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = decodeURIComponent(fileName.replaceAll('"', ''))
  anchor.click()
  URL.revokeObjectURL(url)
}

export const api = {
  me: () => request<Profile>('/api/auth/me'),
  login: (username: string, password: string, twoFactorCode?: string, workspace = 'technology') => request<Profile>('/api/auth/login', { method: 'POST', body: JSON.stringify({ username, password, twoFactorCode, workspace }) }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  setupTwoFactor: () => request<TwoFactorSetup>('/api/auth/2fa/setup', { method: 'POST' }),
  enableTwoFactor: (code: string) => request<{ enabled: boolean; recoveryCodes: string[] }>('/api/auth/2fa/enable', { method: 'POST', body: JSON.stringify({ code }) }),
  disableTwoFactor: () => request<void>('/api/auth/2fa/disable', { method: 'POST' }),
  dashboard: () => request<Dashboard>('/api/dashboard'),
  printers: (includeArchived = false) => request<Printer[]>(`/api/printers${query({ includeArchived })}`),
  savePrinter: (item: Printer) => item.id ? request<Printer>(`/api/printers/${item.id}`, { method: 'PUT', body: JSON.stringify(item) }) : request<Printer>('/api/printers', { method: 'POST', body: JSON.stringify(item) }),
  archivePrinter: (id: number) => request<void>(`/api/printers/${id}`, { method: 'DELETE' }),
  favoritePrinter: (id: number, favorite: boolean) => request<Printer>(`/api/printers/${id}/favorite`, { method: 'PUT', body: JSON.stringify({ favorite }) }),
  consumables: (includeArchived = false) => request<Consumable[]>(`/api/consumables${query({ includeArchived })}`),
  saveConsumable: (item: Consumable) => item.id ? request<Consumable>(`/api/consumables/${item.id}`, { method: 'PUT', body: JSON.stringify(item) }) : request<Consumable>('/api/consumables', { method: 'POST', body: JSON.stringify(item) }),
  updateStock: (id: number, stockGrams: number, lowStockGrams: number) => request<Consumable>(`/api/consumables/${id}/stock`, { method: 'PATCH', body: JSON.stringify({ stockGrams, lowStockGrams }) }),
  addStock: (id: number, kilograms: number, grams: number, pricePerKilogram: number) => request<Consumable>(`/api/consumables/${id}/stock/add`, { method: 'POST', body: JSON.stringify({ kilograms, grams, pricePerKilogram }) }),
  registerLoss: (id: number, grams: number, reason: string) => request<Consumable>(`/api/consumables/${id}/loss`, { method: 'POST', body: JSON.stringify({ grams, reason }) }),
  archiveConsumable: (id: number) => request<void>(`/api/consumables/${id}`, { method: 'DELETE' }),
  alerts: () => request<InventoryAlert[]>('/api/alerts'),
  materialTypes: (includeArchived = false) => request<ConsumableMaterialType[]>('/api/consumable-material-types' + query({ includeArchived })),
  createMaterialType: (name: string) => request<ConsumableMaterialType>('/api/consumable-material-types', { method: 'POST', body: JSON.stringify({ name }) }),
  updateMaterialType: (id: number, name: string) => request<ConsumableMaterialType>('/api/consumable-material-types/' + id, { method: 'PATCH', body: JSON.stringify({ name }) }),
  archiveMaterialType: (id: number) => request<void>('/api/consumable-material-types/' + id, { method: 'DELETE' }),
  extraMaterialCategories: (includeArchived = false) => request<ExtraMaterialCategory[]>('/api/extra-material-categories' + query({ includeArchived })),
  createExtraMaterialCategory: (name: string) => request<ExtraMaterialCategory>('/api/extra-material-categories', { method: 'POST', body: JSON.stringify({ name }) }),
  updateExtraMaterialCategory: (id: number, name: string) => request<ExtraMaterialCategory>('/api/extra-material-categories/' + id, { method: 'PATCH', body: JSON.stringify({ name }) }),
  archiveExtraMaterialCategory: (id: number) => request<void>('/api/extra-material-categories/' + id, { method: 'DELETE' }),
  materials: (includeArchived = false) => request<ExtraMaterial[]>('/api/materials' + query({ includeArchived })),
  saveMaterial: (item: ExtraMaterial) => item.id ? request<ExtraMaterial>(`/api/materials/${item.id}`, { method: 'PUT', body: JSON.stringify(item) }) : request<ExtraMaterial>('/api/materials', { method: 'POST', body: JSON.stringify(item) }),
  archiveMaterial: (id: number) => request<void>(`/api/materials/${id}`, { method: 'DELETE' }),
  settings: () => request<BusinessSettings>('/api/settings'),
  saveSettings: (settings: BusinessSettings) => request<BusinessSettings>('/api/settings', { method: 'PUT', body: JSON.stringify(settings) }),
  calculateQuote: (quote: QuoteRequest) => request<QuoteCalculation>('/api/quotes/calculate', { method: 'POST', body: JSON.stringify(quote) }),
  createQuote: (quote: QuoteRequest) => request<QuoteSummary>('/api/quotes', { method: 'POST', body: JSON.stringify(quote) }),
  updateQuotePrice: (id: number, price: number) => request<{ id: number; recommendedPrice: number; profitAmount: number }>(`/api/quotes/${id}/price`, { method: 'PATCH', body: JSON.stringify({ price }) }),
  copyQuote: (id: number) => request<QuoteSummary>(`/api/quotes/${id}/copy`, { method: 'POST' }),
  downloadVoucher: (id: number) => download(`/api/quotes/${id}/voucher`, `cotización-${id}.pdf`),
  quotes: (filters: { search?: string; from?: string; to?: string } = {}) => request<QuoteSummary[]>(`/api/quotes${query(filters)}`),
  quote: (id: number) => request<QuoteDetail>(`/api/quotes/${id}`),
  confirmSale: (id: number) => request<{ id: number; quoteId: number; soldAtUtc: string; saleAmount: number }>(`/api/quotes/${id}/sale`, { method: 'POST' }),
  deleteQuote: (id: number) => request<void>(`/api/quotes/${id}`, { method: 'DELETE' }),
  exportQuotes: (filters: { search?: string; from?: string; to?: string }) => download(`/api/quotes/export${query(filters)}`, 'cotizaciones.csv'),
  report: (from: string, to: string, userId?: string) => request<Report>(`/api/reports${query({ from, to, userId })}`),
  exportSales: (from: string, to: string) => download(`/api/reports/sales/export${query({ from, to })}`, 'ventas.csv'),
  users: () => request<UserAccount[]>('/api/users'),
  roles: () => request<string[]>('/api/users/roles'),
  createUser: (item: { username: string; displayName: string; email?: string; password: string; role: string; profilePhotoUrl?: string }) => request<UserAccount>('/api/users', { method: 'POST', body: JSON.stringify(item) }),
  updateUser: (id: string, item: { displayName: string; email?: string; role: string; active: boolean }) => request<UserAccount>(`/api/users/${id}`, { method: 'PUT', body: JSON.stringify(item) }),
  resetPassword: (id: string, password: string) => request<void>(`/api/users/${id}/password`, { method: 'POST', body: JSON.stringify({ password }) }),
  deleteUser: (id: string) => request<void>(`/api/users/${id}`, { method: 'DELETE' }),
  exportBackup: () => download('/api/backup', 'sanjcorp3d-backup.json'),
  restoreBackup: (backup: unknown, confirmation: string) => request<{ message: string; quotes: number }>('/api/backup/restore', { method: 'POST', body: JSON.stringify({ backup, confirmation }) }),
  tenants: () => request<Tenant[]>('/api/tenants'),
  createMakerTenant: (item: { name: string; slug: string; logoUrl?: string; username: string; displayName: string; email?: string; password: string }) => request<Tenant>('/api/tenants', { method: 'POST', body: JSON.stringify(item) }),
  updateTenant: (id: string, item: { name: string; logoUrl?: string; active: boolean }) => request<Tenant>(`/api/tenants/${id}`, { method: 'PUT', body: JSON.stringify(item) }),
  deleteMakerTenant: (id: string) => request<void>(`/api/tenants/${id}`, { method: 'DELETE' }),
  createMakerUser: (tenantId: string, item: { username: string; displayName: string; email?: string; password: string }) => request<UserAccount>(`/api/tenants/${tenantId}/users`, { method: 'POST', body: JSON.stringify(item) }),
  chat: (after?: number) => request<ChatMessage[]>(`/api/chat${query({ after })}`),
  sendChat: (body: string, photoUrl?: string) => request<ChatMessage>('/api/chat', { method: 'POST', body: JSON.stringify({ body, photoUrl }) }),
  printOrders: () => request<PrintOrder[]>(`/api/print-orders`),
  startPrintOrder: (id: number) => request<PrintOrder>(`/api/print-orders/${id}/start`, { method: 'POST' }),
  finishPrintOrder: (id: number) => request<PrintOrder>(`/api/print-orders/${id}/finish`, { method: 'POST' }),
  completePrintOrderCooling: (id: number) => request<PrintOrder>(`/api/print-orders/${id}/cooling-complete`, { method: 'POST' }),
  products: (includeArchived = false) => request<ProductCatalog[]>(`/api/products${query({ includeArchived })}`),
  createProduct: (name: string) => request<ProductCatalog>('/api/products', { method: 'POST', body: JSON.stringify({ name }) }),
  saveStoreProduct: (item: ProductCatalog) => item.id ? request<ProductCatalog>(`/api/products/${item.id}`, { method: 'PATCH', body: JSON.stringify(item) }) : request<ProductCatalog>('/api/products', { method: 'POST', body: JSON.stringify(item) }),
  calculateStoreQuote: (quote: StoreQuoteRequest) => request<StoreQuoteCalculation>('/api/products/store/calculate', { method: 'POST', body: JSON.stringify(quote) }),
  createStoreQuote: (quote: StoreQuoteRequest) => request<QuoteSummary>('/api/products/store/quote', { method: 'POST', body: JSON.stringify(quote) }),
  updateProduct: (id: number, name: string) => request<ProductCatalog>(`/api/products/${id}`, { method: 'PATCH', body: JSON.stringify({ name }) }),
  archiveProduct: (id: number) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),
}
