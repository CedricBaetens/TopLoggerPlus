import test from 'node:test'
import assert from 'node:assert/strict'
import { frenchGrade, score, topTen, isActive, averageLabel, colorValue, defaultWallSelection, type Route, type Ascent } from '../app/utils/domain'
import { cacheKey, clearCache, readCache, writeCache } from '../app/utils/cache'
import { ApiError, TopLoggerClient, historyFailure, type Tokens, type Transport } from '../app/utils/toplogger'
import { REFRESH, USER, ROUTES, DAYS, HISTORY } from '../app/utils/queries'

const route = (id: string, grade = 600, outAt: string | null = null): Route => ({ id, grade, outAt, name: null, label: '1', wall: null, holdColor: null, setterName: null, climbSetters: [], inAt: null, outPlannedAt: null, climbUser: null })
const log = (climbId: string, tickType = 1, climbedAtDate = '2026-09-20', gymId = 'gym'): Ascent => ({ id: `${climbId}-${climbedAtDate}`, gymId, climbId, climbType: 'route', tickType, climbedAtDate, valid: true, ticked: true, topped: true })
const now = new Date('2026-09-28T12:00:00')
const tokens = (expiresAt = '2099-01-01T00:00:00Z', access = 'access'): Tokens => ({ access: { token: access, expiresAt }, refresh: { token: 'header.payload.signature', expiresAt: '2099-01-01T00:00:00Z' } })

test('Klimax defaults to sectors 1–7, preserving case; other gyms default to all walls', () => {
  const walls = ['Sector 1', 'sector 2', 'Sector 3', 'Sector 4', 'Sector 5', 'Sector 6', 'Sector 7', 'Sector 8', 'Sector 10', 'Binnen 1', 'Buiten 1']
  assert.deepEqual(defaultWallSelection('Klimax', walls), walls.slice(0, 7))
  assert.deepEqual(defaultWallSelection('KLIMAX', [' sector 2 ']), [' sector 2 '])
  assert.deepEqual(defaultWallSelection('Other gym', walls), [])
  assert.deepEqual(defaultWallSelection('Klimax', ['Buiten 1']), [])
})

test('History errors report server reasons without credentials', () => {
  const body = { errors: [{ message: 'Missing climbedAtDate: header.payload.signature user@example.com' }] }
  const message = historyFailure(HISTORY, body)
  assert(message.includes('Missing climbedAtDate')); assert(!message.includes('header.payload.signature')); assert(!message.includes('user@example.com'))
  assert(!historyFailure(REFRESH, body).includes('Missing'))
})

test('History requests include the selected gym and account', async () => {
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    assert.equal((variables.pagination as { perPage: number }).perPage, 100)
    if (query === DAYS) return { climbDaysPaginated: { data: [{ id: 'day', gymId: 'gym', statsAtDate: '2026-09-20' }, { id: 'other', gymId: 'other-gym', statsAtDate: '2026-09-21' }], pagination: { total: 2, page: 1, perPage: 200 } } }
    assert.equal(query, HISTORY); assert.equal(variables.gymId, 'gym'); assert.equal(variables.userId, 'user')
    assert.equal(variables.date, '2026-09-20')
    return { climbLogs: { data: [log('route')], pagination: { total: 1, page: 1, perPage: 200 } } }
  }, undefined, false)
  await client.connect('header.payload.signature')
  assert.equal((await client.history('gym', 'user')).length, 1)
})

