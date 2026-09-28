import { computed, reactive } from 'vue'
import { ApiError, TopLoggerClient } from '../utils/toplogger'
import { cacheKey, clearCache, readCache, writeCache } from '../utils/cache'
import { dateValue, isActive, topTen, type Ascent, type Community, type Gym, type Route, type User } from '../utils/domain'

const client = new TopLoggerClient()
const state = reactive({
  user: null as User | null, gymId: '', routes: [] as Route[], liveRouteIds: [] as string[], history: [] as Ascent[],
  historyReady: false, historyError: '', syncedAt: '', historyAt: '', busy: false,
  initialized: false, connected: false, needsLogin: false, error: '', cacheWarning: '',
  tab: 'routes' as 'routes' | 'top' | 'account', days: 60,
  selected: null as Route | null, community: null as Community | null,
  communityBusy: false, communityError: '', theme: 'system' as 'system' | 'light' | 'dark',
})
let requestId = 0, detailsId = 0
const PROFILE = cacheKey('_profile', '_', 'user')
interface RouteSnapshot { routes: Route[]; liveIds: string[] }
function report(error: unknown): string {
  if (error instanceof ApiError && (error.kind === 'auth' || error.kind === 'storage')) state.needsLogin = true
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.'
}
function save<T>(key: string, data: T): string {
  try { return writeCache(key, data) }
  catch { state.cacheWarning = 'Device storage is full. These updates could not be saved for offline use.'; return new Date().toISOString() }
}
function cachedGym(): void {
  state.routes = []; state.liveRouteIds = []; state.history = []; state.historyReady = false; state.syncedAt = ''; state.historyAt = ''; state.historyError = ''
  if (!state.user || !state.gymId) return
  const routes = readCache<RouteSnapshot>(cacheKey(state.user.id, state.gymId, 'routes'))
  const history = readCache<Ascent[]>(cacheKey(state.user.id, state.gymId, 'history'))
  if (routes && Array.isArray(routes.data?.routes) && Array.isArray(routes.data.liveIds)) { state.routes = routes.data.routes; state.liveRouteIds = routes.data.liveIds; state.syncedAt = routes.at }
  if (history && Array.isArray(history.data)) {
    state.history = history.data; state.historyAt = history.at
    // Do not present a partial ranking if historical route records are missing.
    state.historyReady = history.data.every(log => log.gymId !== state.gymId || log.climbType !== 'route' || !log.valid || !log.ticked || !log.topped || dateValue(log.climbedAtDate) < Date.now() - 181 * 86400000 || state.routes.some(route => route.id === log.climbId))
  }
}
async function loadHistory(id: number): Promise<void> {
  if (!state.user || !state.gymId) return
  const userId = state.user.id, gymId = state.gymId
  state.historyError = ''
  try {
    const history = await client.history(gymId, userId)
    const known = new Set(state.routes.map(route => route.id))
    const cutoff = new Date(); cutoff.setHours(0, 0, 0, 0); cutoff.setDate(cutoff.getDate() - 180)
    const missing = [...new Set(history.filter(log => log.gymId === gymId && log.climbType === 'route' && log.valid && log.ticked && log.topped && dateValue(log.climbedAtDate) >= cutoff.getTime() && !known.has(log.climbId)).map(log => log.climbId))]
    const recovered: Route[] = []
    for (let offset = 0; offset < missing.length; offset += 4) {
      if (id !== requestId) return
      recovered.push(...await Promise.all(missing.slice(offset, offset + 4).map(routeId => client.route(gymId, routeId, userId))))
    }
    if (id !== requestId) return
    state.routes = [...state.routes, ...recovered]
    save(cacheKey(userId, gymId, 'routes'), { routes: state.routes, liveIds: state.liveRouteIds })
    state.historyAt = save(cacheKey(userId, gymId, 'history'), history)
    state.history = history; state.historyReady = true
  } catch (error) { if (id === requestId) state.historyError = report(error) }
}
async function refresh(): Promise<void> {
  if (!state.user || !state.gymId || state.busy) return
  const id = ++requestId, userId = state.user.id, gymId = state.gymId
  state.busy = true; state.error = ''; state.cacheWarning = ''
  try {
    const routes = await client.routes(gymId, userId)
    if (id !== requestId) return
    // Keep archived route metadata needed by cached history.
    const seen = new Set(routes.map(route => route.id))
    state.routes = [...routes, ...state.routes.filter(route => !seen.has(route.id))]
    state.liveRouteIds = routes.map(route => route.id)
    state.syncedAt = save(cacheKey(userId, gymId, 'routes'), { routes: state.routes, liveIds: state.liveRouteIds })
    if (state.tab === 'top') await loadHistory(id)
  } catch (error) { if (id === requestId) state.error = report(error) }
  finally { if (id === requestId) state.busy = false }
}
async function initialize(): Promise<void> {
  state.theme = (localStorage.getItem('tlp:theme') as typeof state.theme) || 'system'
  state.user = readCache<User>(PROFILE)?.data ?? null
  if (state.user?.id) {
    state.gymId = readCache<string>(cacheKey(state.user.id, '_', 'gym'))?.data || state.user.gym?.id || state.user.gymUserFavorites?.[0]?.gym.id || ''
    cachedGym()
  }
  try {
    state.connected = await client.restore()
    if (state.connected) {
      const user = await client.user()
      if (state.user && state.user.id !== user.id) { clearCache(); state.routes = []; state.history = [] }
      state.user = user; save(PROFILE, user)
      if (!gyms.value.some(gym => gym.id === state.gymId)) state.gymId = user.gym?.id || gyms.value[0]?.id || ''
      cachedGym(); await refresh()
    } else state.needsLogin = true
  } catch (error) { state.error = report(error) }
  finally { state.initialized = true }
}
async function connect(token: string): Promise<void> {
  if (state.busy) return
  state.busy = true; state.error = ''
  try {
    await client.connect(token)
    const user = await client.user()
    if (state.user && state.user.id !== user.id) clearCache()
    state.user = user; state.connected = true; state.needsLogin = false
    save(PROFILE, user)
    state.gymId = readCache<string>(cacheKey(user.id, '_', 'gym'))?.data || user.gym?.id || user.gymUserFavorites[0]?.gym.id || ''
    cachedGym(); state.tab = 'routes'
  } catch (error) { state.connected = false; state.needsLogin = true; state.error = report(error); return }
  finally { state.busy = false }
  await refresh()
}
async function selectGym(gymId: string): Promise<void> {
  if (!state.user || gymId === state.gymId || !gyms.value.some(gym => gym.id === gymId)) return
  requestId++; detailsId++; state.busy = false; state.selected = null; state.error = ''; state.gymId = gymId
  save(cacheKey(state.user.id, '_', 'gym'), gymId)
  cachedGym(); await refresh()
}
async function selectTab(tab: typeof state.tab): Promise<void> {
  state.tab = tab
  if (tab === 'top' && !state.historyReady && !state.busy && state.user && state.connected && !state.needsLogin) {
    const id = ++requestId; state.busy = true
    try { await loadHistory(id) } finally { if (id === requestId) state.busy = false }
  }
}
async function openRoute(route: Route): Promise<void> {
  const id = ++detailsId
  state.selected = route; state.communityError = ''; state.communityBusy = true
  if (!state.user) return
  const key = cacheKey(state.user.id, state.gymId, `community:${route.id}`)
  state.community = readCache<Community>(key)?.data ?? null
  try {
    const community = await client.community(state.gymId, route.id)
    if (id === detailsId) { state.community = community; save(key, community) }
  } catch (error) { if (id === detailsId) state.communityError = report(error) }
  finally { if (id === detailsId) state.communityBusy = false }
}
function closeRoute(): void { detailsId++; state.selected = null; state.community = null }
async function logout(): Promise<void> {
  requestId++; detailsId++; state.busy = false
  try { await client.disconnect() }
  catch { state.error = 'Could not clear your secure connection. Please retry signing out.'; return }
  clearCache(); state.user = null; state.routes = []; state.history = []; state.selected = null; state.community = null
  state.gymId = ''; state.liveRouteIds = []; state.syncedAt = ''; state.historyAt = ''; state.historyReady = false
  state.connected = false; state.needsLogin = true; state.error = ''; state.tab = 'routes'
}
function clearSaved(): void {
  if (!state.user) return
  clearCache(state.user.id); save(PROFILE, state.user)
  state.routes = []; state.liveRouteIds = []; state.history = []; state.historyReady = false; state.syncedAt = ''; state.historyAt = ''; state.community = null
}
const gyms = computed(() => [...new Map([...(state.user?.gym ? [state.user.gym] : []), ...(state.user?.gymUserFavorites ?? []).map(item => item.gym)].map(gym => [gym.id, gym])).values()])
export function useTopLogger() {
  return { state, gyms, activeRoutes: computed(() => { const live = new Set(state.liveRouteIds); return state.routes.filter(route => live.has(route.id) && isActive(route)) }),
    ranked: computed(() => state.historyReady ? topTen(state.routes, state.history, state.gymId, state.days) : []),
    initialize, connect, refresh, selectGym, selectTab, openRoute, closeRoute, logout, clearSaved }
}
