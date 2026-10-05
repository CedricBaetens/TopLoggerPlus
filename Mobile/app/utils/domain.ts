export interface Gym { id: string; name: string }
export function defaultWallSelection(gymName: string, walls: string[]): string[] {
  return /\bklimax\b/i.test(gymName) ? walls.filter(wall => /^sector\s+[1-7]$/i.test(wall.trim())) : []
}
export interface User { id: string; fullName: string; gym: Gym | null; gymUserFavorites: { gym: Gym }[] }
export interface ClimbUser {
  grade: number | null; tickType: number; totalTries: number; totalTicks?: number | null
  triedFirstAtDate: string | null; tickedFirstAtDate: string | null
}
export interface Route {
  leadEnabled?: boolean; leadRequired?: boolean
  id: string; grade: number; name: string | null; label: string | null
  wall: { nameLoc: string } | null; holdColor: { color: string; nameLoc: string } | null
  setterName: string | null; climbSetters: { gymAdmin: { name: string } }[]
  inAt: string | null; outAt: string | null; outPlannedAt: string | null; climbUser: ClimbUser | null
}
export interface Ascent {
  id: string; gymId: string; climbId: string; climbType: string; tickType: number
  climbedAtDate: string; valid: boolean; ticked: boolean; topped: boolean
}
export interface RouteLog {
  id: string; gymId: string; climbId: string; climbedAtDate: string; valid: boolean; topped: boolean; ticked: boolean | null
  tickType: number | null; tickIndex: number | null; tryIndex: number | null; lead: boolean | null; autoAdded: boolean
}
export interface RankedRoute { route: Route; ascent: Ascent; score: number }
export const FRENCH_GRADES = [200, 300, 333, 367, 400, 433, 467, ...Array.from({ length: 5 }, (_, index) => [0, 17, 33, 50, 67, 83].map(offset => (index + 5) * 100 + offset)).flat()].filter(grade => grade <= 950)
export function gradeChoices(grade: number): (number | null)[] {
  const index = FRENCH_GRADES.findIndex(value => frenchGrade(value) === frenchGrade(grade))
  return index < 0 ? [] : [-2, -1, 0, 1, 2].map(offset => FRENCH_GRADES[index + offset] ?? null)
}
export interface Community {
  gradeVoteStats: { grade: number; count: number }[]
  ratingVoteStats: { stars: number; count: number }[]
  toppers: { id: string; user: { id: string; fullName: string } | null; tickType: number; grade: number | null }[]
  toppersUnavailable?: boolean
}
export function frenchGrade(grade: number | null | undefined): string {
  if (!grade || !Number.isFinite(grade)) return '?'
  if (grade < 500) {
    const value = FRENCH_GRADES.filter(value => value < 500).reduce((best, value) => Math.abs(value - grade) < Math.abs(best - grade) ? value : best, 200)
    return ['2', '3a', '3b', '3c', '4a', '4b', '4c'][FRENCH_GRADES.indexOf(value)]!
  }
  const letters = ['a', 'a+', 'b', 'b+', 'c', 'c+']
  const rest = grade % 100
  return `${Math.floor(grade / 100)}${letters[rest < 9 ? 0 : rest < 25 ? 1 : rest < 42 ? 2 : rest < 59 ? 3 : rest < 75 ? 4 : 5]}`
}
export function score(grade: number, tickType: number): number | null {
  return tickType === 1 ? grade : tickType === 2 ? grade + 10 : tickType === 3 ? grade + 15 : null
}
export function dateValue(value: string): number {
  // TopLogger calendar dates belong to the climber's day, rather than midnight UTC.
  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T00:00:00`).getTime() : new Date(value).getTime()
}
export function isActive(route: Route, now = Date.now()): boolean {
  return (!route.inAt || dateValue(route.inAt) <= now) && (!route.outAt || dateValue(route.outAt) > now)
}
export function topTen(routes: Route[], logs: Ascent[], gymId: string, days: number, now = new Date()): RankedRoute[] {
  const cutoff = new Date(now); cutoff.setHours(0, 0, 0, 0); cutoff.setDate(cutoff.getDate() - days)
  const byId = new Map(routes.map(route => [route.id, route]))
  const best = new Map<string, RankedRoute>()
  for (const ascent of logs) {
    const route = byId.get(ascent.climbId), at = dateValue(ascent.climbedAtDate)
    if (!route || ascent.gymId !== gymId || ascent.climbType !== 'route' || !ascent.valid || !ascent.ticked || !ascent.topped || !Number.isFinite(at) || at < cutoff.getTime() || at > now.getTime()) continue
    const value = score(route.grade, ascent.tickType)
    if (value === null || route.grade <= 0) continue
    const previous = best.get(route.id)
    if (!previous || value > previous.score || (value === previous.score && at > dateValue(previous.ascent.climbedAtDate))) best.set(route.id, { route, ascent, score: value })
  }
  return [...best.values()].sort((a, b) => b.score - a.score || dateValue(b.ascent.climbedAtDate) - dateValue(a.ascent.climbedAtDate) || a.route.id.localeCompare(b.route.id)).slice(0, 10)
}
export const ascentLabel = (tickType?: number) => ['Not topped', 'Redpoint', 'Flash', 'Onsight'][tickType ?? 0] ?? 'Not topped'
// Number of tops including repeats; a missing or malformed total counts as none.
export const topCount = (climbUser?: ClimbUser | null) => climbUser?.tickType && Number.isInteger(climbUser.totalTicks) ? climbUser.totalTicks! : 0
// Your log: newest first, without invalid logs or the attempts TopLogger generates for a send.
export function visibleLogs(logs: RouteLog[]): RouteLog[] {
  return logs.filter(log => log.valid && !log.autoAdded).sort((a, b) => dateValue(b.climbedAtDate) - dateValue(a.climbedAtDate) || (b.tryIndex ?? 0) - (a.tryIndex ?? 0))
}
export function logKind(log: RouteLog): string {
  if (!log.topped) return 'Try'
  return (log.tickIndex ?? 0) > 0 ? 'Repeat' : ascentLabel(log.tickType ?? 1)
}
export function dayLabel(value: string, now = new Date()): string {
  const at = dateValue(value)
  if (!Number.isFinite(at)) return '—'
  const today = new Date(now); today.setHours(0, 0, 0, 0)
  const day = new Date(at); day.setHours(0, 0, 0, 0)
  const days = Math.round((today.getTime() - day.getTime()) / 86400000)
  if (days === 0) return 'Today'
  if (days === 1) return 'Yesterday'
  return day.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', ...(day.getFullYear() !== today.getFullYear() ? { year: 'numeric' } : {}) })
}
export function dateLabel(value: string | null | undefined): string {
  return value && Number.isFinite(dateValue(value)) ? new Date(dateValue(value)).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : '—'
}
export function colorValue(color?: string): string {
  return color && /^#(?:[a-f\d]{3}|[a-f\d]{6}|[a-f\d]{8})$/i.test(color) ? color : '#888888'
}
export function averageLabel(ranked: RankedRoute[]): string {
  if (!ranked.length) return '—'
  const value = Math.ceil(ranked.reduce((sum, item) => sum + item.score, 0) / ranked.length)
  const thresholds = [0, 17, 33, 50, 67, 83], rest = value % 100
  const index = thresholds.findLastIndex(threshold => rest >= threshold)
  const width = (thresholds[index + 1] ?? 100) - thresholds[index]!
  return `${Math.floor(value / 100)}${['a','a+','b','b+','c','c+'][index]} · ${Math.round((rest - thresholds[index]!) / width * 100)}%`
}