test('French grades, bonuses, empty averages and untrusted color values', () => {
  assert.deepEqual([0, 600, 608, 609, 624, 625, 641, 642, 658, 659, 674, 675, 683].map(frenchGrade), ['?', '6a', '6a', '6a+', '6a+', '6b', '6b', '6b+', '6b+', '6c', '6c', '6c+', '6c+'])
  assert.deepEqual([0, 1, 2, 3, 4].map(type => score(600, type)), [null, 600, 610, 615, null])
  assert.equal(averageLabel([]), '—'); assert.equal(colorValue('url(https://example.com)'), '#888888')
})
test('Route lifecycle includes future removal and excludes removed or not-yet-set routes', () => {
  assert(isActive(route('live'), now.getTime()))
  assert(!isActive(route('gone', 600, '2026-09-27'), now.getTime()))
  assert(isActive(route('later', 600, '2026-09-29'), now.getTime()))
  assert(!isActive({ ...route('new'), inAt: '2026-09-29' }, now.getTime()))
  assert(!isActive(route('invalid', 600, 'nonsense'), now.getTime()))
})
test('Top 10 keeps the best ascent per route, breaks ties by date, and includes removed routes', () => {
  const ranked = topTen([route('a'), route('b'), route('removed', 700, '2026-09-01')], [log('a', 2, '2026-09-10'), log('a', 1), log('a', 2, '2026-09-25'), log('b', 2, '2026-09-24'), log('removed')], 'gym', 60, now)
  assert.deepEqual(ranked.map(item => [item.route.id, item.score, item.ascent.climbedAtDate]), [['removed', 700, '2026-09-20'], ['a', 610, '2026-09-25'], ['b', 610, '2026-09-24']])
})
test('Top 10 respects period boundaries, account gym, validity, route type, and future dates', () => {
  const start = new Date(now); start.setDate(start.getDate() - 60)
  const day = `${start.getFullYear()}-${String(start.getMonth() + 1).padStart(2, '0')}-${String(start.getDate()).padStart(2, '0')}`
  assert.equal(topTen([route('a')], [log('a', 1, day)], 'gym', 60, now).length, 1)
  for (const item of [log('a', 1, '2026-01-01'), log('a', 1, '2026-09-29'), log('a', 1, '2026-09-20', 'other'), { ...log('a'), valid: false }, { ...log('a'), ticked: false }, { ...log('a'), topped: false }, { ...log('a'), climbType: 'boulder' }]) assert.equal(topTen([route('a')], [item], 'gym', 60, now).length, 0)
  const many = Array.from({ length: 15 }, (_, index) => route(String(index), 600 + index))
  assert.equal(topTen(many, many.map(item => log(item.id)), 'gym', 180, now).length, 10)
})
test('Cache snapshots are scoped, versioned, safe on corruption, and selectively cleared', () => {
  const storage: Record<string, any> = {}
  Object.defineProperties(storage, { getItem: { value: (key: string) => storage[key] ?? null }, setItem: { value: (key: string, value: string) => { storage[key] = value } }, removeItem: { value: (key: string) => { delete storage[key] } } })
  Object.defineProperty(globalThis, 'localStorage', { value: storage, configurable: true })
  const a = cacheKey('user:a', 'gym:b', 'routes'), b = cacheKey('user', 'a:gym:b', 'routes'), c = cacheKey('other', 'gym:b', 'routes')
  assert.notEqual(a, b); writeCache(a, [1]); writeCache(b, [2]); writeCache(c, [3]); assert.deepEqual(readCache(a)?.data, [1])
  storage[a] = '{broken'; assert.equal(readCache(a), null)
  clearCache('user:a'); assert.equal(storage[a], undefined); assert.deepEqual(readCache(b)?.data, [2]); assert.deepEqual(readCache(c)?.data, [3])
  storage[a] = JSON.stringify({ version: 2, at: new Date().toISOString(), data: [] }); assert.equal(readCache(a), null)
  clearCache(); assert.equal(Object.keys(storage).length, 0)
})
test('Concurrent requests share a token refresh and retry expired access once', async () => {
  let refreshCount = 0, persisted: Tokens | null = tokens('2000-01-01T00:00:00Z'), reads = 0
  const storage = { read: async () => ({ value: JSON.stringify(persisted) }), write: async ({ value }: { value: string }) => { persisted = JSON.parse(value) }, clear: async () => { persisted = null } }
  const send: Transport = async (query, _, token) => {
    if (query === REFRESH) { refreshCount++; await new Promise(resolve => setTimeout(resolve, 5)); return { tokens: tokens() } }
    reads++; assert.equal(token, 'access'); return { userMe: { id: 'u', fullName: 'Climber', gym: null, gymUserFavorites: [] } }
  }
  const client = new TopLoggerClient(send, storage, true)
  assert(await client.restore()); await Promise.all([client.user(), client.user()]); assert.equal(refreshCount, 1); assert.equal(reads, 2)
  await client.disconnect(); assert.equal(persisted, null); await assert.rejects(client.user(), (error: ApiError) => error.kind === 'auth')
})
test('Authentication errors are distinct from network failures; only auth failures refresh', async () => {
  let refreshCount = 0, userCalls = 0
  const client = new TopLoggerClient(async query => {
    if (query === REFRESH) { refreshCount++; return { tokens: tokens() } }
    userCalls++; throw new ApiError(userCalls === 1 ? 'auth' : 'network', 'test failure')
  }, undefined, false)
  await client.connect('header.payload.signature')
  await assert.rejects(client.query(USER), (error: ApiError) => error.kind === 'network')
  assert.equal(refreshCount, 2); assert.equal(userCalls, 2)
})
test('Logout prevents an in-flight refresh from re-saving credentials', async () => {
  let resolve!: (value: any) => void, saved = 0
  const pending = new Promise<any>(done => { resolve = done })
  const client = new TopLoggerClient(async () => pending, { read: async () => ({}), write: async () => { saved++ }, clear: async () => {} }, true)
  const connection = client.connect('header.payload.signature')
  await client.disconnect(); resolve({ tokens: tokens() })
  await assert.rejects(connection, (error: ApiError) => error.kind === 'auth'); assert.equal(saved, 0)
})
test('Pagination collects every page and refuses incomplete history', async () => {
  let pages = 0
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    assert.equal(query, ROUTES); pages++
    return { climbs: { data: [route(String(pages))], pagination: { total: 2, page: (variables.pagination as any).page, perPage: 1 } } }
  }, undefined, false)
  await client.connect('header.payload.signature'); assert.equal((await client.routes('gym', 'user')).length, 2); assert.equal(pages, 2)
  const bad = new TopLoggerClient(async query => query === REFRESH ? { tokens: tokens() } : { climbs: { data: [], pagination: { total: 2, page: 1, perPage: 1 } } }, undefined, false)
  await bad.connect('header.payload.signature'); await assert.rejects(bad.routes('gym', 'user'), (error: ApiError) => error.kind === 'api')
})
