import { Capacitor, CapacitorHttp, registerPlugin } from '@capacitor/core'
import type { Ascent, Community, Route, User } from './domain'
import { REFRESH, USER, ROUTES, ROUTE, DAYS, HISTORY, COMMUNITY, TOPPERS } from './queries'

export class ApiError extends Error {
  constructor(public kind: 'auth' | 'network' | 'api' | 'storage', message: string) { super(message) }
}
export interface Tokens { access: { token: string; expiresAt: string }; refresh: { token: string; expiresAt: string } }
interface Vault { read(): Promise<{ value?: string }>; write(options: { value: string }): Promise<void>; clear(): Promise<void> }
const vault = registerPlugin<Vault>('TokenVault')
export type Transport = (query: string, variables: Record<string, unknown>, token?: string) => Promise<any>
export function historyFailure(query: string, body: any): string {
  const fallback = 'TopLogger could not return this data. Try again later.'
  if (query !== HISTORY && query !== DAYS && query !== ROUTE) return fallback
  const error = body?.errors?.[0]
  const detail = error?.extensions?.originalError?.message ?? error?.extensions?.exception?.response?.message
  const message = Array.isArray(detail) ? detail.join('; ') : typeof detail === 'string' ? detail : error?.message
  if (typeof message !== 'string') return fallback
  // History/route queries contain no credentials; redact opaque values in server diagnostics anyway.
  const reason = message.replace(/[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g, '[redacted]').replace(/[A-Za-z0-9_-]{16,}/g, '[redacted]').replace(/[\w.+-]+@[\w.-]+/g, '[redacted]').slice(0, 240)
  return `TopLogger could not return ${query === ROUTE ? 'a historical route' : 'ascent history'}: ${reason}`
}
export async function transport(query: string, variables: Record<string, unknown>, token?: string): Promise<any> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (token) headers.Authorization = `Bearer ${token}`
  let status: number, body: any
  try {
    if (Capacitor.isNativePlatform()) {
      const response = await CapacitorHttp.post({ url: 'https://app.toplogger.nu/graphql', headers, data: { query, variables }, connectTimeout: 15000, readTimeout: 30000 })
      status = response.status; body = typeof response.data === 'string' ? JSON.parse(response.data) : response.data
    } else {
      const response = await fetch('https://app.toplogger.nu/graphql', { method: 'POST', headers, body: JSON.stringify({ query, variables }), signal: AbortSignal.timeout(30000) })
      status = response.status; body = await response.json()
    }
  } catch { throw new ApiError('network', 'Could not reach TopLogger. Check your connection and try again.') }
  if (status === 401 || body?.errors?.some((error: any) => error.extensions?.code === 'UNAUTHENTICATED')) throw new ApiError('auth', 'Your TopLogger session has expired. Connect again to continue.')
  if (status === 403) throw new ApiError('api', 'TopLogger did not allow this request.')
  if (status >= 500) throw new ApiError('network', 'TopLogger is temporarily unavailable. Your saved data is still available.')
  if (status >= 400 || body?.errors?.length || !body?.data) throw new ApiError('api', historyFailure(query, body))
  return body.data
}
function validTokens(value: unknown): value is Tokens {
  const data = value as Tokens
  return typeof data?.access?.token === 'string' && !!data.access.token && typeof data?.refresh?.token === 'string' && !!data.refresh.token && typeof data.access.expiresAt === 'string' && typeof data.refresh.expiresAt === 'string' && Number.isFinite(Date.parse(data.access.expiresAt)) && Number.isFinite(Date.parse(data.refresh.expiresAt))
}
export class TopLoggerClient {
  private tokens: Tokens | null = null
  private refreshing: Promise<string> | null = null
  private generation = 0
  constructor(private send: Transport = transport, private storage: Vault = vault, private native = Capacitor.isNativePlatform()) {}
  async restore(): Promise<boolean> {
    if (!this.native) return false
    try {
      const stored = await this.storage.read()
      if (!stored.value) return false
      const data: unknown = JSON.parse(stored.value)
      if (!validTokens(data)) { await this.storage.clear(); return false }
      this.tokens = data; return true
    } catch { throw new ApiError('storage', 'Could not unlock your saved connection. Please connect again.') }
  }
  async connect(token: string): Promise<void> {
    if (!/^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/.test(token.trim())) throw new ApiError('auth', 'Paste a complete TopLogger refresh token.')
    this.generation++
    await this.refresh(token.trim())
  }
  private refresh(token: string): Promise<string> {
    if (this.refreshing) return this.refreshing
    const generation = this.generation
    this.refreshing = (async () => {
      const data = await this.send(REFRESH, { refreshToken: token }, token)
      if (generation !== this.generation) throw new ApiError('auth', 'Connection cancelled.')
      if (!validTokens(data.tokens)) throw new ApiError('api', 'TopLogger returned an incomplete connection.')
      if (this.native) {
        try { await this.storage.write({ value: JSON.stringify(data.tokens) }) }
        catch { throw new ApiError('storage', 'Could not securely save the connection. Please try again.') }
        if (generation !== this.generation) { await this.storage.clear(); throw new ApiError('auth', 'Connection cancelled.') }
      }
      this.tokens = data.tokens
      return data.tokens.access.token
    })().finally(() => { this.refreshing = null })
    return this.refreshing
  }
  private async access(): Promise<string> {
    if (!this.tokens) throw new ApiError('auth', 'Connect your TopLogger account to continue.')
    if (Date.parse(this.tokens.access.expiresAt) > Date.now() + 60000) return this.tokens.access.token
    if (Date.parse(this.tokens.refresh.expiresAt) <= Date.now()) throw new ApiError('auth', 'Your connection has expired. Paste a fresh refresh token.')
    return this.refresh(this.tokens.refresh.token)
  }
  async query<T>(query: string, variables: Record<string, unknown> = {}): Promise<T> {
    const generation = this.generation
    const token = await this.access()
    try {
      const data = await this.send(query, variables, token)
      if (generation !== this.generation) throw new ApiError('auth', 'Connection cancelled.')
      return data
    } catch (error) {
      if (!(error instanceof ApiError) || error.kind !== 'auth' || !this.tokens || generation !== this.generation) throw error
      // Retry an expired access token once, sharing a refresh with concurrent requests.
      const fresh = this.tokens.access.token !== token ? this.tokens.access.token : await this.refresh(this.tokens.refresh.token)
      const data = await this.send(query, variables, fresh)
      if (generation !== this.generation) throw new ApiError('auth', 'Connection cancelled.')
      return data
    }
  }
  async disconnect(): Promise<void> {
    this.generation++; this.tokens = null
    if (this.native) await this.storage.clear()
  }
  private async pages<T>(query: string, field: string, variables: Record<string, unknown>): Promise<T[]> {
    const data: T[] = []
    for (let page = 1; ; page++) {
      const response = await this.query<Record<string, { data: T[]; pagination: { total: number; page: number; perPage: number } }>>(query, { ...variables, pagination: { page, perPage: 100 } })
      const result = response[field]
      if (!result || !Array.isArray(result.data) || !result.pagination || result.pagination.page !== page || result.pagination.perPage <= 0 || !Number.isFinite(result.pagination.total)) throw new ApiError('api', 'TopLogger returned incomplete pagination. Refresh to try again.')
      data.push(...result.data)
      if (page * result.pagination.perPage >= result.pagination.total) return data
      if (!result.data.length) throw new ApiError('api', 'TopLogger returned incomplete data. Refresh to try again.')
    }
  }
  async user(): Promise<User> {
    const { userMe } = await this.query<{ userMe: User }>(USER)
    if (!userMe?.id || !Array.isArray(userMe.gymUserFavorites)) throw new ApiError('api', 'TopLogger returned an incomplete profile.')
    return userMe
  }
  async routes(gymId: string, userId: string): Promise<Route[]> {
    const routes = await this.pages<Route>(ROUTES, 'climbs', { gymId, userId })
    if (routes.some(route => !route || typeof route.id !== 'string' || !Number.isFinite(route.grade))) throw new ApiError('api', 'TopLogger returned incomplete route data.')
    return routes
  }
  async history(gymId: string, userId: string): Promise<Ascent[]> {
    const cutoff = new Date(); cutoff.setHours(0, 0, 0, 0); cutoff.setDate(cutoff.getDate() - 180)
    const days = await this.pages<{ id: string; gymId: string; statsAtDate: string }>(DAYS, 'climbDaysPaginated', { userId, from: cutoff.toISOString(), until: new Date().toISOString() })
    if (days.some(day => !day || typeof day.gymId !== 'string' || !Number.isFinite(Date.parse(day.statsAtDate)))) throw new ApiError('api', 'TopLogger returned incomplete session dates.')
    const dates = [...new Set(days.filter(day => day.gymId === gymId).map(day => day.statsAtDate))]
    const logs: Ascent[] = []
    // Follow the official history view: load logs by climbing day, within the ranking window.
    for (let offset = 0; offset < dates.length; offset += 4) {
      const batches = await Promise.all(dates.slice(offset, offset + 4).map(date => this.pages<Ascent>(HISTORY, 'climbLogs', { gymId, userId, date })))
      logs.push(...batches.flat())
    }
    if (logs.some(log => !log || typeof log.id !== 'string' || typeof log.gymId !== 'string' || typeof log.climbId !== 'string' || typeof log.climbedAtDate !== 'string')) throw new ApiError('api', 'TopLogger returned incomplete ascent history.')
    return logs
  }
  async route(gymId: string, id: string, userId: string): Promise<Route> {
    const data = await this.query<{ climb: Route | null }>(ROUTE, { gymId, id, userId })
    if (typeof data.climb?.id !== 'string' || !Number.isFinite(data.climb.grade)) throw new ApiError('api', 'This historical route is no longer available from TopLogger.')
    return data.climb
  }
  async community(gymId: string, id: string): Promise<Community> {
    const data = await this.query<{ climb: Omit<Community, 'toppers'> | null }>(COMMUNITY, { gymId, id })
    if (!data.climb || !Array.isArray(data.climb.gradeVoteStats) || !Array.isArray(data.climb.ratingVoteStats)) throw new ApiError('api', 'Community statistics are not available for this route.')
    let toppers: Community['toppers'] = [], toppersUnavailable = false
    try { toppers = (await this.pages<Community['toppers'][number]>(TOPPERS, 'climbUsers', { gymId, climbId: id })).filter(user => user.tickType > 0) }
    catch (error) { if (error instanceof ApiError && error.kind === 'api') toppersUnavailable = true; else throw error }
    return { ...data.climb, toppers, toppersUnavailable }
  }
}
