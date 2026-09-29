<script setup lang="ts">
import { Capacitor } from '@capacitor/core'
import { App } from '@capacitor/app'
import { loginToken } from './utils/login'
import { ascentLabel, averageLabel, dateLabel, defaultWallSelection, frenchGrade, gradeChoices, isActive, score } from './utils/domain'

const { adventureProgress, resetAdventure, state, gyms, activeRoutes, ranked, initialize, connect, refresh, selectGym, selectTab, openRoute, closeRoute, submitAscent, submitGrade, logout, clearSaved } = useTopLogger()
const showFilters = ref(false), search = ref('')
const signingIn = ref(false)
const appVersion = ref('3.0.0')
const grade = ref(''), selectedWalls = ref<string[]>([]), color = ref(''), status = ref('')
const wallsChanged = ref(false)
const root = ref<HTMLElement | null>(null), detailHeading = ref<HTMLElement | null>(null)
const tabs = [{ id: 'routes', title: 'Routes' }, { id: 'top', title: 'Top 10' }, { id: 'adventure', title: 'Adventure' }, { id: 'account', title: 'Account' }] as const
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
const gradeVotes = computed(() => [...(state.community?.gradeVoteStats ?? [])].filter(vote => vote.count > 0).sort((a, b) => a.grade - b.grade))
const gradeVoteTotal = computed(() => gradeVotes.value.reduce((total, vote) => total + vote.count, 0))
const mostVotedGrades = computed(() => {
  const highest = Math.max(...gradeVotes.value.map(vote => vote.count), 0)
  return gradeVotes.value.filter(vote => vote.count === highest).map(vote => frenchGrade(vote.grade)).join(' / ')
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
async function signIn() {
  signingIn.value = true; state.error = ''
  try { const value = await loginToken(); await connect(value) }
  catch (error) { state.error = error instanceof Error ? error.message : 'Sign-in could not be completed. Please try again.' }
  finally { signingIn.value = false }
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
      <label v-if="state.user && gyms.length" class="gym-picker"><span class="sr-only">Selected gym</span><select aria-label="Selected gym" :value="state.gymId" :disabled="state.ascentBusy" @change="selectGym(($event.target as HTMLSelectElement).value)"><option v-for="gym in gyms" :key="gym.id" :value="gym.id">{{ gym.name }}</option></select></label>
      
    </header>

    <main id="main" :class="{ 'login-main': !state.user }">
      <div v-if="state.user" role="status" aria-live="polite" aria-atomic="true"><p v-if="state.adventureMessage" class="notice">{{ state.adventureMessage }}</p></div>
      <p v-if="state.user && state.adventureWarning" class="notice error" role="alert">{{ state.adventureWarning }}</p>
      <div v-if="!state.initialized" class="empty-state" role="status"><span class="spinner" /><h1>Getting things ready</h1><p>Opening your saved connection.</p></div>
      <section v-else-if="!state.user" class="onboarding">
        <h1>Connect TopLogger</h1>
        <div class="connect-card">

          <div v-if="state.error" class="notice error" role="alert">{{ state.error }}</div>
          <button v-if="Capacitor.isNativePlatform()" class="primary full" :disabled="signingIn || state.busy" @click="signIn"><AppIcon name="connect" />{{ signingIn ? 'Waiting for TopLogger…' : 'Sign in with TopLogger' }}</button>
          <p v-if="!Capacitor.isNativePlatform()" class="muted">Sign in using the Android app.</p>
          <p class="privacy-note">{{ Capacitor.isNativePlatform() ? 'Encrypted on your device. No Plus server. No password stored.' : 'Browser preview.' }}</p>
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
          <div v-if="isActive(state.selected) || state.selected.climbUser?.tickType" class="detail-panel">
            <div class="ascent-buttons" aria-label="Log ascent">
              <button v-for="type in [3, 2, 1]" :key="type" class="primary" :aria-pressed="state.selected.climbUser?.tickType === type" :disabled="!isActive(state.selected) || state.ascentBusy || state.busy || state.needsLogin || !state.connected || !!state.selected.climbUser?.tickType || (type > 1 && !!state.selected.climbUser?.totalTries) || typeof state.selected.leadEnabled !== 'boolean'" :aria-busy="state.ascentAction === `${state.selected.id}:ascent:${type}`" @click="submitAscent(type)"><span v-if="state.ascentAction === `${state.selected.id}:ascent:${type}`" class="spinner" aria-hidden="true" /><AppIcon v-else :name="type === 3 ? 'doubleCheck' : type === 2 ? 'flash' : 'check'" />{{ ascentLabel(type) }}</button>
            </div>
            <div class="try-row">
              <button class="secondary" :disabled="!isActive(state.selected) || state.ascentBusy || state.busy || state.needsLogin || !state.connected || typeof state.selected.leadEnabled !== 'boolean'" :aria-busy="state.ascentAction === `${state.selected.id}:ascent:0`" @click="submitAscent(0)"><span v-if="state.ascentAction === `${state.selected.id}:ascent:0`" class="spinner" aria-hidden="true" /><AppIcon v-else name="plus" />Try</button>
              <span class="try-count" role="status"><strong>{{ state.selected.climbUser?.totalTries || 0 }}</strong> {{ state.selected.climbUser?.totalTries === 1 ? 'try' : 'tries' }}</span>
            </div>
            <button v-if="state.selected.climbUser?.tickType" class="secondary full unsend-button" :disabled="state.ascentBusy || state.busy || state.needsLogin || !state.connected" :aria-busy="state.ascentAction === `${state.selected.id}:ascent:-1`" @click="submitAscent(-1)"><span v-if="state.ascentAction === `${state.selected.id}:ascent:-1`" class="spinner" aria-hidden="true" /><AppIcon v-else name="back" />Unsend</button>
            <template v-if="gradeChoices(state.selected.grade).length">
              <h3 class="grade-vote-title">Your grade</h3>
              <div class="grade-buttons" role="group" aria-label="Vote on the grade">
                <button v-for="(grade, index) in gradeChoices(state.selected.grade)" :key="index" class="secondary" :class="{ 'current-grade': index === 2, 'button-loading': state.ascentAction === `${state.selected.id}:grade:${grade}` }" :aria-busy="state.ascentAction === `${state.selected.id}:grade:${grade}`" :aria-pressed="grade !== null && state.selected.climbUser?.grade === grade" :disabled="grade === null || state.ascentBusy || state.busy || state.needsLogin || !state.connected || state.selected.climbUser?.grade === grade" @click="grade !== null && submitGrade(grade)"><span class="button-label">{{ grade === null ? '—' : frenchGrade(grade) }}</span><span v-if="state.ascentAction === `${state.selected.id}:grade:${grade}`" class="spinner" aria-hidden="true" /></button>
              </div>
            </template>
            <div v-if="state.ascentError" class="notice error" role="alert">{{ state.ascentError }}</div>
          </div>
          <div class="detail-panel"><h2>Route</h2><dl class="detail-grid"><div><dt>Wall</dt><dd>{{ state.selected.wall?.nameLoc || 'Unknown' }}</dd></div><div><dt>Rope / label</dt><dd>{{ state.selected.label || '—' }}</dd></div><div><dt>Hold color</dt><dd>{{ state.selected.holdColor?.nameLoc || 'Unknown' }}</dd></div><div><dt>Setter</dt><dd>{{ [...(state.selected.climbSetters || []).map(setter => setter.gymAdmin.name), ...(state.selected.setterName ? [state.selected.setterName] : [])].join(', ') || 'Unknown' }}</dd></div><div><dt>Set on</dt><dd>{{ dateLabel(state.selected.inAt) }}</dd></div><div><dt>Leaving / removed</dt><dd>{{ dateLabel(state.selected.outAt || state.selected.outPlannedAt) }}</dd></div></dl></div>
          <div class="detail-panel"><h2>Your ascents</h2><dl class="detail-grid"><div><dt>Attempts</dt><dd>{{ state.selected.climbUser?.totalTries || 0 }}</dd></div><div><dt>Best ascent</dt><dd>{{ ascentLabel(state.selected.climbUser?.tickType) }}</dd></div><div><dt>Your grade</dt><dd>{{ frenchGrade(state.selected.climbUser?.grade) }}</dd></div><div><dt>Score</dt><dd>{{ score(state.selected.grade, state.selected.climbUser?.tickType || 0) ?? '—' }}</dd></div><div><dt>First attempt</dt><dd>{{ dateLabel(state.selected.climbUser?.triedFirstAtDate) }}</dd></div><div><dt>First top</dt><dd>{{ dateLabel(state.selected.climbUser?.tickedFirstAtDate) }}</dd></div></dl></div>
          <div class="detail-panel"><div class="section-heading"><h2>Community</h2><span v-if="state.communityBusy" class="spinner" aria-label="Loading community data" /></div><div v-if="state.communityError" class="notice error" role="alert">{{ state.communityError }}<button @click="openRoute(state.selected!)">Retry</button></div><template v-if="state.community"><h3>Grade votes</h3>
            <template v-if="gradeVoteTotal">
              <div class="grade-vote-summary"><strong>{{ mostVotedGrades }}</strong><span>Most voted · {{ gradeVoteTotal }} {{ gradeVoteTotal === 1 ? 'vote' : 'votes' }}</span></div>
              <ul class="grade-vote-list" aria-label="Community grade votes">
                <li v-for="vote in gradeVotes" :key="vote.grade" :class="{ 'your-grade-vote': vote.grade === state.selected.climbUser?.grade }">
                  <div class="grade-vote-label"><strong>{{ frenchGrade(vote.grade) }}</strong><span v-if="vote.grade === state.selected.grade" class="grade-vote-tag">Route grade</span><span v-if="vote.grade === state.selected.climbUser?.grade" class="grade-vote-tag your-vote-tag">Your vote</span></div>
                  <div class="grade-vote-result"><span class="grade-vote-track" aria-hidden="true"><span :style="{ width: `${vote.count / gradeVoteTotal * 100}%` }" /></span><span class="grade-vote-count"><strong>{{ vote.count }}</strong> {{ vote.count === 1 ? 'vote' : 'votes' }} <span class="muted">· {{ Math.round(vote.count / gradeVoteTotal * 100) }}%</span></span></div>
                </li>
              </ul>
            </template>
            <p v-else class="muted">No grade votes yet.</p>
            <p v-if="state.selected.climbUser?.grade" class="grade-personal-vote">Your vote: <strong>{{ frenchGrade(state.selected.climbUser.grade) }}</strong></p>
            <h3>Rating <span class="muted">{{ communityRating }}</span></h3><div class="rating-row" v-for="vote in state.community.ratingVoteStats" :key="vote.stars"><span>{{ vote.stars }} stars</span><meter :value="vote.count" :max="Math.max(...state.community.ratingVoteStats.map(item => item.count), 1)" :aria-label="`${vote.count} votes for ${vote.stars} stars`" /><span>{{ vote.count }}</span></div><h3>Toppers</h3><p v-if="state.community.toppersUnavailable" class="muted">TopLogger has not made the toppers list available.</p><p v-else-if="!state.community.toppers.length" class="muted">No public tops yet.</p><ul v-else class="toppers"><li v-for="topper in state.community.toppers" :key="topper.id"><AppIcon name="check" /><span>{{ topper.user?.fullName || 'Anonymous climber' }}</span><span class="muted">{{ ascentLabel(topper.tickType) }}<template v-if="topper.grade"> · {{ frenchGrade(topper.grade) }}</template></span></li></ul></template></div>
        </section>

        <section v-else-if="state.tab === 'adventure'">
          <div class="page-heading"><h1>Adventure</h1></div>
          <div class="detail-panel adventure-level"><span class="section-kicker">Your climbing adventure</span><h2>Level {{ adventureProgress.level }}</h2><p>{{ adventureProgress.xp }} total XP · {{ adventureProgress.sends }} rewarded sends</p><progress :value="adventureProgress.currentXp" :max="500" aria-label="XP toward next level" /><p class="muted">{{ adventureProgress.currentXp }} / 500 XP toward level {{ adventureProgress.level + 1 }}</p></div>
          <div class="detail-panel"><h2>Achievements</h2><p class="muted">Every milestone leads to another. Keep climbing to unlock higher targets.</p><ul class="achievement-list"><li v-for="item in adventureProgress.achievements" :key="item.id"><AppIcon :name="item.unlocked ? 'check' : 'mountain'" /><div><strong>{{ item.name }}</strong><p class="muted">{{ item.description }}</p><template v-if="!item.unlocked"><progress :value="item.current" :max="item.target" :aria-label="`${item.name} progress`" /><p class="muted">{{ item.current.toLocaleString() }} / {{ item.target.toLocaleString() }}</p></template></div><span class="badge" :class="{ topped: item.unlocked }">{{ item.unlocked ? 'Unlocked' : 'Locked' }}</span></li></ul></div>
          <div class="detail-panel"><h2>How XP works</h2><p class="muted">New sends logged here earn 100 XP for Redpoint, 125 for Flash, or 150 for Onsight. Each route counts once. Unsend removes its reward. Attempts and grade votes earn no XP.</p><p class="muted">Progress belongs to this account across all gyms, on this device only. Clearing offline data and signing out keep your adventure.</p><button class="danger full" :disabled="state.ascentBusy" @click="resetAdventure">Reset adventure progress</button></div>
        </section>

        <section v-else-if="state.tab === 'account'">
          <div class="page-heading"><div><h1>Account</h1></div></div>
          <div class="profile-card"><span class="avatar">{{ state.user.fullName?.slice(0, 1) || '?' }}</span><div><h2>{{ state.user.fullName }}</h2><p>{{ gyms.find(gym => gym.id === state.gymId)?.name || 'No gym selected' }}</p></div><span class="badge" :class="{ topped: !state.needsLogin }">{{ state.needsLogin ? 'Reconnect' : 'Connected' }}</span></div>
          <div v-if="state.needsLogin" class="detail-panel"><h2>Reconnect TopLogger</h2><button v-if="Capacitor.isNativePlatform()" class="primary full" :disabled="signingIn || state.busy" @click="signIn">{{ signingIn ? 'Waiting for TopLogger…' : 'Sign in with TopLogger' }}</button><p v-else class="muted">Sign in using the Android app.</p></div>
          <div class="detail-panel"><h2>Appearance</h2><div class="segmented"><button v-for="theme in (['system', 'light', 'dark'] as const)" :key="theme" :aria-pressed="state.theme === theme" @click="state.theme = theme">{{ theme[0]!.toUpperCase() + theme.slice(1) }}</button></div></div>
          <div class="detail-panel"><h2>Saved data</h2><p class="muted">Clear offline data. Keep your login and adventure progress.</p><button class="secondary full" :disabled="state.busy || state.ascentBusy" @click="clearSaved">Clear saved data</button></div>
          <button class="danger full" :disabled="state.ascentBusy" @click="logout">Sign out and clear offline data</button><p class="muted">Adventure progress stays on this device after signing out.</p><p class="account-footer">TopLogger Plus {{ appVersion }}</p>
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
