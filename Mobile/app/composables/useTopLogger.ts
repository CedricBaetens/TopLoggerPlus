import { emptyAdventure, loadAdventure, saveAdventure, updateReward, progress, newlyUnlocked } from '../utils/adventure'
import { computed, reactive } from 'vue'
import { ApiError, TopLoggerClient, type HistorySnapshot } from '../utils/toplogger'
import { cacheKey, clearCache, readCache, writeCache } from '../utils/cache'
import { dateValue, dayLabel, isActive, topTen, visibleLogs, type Ascent, type ClimbUser, type Community, type Gym, type Route, type RouteLog, type User } from '../utils/domain'
import { COMMUNITY } from '../utils/queries'

const client = new TopLoggerClient()
const state = reactive({
  user: null as User | null, gymId: '', routes: [] as Route[], liveRouteIds: [] as string[], history: [] as Ascent[],
  historyReady: false, historyError: '', syncedAt: '', historyAt: '', busy: false,
  initialized: false, connected: false, needsLogin: false, error: '', cacheWarning: '',
  tab: 'routes' as 'routes' | 'top' | 'adventure' | 'account',
  adventure: emptyAdventure(), adventureMessage: '', adventureWarning: '', days: 60,
  selected: null as Route | null, community: null as Community | null,
  communityBusy: false, communityError: '', theme: 'system' as 'system' | 'light' | 'dark',
  ascentBusy: false, ascentError: '', ascentAction: '',
  logs: null as RouteLog[] | null, logsBusy: false, logsError: '',
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
    const sessionsKey = cacheKey(userId, gymId, 'historySessions')
    const snapshot = await client.history(gymId, userId, readCache<HistorySnapshot>(sessionsKey)?.data)
    if (id !== requestId) return
    const history = snapshot.sessions.flatMap(session => session.logs)
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
    save(sessionsKey, snapshot)
    state.history = history; state.historyReady = true
  } catch (error) { if (id === requestId) state.historyError = report(error) }
}
async function refresh(): Promise<void> {
  if (!state.user || !state.gymId || state.busy || state.ascentBusy) return
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
    state.adventure = loadAdventure(state.user.id)
    cachedGym()
  }
  try {
    state.connected = await client.restore()
    if (state.connected) {
      const user = await client.user()
      if (state.user && state.user.id !== user.id) { clearCache(); state.routes = []; state.history = [] }
      state.user = user; state.adventure = loadAdventure(user.id); save(PROFILE, user)
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
    state.user = user; state.adventure = loadAdventure(user.id); state.adventureMessage = ''; state.adventureWarning = ''; state.connected = true; state.needsLogin = false
    save(PROFILE, user)
    state.gymId = readCache<string>(cacheKey(user.id, '_', 'gym'))?.data || user.gym?.id || user.gymUserFavorites[0]?.gym.id || ''
    cachedGym(); state.tab = 'routes'
  } catch (error) { state.connected = false; state.needsLogin = true; state.error = report(error); return }
  finally { state.busy = false }
  await refresh()
}
async function selectGym(gymId: string): Promise<void> {
  if (state.ascentBusy || !state.user || gymId === state.gymId || !gyms.value.some(gym => gym.id === gymId)) return
  requestId++; detailsId++; state.busy = false; state.selected = null; state.error = ''; state.gymId = gymId
  save(cacheKey(state.user.id, '_', 'gym'), gymId)
  cachedGym(); await refresh()
}
async function selectTab(tab: typeof state.tab): Promise<void> {
  state.tab = tab
  if (tab === 'top' && !state.historyReady && !state.busy && !state.ascentBusy && state.user && state.connected && !state.needsLogin) {
    const id = ++requestId; state.busy = true
    try { await loadHistory(id) } finally { if (id === requestId) state.busy = false }
  }
}
async function openRoute(route: Route): Promise<void> {
  const id = ++detailsId
  state.selected = route; state.communityError = ''; state.communityBusy = true
  state.ascentError = ''; state.logs = null
  if (!state.user) return
  void loadLogs(route.id, id)
  const key = cacheKey(state.user.id, state.gymId, `community:${route.id}`)
  state.community = readCache<Community>(key)?.data ?? null
  try {
    const community = await client.community(state.gymId, route.id)
    if (id === detailsId) { state.community = community; save(key, community) }
  } catch (error) { if (id === detailsId) state.communityError = report(error) }
  finally { if (id === detailsId) state.communityBusy = false }
}
async function loadLogs(routeId: string, id = detailsId): Promise<void> {
  if (!state.user) return
  state.logsBusy = true; state.logsError = ''
  try {
    const logs = await client.routeLogs(state.gymId, state.user.id, routeId)
    if (id === detailsId) state.logs = visibleLogs(logs)
  } catch (error) { if (id === detailsId) state.logsError = report(error) }
  finally { if (id === detailsId) state.logsBusy = false }
}
function reward(userId: string, gymId: string, routeId: string, tickType: number): void {
  const before = progress(state.adventure)
  const next = updateReward(state.adventure, gymId, routeId, tickType)
  const after = progress(next)
  state.adventure = next
  state.adventureWarning = saveAdventure(userId, next) ? '' : 'Adventure progress could not be saved on this device. It may be lost when you close the app.'
  if (after.xp > before.xp) {
    const unlocked = newlyUnlocked(before.achievements, after.achievements).map(item => item.name)
    state.adventureMessage = `+${after.xp - before.xp} XP!${after.level > before.level ? ` Level ${after.level}!` : ''}${unlocked.length ? ` Achievement unlocked: ${unlocked.join(', ')}.` : ''}`
  }
}
async function applyClimbUser(route: Route, climbUser: ClimbUser | null, userId: string, gymId: string, detail: number, removed: boolean): Promise<void> {
  const updated = { ...route, climbUser }
  state.routes = state.routes.map(item => item.id === route.id ? updated : item)
  save(cacheKey(userId, gymId, 'routes'), { routes: state.routes, liveIds: state.liveRouteIds })
  // Removing logs may change older days; invalidate only sessions containing this route.
  state.historyReady = false; state.historyAt = ''
  try {
    localStorage.removeItem(cacheKey(userId, gymId, 'history'))
    if (removed) {
      const key = cacheKey(userId, gymId, 'historySessions')
      const cached = readCache<HistorySnapshot>(key)?.data
      if (Array.isArray(cached?.sessions)) save(key, { sessions: cached.sessions.filter(session => !session.logs.some(log => log.climbId === route.id)) })
    }
  }
  catch { state.cacheWarning = 'Could not clear saved history. Refresh Top 10 to see your ascent.' }
  if (detail === detailsId) state.selected = updated
  if (state.tab === 'top') await loadHistory(++requestId)
}
async function submitAscent(tickType: number): Promise<void> {
  if (!state.selected || !state.user || !state.connected || state.needsLogin || state.busy || state.ascentBusy) return
  const route = state.selected, userId = state.user.id, gymId = state.gymId, detail = detailsId
  const repeat = tickType > 0 && !!route.climbUser?.tickType
  if (tickType === -1) {
    const tops = state.logs ? state.logs.filter(log => log.topped).length : route.climbUser?.totalTicks ?? 0
    if (tops > 1 && !window.confirm(`Remove ${tops} ascents?`)) return
  }
  state.ascentBusy = true; state.ascentError = ''; state.adventureMessage = ''; state.ascentAction = `${route.id}:ascent:${repeat ? 'repeat' : tickType}`
  try {
    const climbUser = tickType === -1 ? await client.unsend(gymId, userId, route.id) : await client.logAscent(gymId, userId, route, tickType)
    if (tickType === -1 || (tickType > 0 && !route.climbUser?.tickType && climbUser?.tickType === tickType)) reward(userId, gymId, route.id, tickType)
    await applyClimbUser(route, climbUser, userId, gymId, detail, tickType === -1)
  } catch (error) { if (detail === detailsId) state.ascentError = report(error) }
  finally { state.ascentBusy = false; state.ascentAction = ''; if (detail === detailsId) void loadLogs(route.id, detail) }
}
async function deleteLog(log: RouteLog): Promise<void> {
  if (!state.selected || !state.user || !state.connected || state.needsLogin || state.busy || state.ascentBusy) return
  const route = state.selected, userId = state.user.id, gymId = state.gymId, detail = detailsId
  const warnings: string[] = []
  // A route that left the wall cannot be logged again, so the deletion is permanent.
  if (route.outAt && dateValue(route.outAt) <= Date.now()) warnings.push(`This will remove your ${log.topped ? 'ascent' : 'try'} from ${dayLabel(log.climbedAtDate).replace(/^(Today|Yesterday)$/, day => day.toLowerCase())}. This cannot be undone.`)
  if (log.topped && !log.tickIndex && state.logs?.some(other => other.id !== log.id && other.topped)) warnings.push('Your next ascent will become your first top.')
  if (warnings.length && !window.confirm(warnings.join(' '))) return
  state.ascentBusy = true; state.ascentError = ''; state.adventureMessage = ''; state.ascentAction = `${route.id}:log:${log.id}`
  try {
    const climbUser = await client.deleteLog(gymId, userId, route.id, log.id)
    // Same Adventure hook as Unsend, only when no top is left.
    if (route.climbUser?.tickType && !climbUser?.tickType) reward(userId, gymId, route.id, -1)
    await applyClimbUser(route, climbUser, userId, gymId, detail, true)
  } catch (error) { if (detail === detailsId) state.ascentError = report(error) }
  finally { state.ascentBusy = false; state.ascentAction = ''; if (detail === detailsId) void loadLogs(route.id, detail) }
}
async function submitGrade(grade: number): Promise<void> {
  if (!state.selected || !state.user || !state.connected || state.needsLogin || state.busy || state.ascentBusy || state.selected.climbUser?.grade === grade) return
  const route = state.selected, userId = state.user.id, gymId = state.gymId, detail = detailsId
  state.ascentBusy = true; state.ascentError = ''; state.ascentAction = `${route.id}:grade:${grade}`
  try {
    const climbUser = await client.voteGrade(gymId, userId, route.id, grade)
    const updated = { ...route, climbUser }
    state.routes = state.routes.map(item => item.id === route.id ? updated : item)
    save(cacheKey(userId, gymId, 'routes'), { routes: state.routes, liveIds: state.liveRouteIds })
    if (detail === detailsId) state.selected = updated
    // Refresh vote counts without fetching the toppers list or ascent history again.
    try {
      const { climb } = await client.query<{ climb: Pick<Community, 'gradeVoteStats' | 'ratingVoteStats'> }>(COMMUNITY, { gymId, id: route.id })
      if (detail === detailsId && state.community && Array.isArray(climb?.gradeVoteStats)) {
        state.community = { ...state.community, gradeVoteStats: climb.gradeVoteStats }
        save(cacheKey(userId, gymId, `community:${route.id}`), state.community)
      }
    } catch { /* Personal grade is confirmed even if community statistics are temporarily unavailable. */ }
  } catch (error) { if (detail === detailsId) state.ascentError = report(error) }
  finally { state.ascentBusy = false; state.ascentAction = '' }
}
function closeRoute(): void { detailsId++; state.selected = null; state.community = null; state.ascentError = ''; state.logs = null; state.logsError = ''; state.logsBusy = false }
async function logout(): Promise<void> {
  if (state.ascentBusy) return
  requestId++; detailsId++; state.busy = false
  try { await client.disconnect() }
  catch { state.error = 'Could not clear your secure connection. Please retry signing out.'; return }
  clearCache(); state.adventure = emptyAdventure(); state.adventureMessage = ''; state.adventureWarning = ''; state.user = null; state.routes = []; state.history = []; state.selected = null; state.community = null
  state.gymId = ''; state.liveRouteIds = []; state.syncedAt = ''; state.historyAt = ''; state.historyReady = false
  state.connected = false; state.needsLogin = true; state.error = ''; state.tab = 'routes'
}
function clearSaved(): void {
  if (!state.user || state.ascentBusy) return
  clearCache(state.user.id); save(PROFILE, state.user)
  state.routes = []; state.liveRouteIds = []; state.history = []; state.historyReady = false; state.syncedAt = ''; state.historyAt = ''; state.community = null
}
function resetAdventure(): void {
  if (!state.user || state.ascentBusy || !window.confirm('Reset all adventure XP and achievements for this account on this device?')) return
  const empty = emptyAdventure()
  if (!saveAdventure(state.user.id, empty)) { state.adventureWarning = 'Could not reset adventure progress. Please retry.'; return }
  state.adventure = empty; state.adventureWarning = ''; state.adventureMessage = 'Adventure progress reset. Level 1 awaits!'
}
const gyms = computed(() => [...new Map([...(state.user?.gym ? [state.user.gym] : []), ...(state.user?.gymUserFavorites ?? []).map(item => item.gym)].map(gym => [gym.id, gym])).values()])
export function useTopLogger() {
  return { state, gyms, adventureProgress: computed(() => progress(state.adventure)), resetAdventure, activeRoutes: computed(() => { const live = new Set(state.liveRouteIds); return state.routes.filter(route => live.has(route.id) && isActive(route)) }),
    ranked: computed(() => state.historyReady ? topTen(state.routes, state.history, state.gymId, state.days) : []),
    initialize, connect, refresh, selectGym, selectTab, openRoute, closeRoute, loadLogs, submitAscent, deleteLog, submitGrade, logout, clearSaved }
}
