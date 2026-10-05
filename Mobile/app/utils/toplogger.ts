import { Capacitor, CapacitorHttp, registerPlugin } from '@capacitor/core'
import type { Ascent, ClimbUser, Community, Route, RouteLog, User } from './domain'
import { FRENCH_GRADES } from './domain'
import { REFRESH, USER, ROUTES, ROUTE, DAYS, HISTORY, COMMUNITY, TOPPERS, LOG_ASCENT, ROUTE_LOGS, UNSEND, GRADE_VOTE } from './queries'

export class ApiError extends Error {
  constructor(public kind: 'auth' | 'network' | 'api' | 'storage', message: string) { super(message) }
}
export interface Tokens { access: { token: string; expiresAt: string }; refresh: { token: string; expiresAt: string } }
export interface HistorySnapshot { sessions: { date: string; at: string; logs: Ascent[] }[] }
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
    if (!/^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/.test(token.trim())) throw new ApiError('auth', 'TopLogger did not return a valid connection. Please sign in again.')
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
    if (Date.parse(this.tokens.refresh.expiresAt) <= Date.now()) throw new ApiError('auth', 'Your connection has expired. Please sign in again.')
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
  async history(gymId: string, userId: string, cached?: HistorySnapshot): Promise<HistorySnapshot> {
    const now = Date.now(), week = 7 * 86400000
    const cutoff = new Date(); cutoff.setHours(0, 0, 0, 0); cutoff.setDate(cutoff.getDate() - 180)
    const days = await this.pages<{ id: string; gymId: string; statsAtDate: string }>(DAYS, 'climbDaysPaginated', { userId, from: cutoff.toISOString(), until: new Date().toISOString() })
    if (days.some(day => !day || typeof day.gymId !== 'string' || !Number.isFinite(Date.parse(day.statsAtDate)))) throw new ApiError('api', 'TopLogger returned incomplete session dates.')
    const dates = [...new Set(days.filter(day => day.gymId === gymId).map(day => day.statsAtDate))]
    const validLogs = (logs: Ascent[]) => Array.isArray(logs) && logs.every(log => log && typeof log.id === 'string' && log.gymId === gymId && typeof log.climbId === 'string' && Number.isFinite(Date.parse(log.climbedAtDate)))
    const previous = new Map((Array.isArray(cached?.sessions) ? cached.sessions : []).filter(session => session && typeof session.date === 'string' && validLogs(session.logs)).map(session => [session.date, session]))
    const sessions: HistorySnapshot['sessions'] = []
    const missing: string[] = []
    for (const date of dates) {
      const session = previous.get(date), age = session ? now - Date.parse(session.at) : NaN
      // ponytail: older edits can lag by a week; add server change markers if the API exposes them.
      if (session && Date.parse(date) < now - week && age >= 0 && age < week) sessions.push(session)
      else missing.push(date)
    }
    // Cache complete days, including empty ones. The current day list drops deleted/expired sessions.
    for (let offset = 0; offset < missing.length; offset += 4) {
      const batches = await Promise.all(missing.slice(offset, offset + 4).map(async date => {
        const logs = await this.pages<Ascent>(HISTORY, 'climbLogs', { gymId, userId, date })
        if (!validLogs(logs)) throw new ApiError('api', 'TopLogger returned incomplete ascent history.')
        return { date, at: new Date().toISOString(), logs }
      }))
      sessions.push(...batches)
    }
    return { sessions }
  }
  async route(gymId: string, id: string, userId: string): Promise<Route> {
    const data = await this.query<{ climb: Route | null }>(ROUTE, { gymId, id, userId })
    if (typeof data.climb?.id !== 'string' || !Number.isFinite(data.climb.grade)) throw new ApiError('api', 'This historical route is no longer available from TopLogger.')
    return data.climb
  }
  async logAscent(gymId: string, userId: string, route: Route, tickType: number, lead = !!route.leadRequired): Promise<ClimbUser> {
    if (![0, 1, 2, 3].includes(tickType)) throw new ApiError('api', 'Choose Try, Redpoint, Flash, or Onsight.')
    if (tickType > 1 && (route.climbUser?.totalTries || route.climbUser?.tickType)) throw new ApiError('api', 'Flash and Onsight require a first attempt. Choose Redpoint for another ascent.')
    if (typeof route.leadEnabled !== 'boolean' || typeof route.leadRequired !== 'boolean') throw new ApiError('api', 'Refresh routes before saving an ascent.')
    const today = new Date()
    const climbedAtDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
    try {
      const data = await this.query<{ climbUsers: (ClimbUser & { climbId: string })[] }>(LOG_ASCENT, {
        gymId, userId, climbIds: [route.id], climbLogTriesBefore: tickType === 1 && !route.climbUser?.totalTries ? 1 : 0,
        climbLogData: { topped: tickType > 0, foreknowledge: tickType === 1 || tickType === 2, zones: 0, lead: route.leadEnabled ? lead : !!route.leadRequired, climbedAtDate },
      })
      const saved = data.climbUsers?.find(item => item.climbId === route.id)
      if (!saved || !Number.isFinite(saved.tickType) || !Number.isFinite(saved.totalTries)) throw new ApiError('network', 'Incomplete save response.')
      return saved
    } catch (error) {
      if (!(error instanceof ApiError) || error.kind !== 'auth') throw new ApiError('network', 'Could not confirm whether the ascent was saved. Check TopLogger before saving again.')
      throw error
    }
  }
  async routeLogs(gymId: string, userId: string, routeId: string): Promise<RouteLog[]> {
    const logs = await this.pages<RouteLog>(ROUTE_LOGS, 'climbLogs', { gymId, userId, climbId: routeId })
    if (logs.some(log => !log || typeof log.id !== 'string' || log.gymId !== gymId || log.climbId !== routeId || typeof log.valid !== 'boolean' || typeof log.topped !== 'boolean' || typeof log.autoAdded !== 'boolean' || !Number.isFinite(Date.parse(log.climbedAtDate)))) throw new ApiError('api', 'Could not verify this route’s ascents. Refresh and try again.')
    return logs
  }
  private async deleteLogs(gymId: string, userId: string, routeId: string, ids: string[], failure: string): Promise<ClimbUser | null> {
    try {
      const data = await this.query<{ climbUsers: (ClimbUser & { climbId: string })[] }>(UNSEND, { gymId, userId, ids })
      const updated = data.climbUsers?.find(item => item.climbId === routeId)
      if (updated && Number.isFinite(updated.tickType) && Number.isFinite(updated.totalTries)) return updated
      return (await this.route(gymId, routeId, userId)).climbUser
    } catch (error) {
      if (!(error instanceof ApiError) || error.kind !== 'auth') throw new ApiError('network', failure)
      throw error
    }
  }
  async unsend(gymId: string, userId: string, routeId: string): Promise<ClimbUser | null> {
    const logs = await this.routeLogs(gymId, userId, routeId)
    // Match the official Redpoint uncheck: keep genuine attempts, remove sends and generated attempts.
    const ids = logs.filter(log => log.valid && (log.topped || log.autoAdded)).map(log => log.id)
    if (!ids.length) return (await this.route(gymId, routeId, userId)).climbUser
    return this.deleteLogs(gymId, userId, routeId, ids, 'Could not confirm whether the sends were removed. Check TopLogger before trying again.')
  }
  async deleteLog(gymId: string, userId: string, routeId: string, logId: string): Promise<ClimbUser | null> {
    // Only delete a log TopLogger currently lists for this account and route.
    if (!(await this.routeLogs(gymId, userId, routeId)).some(log => log.id === logId)) throw new ApiError('api', 'This log is no longer on TopLogger. Refresh and try again.')
    return this.deleteLogs(gymId, userId, routeId, [logId], 'Could not confirm whether the log was removed. Check TopLogger before trying again.')
  }
  async voteGrade(gymId: string, userId: string, routeId: string, grade: number): Promise<ClimbUser> {
    if (!FRENCH_GRADES.includes(grade)) throw new ApiError('api', 'Choose a valid French grade.')
    try {
      const data = await this.query<{ climbUsers: (ClimbUser & { climbId: string })[] }>(GRADE_VOTE, { gymId, userId, climbIds: [routeId], grade })
      const updated = data.climbUsers?.find(item => item.climbId === routeId)
      if (!updated || updated.grade !== grade || !Number.isFinite(updated.tickType) || !Number.isFinite(updated.totalTries)) throw new ApiError('network', 'Incomplete grade response.')
      return updated
    } catch (error) {
      if (!(error instanceof ApiError) || error.kind !== 'auth') throw new ApiError('network', 'Could not confirm whether your grade was saved. Check TopLogger before trying again.')
      throw error
    }
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
