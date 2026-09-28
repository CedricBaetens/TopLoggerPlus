<script setup lang="ts">
import { Capacitor } from '@capacitor/core'
import { App } from '@capacitor/app'
import { Browser } from '@capacitor/browser'
import { loginToken } from './utils/login'
import { ascentLabel, averageLabel, dateLabel, defaultWallSelection, frenchGrade, score } from './utils/domain'

const { state, gyms, activeRoutes, ranked, initialize, connect, refresh, selectGym, selectTab, openRoute, closeRoute, logout, clearSaved } = useTopLogger()
const token = ref(''), showToken = ref(false), showFilters = ref(false), search = ref('')
const signingIn = ref(false)
const appVersion = ref('3.0.0')
const grade = ref(''), selectedWalls = ref<string[]>([]), color = ref(''), status = ref('')
const wallsChanged = ref(false)
const root = ref<HTMLElement | null>(null), detailHeading = ref<HTMLElement | null>(null)
const tabs = [{ id: 'routes', title: 'Routes' }, { id: 'top', title: 'Top 10' }, { id: 'account', title: 'Account' }] as const
const title = computed(() => tabs.find(tab => tab.id === state.tab)?.title || 'Routes')
const source = computed(() => [...activeRoutes.value].sort((a, b) => a.grade - b.grade || (a.wall?.nameLoc || '').localeCompare(b.wall?.nameLoc || '') || (a.label || '').localeCompare(b.label || '', undefined, { numeric: true })))
const filtered = computed(() => source.value.filter(route => {
  const query = search.value.trim().toLowerCase()
  return (!query || [route.name, route.label, route.wall?.nameLoc, route.holdColor?.nameLoc, route.setterName, frenchGrade(route.grade)].some(value => value?.toLowerCase().includes(query)))
    && (!grade.value || String(route.grade) === grade.value) && (!selectedWalls.value.length || selectedWalls.value.includes(route.wall?.nameLoc || ''))
    && (!color.value || route.holdColor?.nameLoc === color.value)
    && (!status.value || (status.value === 'topped' ? (route.climbUser?.tickType || 0) > 0 : status.value === 'todo' ? !route.climbUser?.tickType : status.value === 'attempted' ? (route.climbUser?.totalTries || 0) > 0 && !route.climbUser?.tickType : !route.climbUser?.totalTries && !route.climbUser?.tickType))
}))
const grades = computed(() => [...new Set(activeRoutes.value.map(route => route.grade))].sort((a, b) => a - b))
const walls = computed(() => [...new Set(activeRoutes.value.flatMap(route => route.wall?.nameLoc ? [route.wall.nameLoc] : []))].sort((a, b) => a.localeCompare(b, undefined, { numeric: true })))
const colors = computed(() => [...new Set(activeRoutes.value.map(route => route.holdColor?.nameLoc).filter(Boolean))].sort())
const filterCount = computed(() => [grade.value, selectedWalls.value.length, color.value, status.value].filter(Boolean).length)
const topped = computed(() => activeRoutes.value.filter(route => (route.climbUser?.tickType || 0) > 0).length)
const communityRating = computed(() => {
  const votes = state.community?.ratingVoteStats ?? [], count = votes.reduce((sum, item) => sum + item.count, 0)
  return count ? `${(votes.reduce((sum, item) => sum + item.stars * item.count, 0) / count).toFixed(1)} / 5` : 'No ratings yet'
})
const synced = computed(() => {
  const value = state.tab === 'top' ? state.historyAt : state.syncedAt
  return value ? new Date(value).toLocaleString('en-GB', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }) : ''
})
watch(() => state.gymId, () => { grade.value = ''; selectedWalls.value = []; wallsChanged.value = false; color.value = ''; status.value = ''; search.value = '' })
watch([() => state.gymId, walls], () => {
  if (!wallsChanged.value) selectedWalls.value = defaultWallSelection(gyms.value.find(gym => gym.id === state.gymId)?.name || '', walls.value)
})
function allWalls() { wallsChanged.value = true; selectedWalls.value = [] }
watch(() => state.theme, theme => {
  document.documentElement.dataset.theme = theme
  localStorage.setItem('tlp:theme', theme)
})
async function submitToken() { const value = token.value; token.value = ''; await connect(value) }
async function signIn() {
  signingIn.value = true; state.error = ''
  try { const value = await loginToken(); await connect(value) }
  catch (error) { state.error = error instanceof Error ? error.message : 'Sign-in could not be completed. Try token entry.' }
  finally { signingIn.value = false }
}
async function officialLogin() {
  if (Capacitor.isNativePlatform()) await Browser.open({ url: 'https://app.toplogger.nu/en/sign-in' })
  else window.open('https://app.toplogger.nu/en/sign-in', '_blank', 'noopener,noreferrer')
}
function resetFilters() { grade.value = ''; allWalls(); color.value = ''; status.value = ''; search.value = '' }
async function changeTab(tab: typeof state.tab) { closeRoute(); await selectTab(tab); window.scrollTo({ top: 0 }) }
function showRoute(route: Parameters<typeof openRoute>[0]) {
  history.pushState({ route: route.id }, '')
  void openRoute(route)
  window.scrollTo({ top: 0 }); nextTick(() => detailHeading.value?.focus())
}
function back() {
  if (state.selected) { if (history.state?.route) history.back(); else closeRoute() }
  else if (showFilters.value) showFilters.value = false
  else if (state.tab !== 'routes') void changeTab('routes')
  else if (Capacitor.isNativePlatform()) void App.exitApp()
}
const pullStart = ref<number | null>(null), pullDistance = ref(0)
function touchStart(event: TouchEvent) { pullStart.value = window.scrollY <= 0 && !state.selected && !state.busy && state.tab !== 'account' ? event.touches[0]!.clientY : null }
function touchMove(event: TouchEvent) { pullDistance.value = pullStart.value === null ? 0 : Math.max(0, event.touches[0]!.clientY - pullStart.value) }
function touchEnd() { if (pullDistance.value > 90 && state.connected && !state.needsLogin) void refresh(); pullStart.value = null; pullDistance.value = 0 }
let backHandle: Awaited<ReturnType<typeof App.addListener>> | undefined
const popState = () => closeRoute()
onMounted(async () => {
  window.addEventListener('popstate', popState)
  if (Capacitor.isNativePlatform()) { backHandle = await App.addListener('backButton', back); appVersion.value = (await App.getInfo()).version }
  await initialize()
  document.documentElement.dataset.theme = state.theme
})
onBeforeUnmount(() => { window.removeEventListener('popstate', popState); void backHandle?.remove() })
</script>

<template>
  <div ref="root" class="app-shell" @touchstart.passive="touchStart" @touchmove.passive="touchMove" @touchend.passive="touchEnd">
    <a class="skip-link" href="#main">Skip to content</a>
    <header class="app-header">
      <a class="brand" href="#" aria-label="TopLogger Plus home" @click.prevent="changeTab('routes')"><span>TopLogger <span class="brand-plus">Plus</span></span></a>
      <label v-if="state.user && gyms.length" class="gym-picker"><span class="sr-only">Selected gym</span><select aria-label="Selected gym" :value="state.gymId" @change="selectGym(($event.target as HTMLSelectElement).value)"><option v-for="gym in gyms" :key="gym.id" :value="gym.id">{{ gym.name }}</option></select></label>
      
    </header>

    <main id="main" :class="{ 'login-main': !state.user }">
      <div v-if="!state.initialized" class="empty-state" role="status"><span class="spinner" /><h1>Getting things ready</h1><p>Opening your saved connection.</p></div>
      <section v-else-if="!state.user" class="onboarding">
        <h1>Connect TopLogger</h1>
        <div class="connect-card">

          <div v-if="state.error" class="notice error" role="alert">{{ state.error }}</div>
          <button v-if="Capacitor.isNativePlatform()" class="primary full" :disabled="signingIn || state.busy" @click="signIn"><AppIcon name="connect" />{{ signingIn ? 'Waiting for TopLogger…' : 'Sign in with TopLogger' }}</button>
          <p v-if="Capacitor.isNativePlatform()" class="login-alternative">Or connect with a refresh token</p>
          <form @submit.prevent="submitToken"><label for="token">Refresh token</label><div class="token-input"><input id="token" v-model="token" :type="showToken ? 'text' : 'password'" autocomplete="off" autocapitalize="none" spellcheck="false" placeholder="Paste your TopLogger refresh token" required :disabled="state.busy"><button type="button" :aria-pressed="showToken" @click="showToken = !showToken">{{ showToken ? 'Hide' : 'Show' }}</button></div><button class="primary full" :disabled="state.busy || !token.trim()"><span v-if="state.busy" class="spinner" /><AppIcon v-else name="connect" />{{ state.busy ? 'Connecting…' : 'Connect my account' }}</button></form>
          <details class="token-help"><summary>How do I get my token?</summary><p>Sign in to TopLogger on a desktop browser. Open Developer Tools → Application → Local Storage → app.toplogger.nu. Find <code>tl-auth</code> and copy only <code>refresh.token</code>. Paste it above.</p><p>On Android, use Sign in with TopLogger above to connect automatically. Signing in through an external browser requires this token fallback.</p><button class="secondary full" @click="officialLogin">Open TopLogger sign-in ↗</button></details>
          <p class="privacy-note">{{ Capacitor.isNativePlatform() ? 'Encrypted on your device. No Plus server. No password stored.' : 'Browser preview: tokens stay in memory for this session.' }}</p>
        </div>

      </section>

      <template v-else>
        <div v-if="state.error" class="notice error" role="alert">{{ state.error }}<button v-if="!state.needsLogin" @click="refresh">Retry</button></div>
        <div v-if="state.needsLogin" class="notice" role="status">Reconnect to refresh your data. Saved routes remain available.<button @click="changeTab('account')">Reconnect</button></div>
        <div v-if="state.cacheWarning" class="notice" role="status">{{ state.cacheWarning }}</div>
        <div v-if="pullDistance > 40" class="pull-hint" role="status">{{ pullDistance > 90 ? 'Release to refresh' : 'Pull to refresh' }}</div>

        <section v-if="state.selected" class="details-view">
          <button class="back-button" @click="back"><AppIcon name="back" />Back to {{ title }}</button>
          <div class="detail-hero"><div><span class="eyebrow">{{ state.selected.wall?.nameLoc || 'ROUTE DETAILS' }}</span><h1 ref="detailHeading" tabindex="-1">{{ frenchGrade(state.selected.grade) }}<span>{{ state.selected.name || (state.selected.label ? `Rope ${state.selected.label}` : 'Route') }}</span></h1><span class="badge topped">{{ ascentLabel(state.selected.climbUser?.tickType) }}</span></div><span class="detail-color" :style="{ background: /^#[a-f\d]{3,8}$/i.test(state.selected.holdColor?.color || '') ? state.selected.holdColor?.color : '#888' }" /></div>
          <div class="detail-panel"><h2>Route</h2><dl class="detail-grid"><div><dt>Wall</dt><dd>{{ state.selected.wall?.nameLoc || 'Unknown' }}</dd></div><div><dt>Rope / label</dt><dd>{{ state.selected.label || '—' }}</dd></div><div><dt>Hold color</dt><dd>{{ state.selected.holdColor?.nameLoc || 'Unknown' }}</dd></div><div><dt>Setter</dt><dd>{{ [...(state.selected.climbSetters || []).map(setter => setter.gymAdmin.name), ...(state.selected.setterName ? [state.selected.setterName] : [])].join(', ') || 'Unknown' }}</dd></div><div><dt>Set on</dt><dd>{{ dateLabel(state.selected.inAt) }}</dd></div><div><dt>Leaving / removed</dt><dd>{{ dateLabel(state.selected.outAt || state.selected.outPlannedAt) }}</dd></div></dl></div>
          <div class="detail-panel"><h2>Your ascents</h2><dl class="detail-grid"><div><dt>Attempts</dt><dd>{{ state.selected.climbUser?.totalTries || 0 }}</dd></div><div><dt>Best ascent</dt><dd>{{ ascentLabel(state.selected.climbUser?.tickType) }}</dd></div><div><dt>Your grade</dt><dd>{{ frenchGrade(state.selected.climbUser?.grade) }}</dd></div><div><dt>Score</dt><dd>{{ score(state.selected.grade, state.selected.climbUser?.tickType || 0) ?? '—' }}</dd></div><div><dt>First attempt</dt><dd>{{ dateLabel(state.selected.climbUser?.triedFirstAtDate) }}</dd></div><div><dt>First top</dt><dd>{{ dateLabel(state.selected.climbUser?.tickedFirstAtDate) }}</dd></div></dl></div>
          <div class="detail-panel"><div class="section-heading"><h2>Community</h2><span v-if="state.communityBusy" class="spinner" aria-label="Loading community data" /></div><div v-if="state.communityError" class="notice error" role="alert">{{ state.communityError }}<button @click="openRoute(state.selected!)">Retry</button></div><template v-if="state.community"><h3>Grade votes</h3><div class="vote-chips"><span v-for="vote in state.community.gradeVoteStats" :key="vote.grade" class="badge">{{ frenchGrade(vote.grade) }} · {{ vote.count }} {{ vote.count === 1 ? 'vote' : 'votes' }}</span><p v-if="!state.community.gradeVoteStats.length" class="muted">No grade votes yet.</p></div><h3>Rating <span class="muted">{{ communityRating }}</span></h3><div class="rating-row" v-for="vote in state.community.ratingVoteStats" :key="vote.stars"><span>{{ vote.stars }} stars</span><meter :value="vote.count" :max="Math.max(...state.community.ratingVoteStats.map(item => item.count), 1)" :aria-label="`${vote.count} votes for ${vote.stars} stars`" /><span>{{ vote.count }}</span></div><h3>Toppers</h3><p v-if="state.community.toppersUnavailable" class="muted">TopLogger has not made the toppers list available.</p><p v-else-if="!state.community.toppers.length" class="muted">No public tops yet.</p><ul v-else class="toppers"><li v-for="topper in state.community.toppers" :key="topper.id"><AppIcon name="check" /><span>{{ topper.user?.fullName || 'Anonymous climber' }}</span><span class="muted">{{ ascentLabel(topper.tickType) }}<template v-if="topper.grade"> · {{ frenchGrade(topper.grade) }}</template></span></li></ul></template></div>
        </section>

        <section v-else-if="state.tab === 'account'">
          <div class="page-heading"><div><h1>Account</h1></div></div>
          <div class="profile-card"><span class="avatar">{{ state.user.fullName?.slice(0, 1) || '?' }}</span><div><h2>{{ state.user.fullName }}</h2><p>{{ gyms.find(gym => gym.id === state.gymId)?.name || 'No gym selected' }}</p></div><span class="badge" :class="{ topped: !state.needsLogin }">{{ state.needsLogin ? 'Reconnect' : 'Connected' }}</span></div>
          <div v-if="state.needsLogin" class="detail-panel"><h2>Reconnect TopLogger</h2><button v-if="Capacitor.isNativePlatform()" class="primary full" :disabled="signingIn || state.busy" @click="signIn">Sign in with TopLogger</button><p class="muted login-alternative">Or paste a fresh refresh token from TopLogger’s browser storage (<code>tl-auth → refresh.token</code>).</p><form @submit.prevent="submitToken"><label for="reconnect-token">Refresh token</label><input id="reconnect-token" v-model="token" type="password" autocomplete="off" autocapitalize="none" spellcheck="false" required><button class="primary full" :disabled="state.busy || !token.trim()">{{ state.busy ? 'Connecting…' : 'Reconnect' }}</button></form></div>
          <div class="detail-panel"><h2>Appearance</h2><div class="segmented"><button v-for="theme in (['system', 'light', 'dark'] as const)" :key="theme" :aria-pressed="state.theme === theme" @click="state.theme = theme">{{ theme[0]!.toUpperCase() + theme.slice(1) }}</button></div></div>
          <div class="detail-panel"><h2>Saved data</h2><p class="muted">Clear offline data. Keep your login.</p><button class="secondary full" :disabled="state.busy" @click="clearSaved">Clear saved data</button></div>
          <button class="danger full" @click="logout">Sign out and remove account data</button><p class="account-footer">TopLogger Plus {{ appVersion }}</p>
        </section>

        <section v-else>
          <div class="page-heading"><div><h1>{{ title }}</h1></div><button class="icon-button" aria-label="Refresh TopLogger data" :disabled="state.busy || state.needsLogin" @click="refresh"><span v-if="state.busy" class="spinner" /><AppIcon v-else name="refresh" /></button></div>
          <template v-if="state.tab === 'top'">
            <div class="segmented period"><button v-for="days in [60, 120, 180]" :key="days" :aria-pressed="state.days === days" @click="state.days = days">{{ days / 30 }} months</button></div>
            <div class="progress-card"><div><span class="section-kicker">Average</span><strong>{{ state.historyReady ? averageLabel(ranked) : '—' }}</strong><span>{{ state.historyReady ? `${ranked.length} ${ranked.length === 1 ? 'route' : 'routes'} · past ${state.days} days` : state.busy ? 'Loading ascent history…' : 'Ascent history unavailable' }}</span></div></div>
            <div v-if="state.historyError" class="notice error" role="alert">{{ state.historyError }}<button :disabled="state.busy || state.needsLogin" @click="refresh">Retry</button></div>
            <div class="section-heading"><h2>Best ascents</h2><span class="muted">{{ ranked.length }} / 10</span></div>
            <RouteCard v-for="(item, index) in ranked" :key="item.route.id" :route="item.route" :rank="index + 1" :ranked="item" @open="showRoute" />
            <div v-if="!ranked.length" class="empty-state" role="status"><AppIcon name="top" /><h2>{{ state.busy ? 'Loading history' : state.historyReady ? 'No ascents in this period' : 'Ascent history unavailable' }}</h2><p>{{ state.busy ? 'Including routes that have left the wall.' : state.historyReady ? 'No qualifying ascents in this period. Try a longer window.' : 'Connect and refresh to load verified ascent history.' }}</p></div>
            <details class="scoring-note"><summary>Scoring</summary><p>Redpoint: route grade. Flash: grade + 10. Onsight: grade + 15. Your best qualifying ascent per route counts, including removed routes. Ties favor the most recent ascent. The average uses the routes available, up to ten.</p></details>
          </template>
          <template v-else>
            <div v-if="state.tab === 'routes'" class="route-tabs" aria-label="Route completion"><button :aria-pressed="!status" @click="status = ''">All <span>{{ activeRoutes.length }}</span></button><button :aria-pressed="status === 'todo'" @click="status = 'todo'">To do <span>{{ activeRoutes.length - topped }}</span></button><button :aria-pressed="status === 'topped'" @click="status = 'topped'"><AppIcon name="check" />Done <span>{{ topped }}</span></button></div>
            <div class="search-row"><label class="search"><AppIcon name="search" /><input v-model="search" type="search" placeholder="Search" aria-label="Search routes"></label><button class="filter-button" aria-label="Filters" :aria-expanded="showFilters" aria-controls="filters" @click="showFilters = !showFilters"><AppIcon name="filter" /><span>Filters<template v-if="filterCount"> · {{ filterCount }}</template></span></button></div>
            <div v-if="showFilters" id="filters" class="filters"><label>Grade<select v-model="grade" aria-label="Grade"><option value="">All grades</option><option v-for="value in grades" :key="value" :value="String(value)">{{ frenchGrade(value) }}</option></select></label><fieldset class="wall-filter"><legend>Walls</legend><button class="text-button" :aria-pressed="!selectedWalls.length" @click="allWalls">All walls</button><div class="wall-options"><label v-for="value in walls" :key="value"><input v-model="selectedWalls" type="checkbox" :value="value" @change="wallsChanged = true"><span>{{ value }}</span></label></div></fieldset><label>Hold color<select v-model="color" aria-label="Hold color"><option value="">All colors</option><option v-for="value in colors" :key="value!" :value="value">{{ value }}</option></select></label><label>Ascent status<select v-model="status" aria-label="Ascent status"><option value="">Any status</option><option value="untried">To try</option><option value="attempted">Attempted</option><option value="todo">Not done</option><option value="topped">Done</option></select></label><button class="text-button" @click="resetFilters">Reset filters</button></div>
            <div class="list-count muted"><span v-if="selectedWalls.length">{{ selectedWalls[0] }}<template v-if="selectedWalls.length > 1"> +{{ selectedWalls.length - 1 }}</template> · </span>{{ filtered.length }} {{ filtered.length === 1 ? 'route' : 'routes' }}</div>
            <RouteCard v-for="route in filtered" :key="route.id" :route="route" @open="showRoute" />
            <div v-if="!filtered.length" class="empty-state" role="status"><AppIcon name="routes" /><h2>{{ state.busy ? 'Loading routes' : search || filterCount ? 'No matching routes' : 'No routes to show' }}</h2><p>{{ state.busy ? 'Fetching the latest from TopLogger.' : search || filterCount ? 'Try a broader search or clear your filters.' : !state.gymId ? 'Choose a gym in TopLogger, then reconnect here.' : 'Refresh to load the routes at this gym.' }}</p><button v-if="search || filterCount" class="secondary" @click="resetFilters">Clear filters</button></div>
          </template>
          <div class="sync-footer" role="status"><span :class="['sync-dot', { busy: state.busy }]" />{{ state.busy ? 'Syncing with TopLogger…' : synced ? `Last synced ${synced}` : 'Not synced yet' }}<span v-if="state.historyError && state.tab === 'top' && state.historyReady"> · Saved history</span></div>
        </section>
      </template>
    </main>
    <nav v-if="state.user" class="bottom-nav" aria-label="Main navigation"><button v-for="tab in tabs" :key="tab.id" :aria-current="state.tab === tab.id ? 'page' : undefined" @click="changeTab(tab.id)"><AppIcon :name="tab.id" /><span>{{ tab.title }}</span></button></nav>
  </div>
</template>
