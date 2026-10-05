import test from 'node:test'
import assert from 'node:assert/strict'
import { frenchGrade, gradeChoices, score, topTen, isActive, averageLabel, colorValue, defaultWallSelection, visibleLogs, logKind, dayLabel, topCount, daysLeft, daysLeftLabel, type Route, type Ascent, type RouteLog } from '../app/utils/domain'
import { cacheKey, clearCache, readCache, writeCache } from '../app/utils/cache'
import { ApiError, TopLoggerClient, historyFailure, type Tokens, type Transport } from '../app/utils/toplogger'
import { REFRESH, USER, ROUTES, DAYS, HISTORY, LOG_ASCENT, ROUTE_LOGS, UNSEND, GRADE_VOTE } from '../app/utils/queries'

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
  assert.equal((await client.history('gym', 'user')).sessions[0]?.logs.length, 1)
})

test('Ascent logging uses TopLogger first-try semantics, current local date, and lead selection', async () => {
  const calls: Record<string, any>[] = []
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    assert.equal(query, LOG_ASCENT); calls.push(variables)
    return { climbUsers: [{ climbId: 'route', grade: null, tickType: 1, totalTries: 2, triedFirstAtDate: null, tickedFirstAtDate: null }] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  const climb = { ...route('route'), leadEnabled: true, leadRequired: true }
  for (const type of [1, 2, 3]) await client.logAscent('gym', 'user', climb, type, true)
  assert.deepEqual(calls.map(call => [call.climbLogTriesBefore, call.climbLogData.foreknowledge]), [[1, true], [0, true], [0, false]])
  for (const call of calls) {
    assert.equal(call.gymId, 'gym'); assert.equal(call.userId, 'user'); assert.deepEqual(call.climbIds, ['route'])
    assert.equal(call.climbLogData.topped, true); assert.equal(call.climbLogData.lead, true); assert.equal(call.climbLogData.zones, 0)
    const date = new Date(call.climbLogData.climbedAtDate + 'T00:00:00')
    assert.equal(date.toDateString(), new Date().toDateString())
  }
  const attempted = { ...climb, climbUser: { grade: null, tickType: 0, totalTries: 3, triedFirstAtDate: null, tickedFirstAtDate: null } }
  await assert.rejects(client.logAscent('gym', 'user', attempted, 2, true))
  await assert.rejects(client.logAscent('gym', 'user', attempted, 3, true))
  await assert.rejects(client.logAscent('gym', 'user', climb, 4, true))
  await assert.rejects(client.logAscent('gym', 'user', route('route'), 1, true))
  assert.equal(calls.length, 3)
  await client.logAscent('gym', 'user', attempted, 1, false)
  assert.equal(calls[3]?.climbLogTriesBefore, 0); assert.equal(calls[3]?.climbLogData.lead, false)
})

test('An uncertain ascent write is never retried', async () => {
  let writes = 0
  const client = new TopLoggerClient(async query => {
    if (query === REFRESH) return { tokens: tokens() }
    writes++; throw new ApiError('network', 'timeout')
  }, undefined, false)
  await client.connect('header.payload.signature')
  await assert.rejects(client.logAscent('gym', 'user', { ...route('route'), leadEnabled: false, leadRequired: false }, 1, false), /Check TopLogger before saving again/)
  assert.equal(writes, 1)
})

test('A try records an untopped attempt without replacing an existing send', async () => {
  let tries = 2
  const climb = { ...route('route'), leadEnabled: false, leadRequired: false, climbUser: { grade: 633, tickType: 1, totalTries: tries, triedFirstAtDate: null, tickedFirstAtDate: null } }
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    assert.equal(query, LOG_ASCENT)
    assert.equal(variables.climbLogTriesBefore, 0)
    const data = variables.climbLogData as Record<string, unknown>
    assert.equal(data.topped, false); assert.equal(data.foreknowledge, false); assert.equal(data.zones, 0)
    return { climbUsers: [{ climbId: climb.id, ...climb.climbUser, totalTries: ++tries }] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  for (const expected of [3, 4]) {
    climb.climbUser = await client.logAscent('gym', 'user', climb, 0)
    assert.equal(climb.climbUser.totalTries, expected); assert.equal(climb.climbUser.tickType, 1); assert.equal(climb.climbUser.grade, 633)
  }
})

test('Five grade choices keep the route grade centered across number boundaries and scale limits', () => {
  assert.deepEqual(gradeChoices(633), [600, 617, 633, 650, 667])
  assert.deepEqual(gradeChoices(700), [667, 683, 700, 717, 733])
  assert.deepEqual(gradeChoices(467), [400, 433, 467, 500, 517])
  assert.deepEqual(gradeChoices(200), [null, null, 200, 300, 333])
  assert.deepEqual(gradeChoices(950), [917, 933, 950, null, null])
  assert.deepEqual(gradeChoices(0), [])
  assert.deepEqual(gradeChoices(633).map(grade => frenchGrade(grade)), ['6a', '6a+', '6b', '6b+', '6c'])
  assert.equal(frenchGrade(433), '4b')
})

test('Grade voting updates only the selected route’s opinion and never retries uncertain writes', async () => {
  let calls = 0, fail = false
  const updated = { climbId: 'route', grade: 650, tickType: 2, totalTries: 1, triedFirstAtDate: null, tickedFirstAtDate: null }
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    assert.equal(query, GRADE_VOTE); calls++
    assert.deepEqual(variables, { gymId: 'gym', userId: 'user', climbIds: ['route'], grade: 650 })
    if (fail) throw new ApiError('network', 'timeout')
    return { climbUsers: [updated] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  assert.deepEqual(await client.voteGrade('gym', 'user', 'route', 650), updated)
  await assert.rejects(client.voteGrade('gym', 'user', 'route', 651), /valid French grade/)
  assert.equal(calls, 1)
  fail = true
  await assert.rejects(client.voteGrade('gym', 'user', 'route', 650), /Check TopLogger/)
  assert.equal(calls, 2)
})

test('Unsend deletes only this route’s valid sends and generated attempts across all pages', async () => {
  let pages = 0, deletes = 0
  const updated = { climbId: 'route', grade: null, tickType: 0, totalTries: 1, triedFirstAtDate: null, tickedFirstAtDate: null }
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    if (query === ROUTE_LOGS) {
      pages++
      assert.equal(variables.gymId, 'gym'); assert.equal(variables.userId, 'user'); assert.equal(variables.climbId, 'route')
      const logs = pages === 1 ? [{ id: 'old-send', topped: true, autoAdded: false }, { id: 'real-attempt', topped: false, autoAdded: false }] : [{ id: 'recent-send', topped: true, autoAdded: false }, { id: 'generated-attempt', topped: false, autoAdded: true }, { id: 'invalid-send', topped: true, autoAdded: false, valid: false }]
      return { climbLogs: { data: logs.map(log => ({ gymId: 'gym', climbId: 'route', climbedAtDate: '2026-09-20', valid: true, ...log })), pagination: { total: 4, page: pages, perPage: 2 } } }
    }
    assert.equal(query, UNSEND); deletes++
    assert.deepEqual(variables.ids, ['old-send', 'recent-send', 'generated-attempt'])
    assert.equal(variables.gymId, 'gym'); assert.equal(variables.userId, 'user')
    return { climbUsers: [updated] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  assert.deepEqual(await client.unsend('gym', 'user', 'route'), updated)
  assert.equal(pages, 2); assert.equal(deletes, 1)
})

test('Unsend never deletes another route’s logs or retries an uncertain delete', async () => {
  let deletes = 0, wrongRoute = true
  const client = new TopLoggerClient(async query => {
    if (query === REFRESH) return { tokens: tokens() }
    if (query === ROUTE_LOGS) return { climbLogs: { data: [{ id: 'log', gymId: 'gym', climbId: wrongRoute ? 'other' : 'route', climbedAtDate: '2026-09-20', valid: true, topped: true, autoAdded: false }], pagination: { total: 1, page: 1, perPage: 100 } } }
    assert.equal(query, UNSEND); deletes++; throw new ApiError('network', 'timeout')
  }, undefined, false)
  await client.connect('header.payload.signature')
  await assert.rejects(client.unsend('gym', 'user', 'route'), /Could not verify/)
  assert.equal(deletes, 0)
  wrongRoute = false
  await assert.rejects(client.unsend('gym', 'user', 'route'), /Check TopLogger/)
  assert.equal(deletes, 1)
})

const routeLog = (id: string, climbedAtDate: string, extra: Partial<RouteLog> = {}): RouteLog => ({ id, gymId: 'gym', climbId: 'route', climbedAtDate, valid: true, topped: true, ticked: true, tickType: 1, tickIndex: 0, tryIndex: 0, lead: false, autoAdded: false, ...extra })

test('Your log lists valid, genuine logs newest first with their kind', () => {
  const logs = visibleLogs([
    routeLog('try', '2026-09-01', { topped: false, ticked: false, tickType: null, tickIndex: null, tryIndex: 1 }),
    routeLog('first', '2026-09-01', { tickType: 3, tryIndex: 2 }),
    routeLog('generated', '2026-09-01', { topped: false, autoAdded: true }),
    routeLog('invalid', '2026-09-20', { valid: false }),
    routeLog('repeat', '2026-09-20', { tickIndex: 1 }),
  ])
  assert.deepEqual(logs.map(log => log.id), ['repeat', 'first', 'try'])
  assert.deepEqual(logs.map(logKind), ['Repeat', 'Onsight', 'Try'])
  assert.equal(dayLabel('2026-09-28', now), 'Today'); assert.equal(dayLabel('2026-09-27', now), 'Yesterday')
  assert.equal(dayLabel('2026-08-12', now), '12 Aug'); assert.equal(dayLabel('2025-08-12', now), '12 Aug 2025')
})

test('Days left matches the Top 10 window', () => {
  for (const [date, left] of [['2026-09-28', 60], ['2026-07-31', 1], ['2026-07-30', 0]] as const) {
    assert.equal(daysLeft(date, 60, now), left)
    // Still ranked on its last day, gone the next.
    assert.equal(topTen([route('r')], [log('r', 1, date)], 'gym', 60, now).length, 1)
  }
  assert.equal(topTen([route('r')], [log('r', 1, '2026-07-29')], 'gym', 60, now).length, 0)
  assert.deepEqual([0, 1, 5].map(daysLeftLabel), ['Expires today', '1 day left', '5 days left'])
})

test('Top count needs a top and a numeric total', () => {
  const climbUser = { grade: null, tickType: 1, totalTries: 2, triedFirstAtDate: null, tickedFirstAtDate: null }
  assert.equal(topCount({ ...climbUser, totalTicks: 3 }), 3)
  assert.equal(topCount({ ...climbUser, totalTicks: null }), 0)
  assert.equal(topCount(climbUser), 0)
  assert.equal(topCount({ ...climbUser, tickType: 0, totalTicks: 2 }), 0)
})

test('Deleting a log removes only that listed log for this route and account', async () => {
  const deleted: unknown[] = []
  const updated = { climbId: 'route', grade: null, tickType: 1, totalTries: 2, totalTicks: 1, triedFirstAtDate: null, tickedFirstAtDate: null }
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    if (query === ROUTE_LOGS) {
      assert.equal(variables.gymId, 'gym'); assert.equal(variables.userId, 'user'); assert.equal(variables.climbId, 'route')
      return { climbLogs: { data: [routeLog('first', '2026-09-01'), routeLog('repeat', '2026-09-20', { tickIndex: 1 })], pagination: { total: 2, page: 1, perPage: 100 } } }
    }
    assert.equal(query, UNSEND); deleted.push(variables.ids)
    return { climbUsers: [updated] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  assert.deepEqual(await client.deleteLog('gym', 'user', 'route', 'repeat'), updated)
  await assert.rejects(client.deleteLog('gym', 'user', 'route', 'unknown'), /no longer on TopLogger/)
  assert.deepEqual(deleted, [['repeat']])
})

test('A repeat is a Redpoint log with no tries before it', async () => {
  const calls: Record<string, any>[] = []
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    calls.push(variables)
    return { climbUsers: [{ climbId: 'route', grade: null, tickType: 3, totalTries: 1, totalTicks: 2, triedFirstAtDate: null, tickedFirstAtDate: null }] }
  }, undefined, false)
  await client.connect('header.payload.signature')
  const topped = { ...route('route'), leadEnabled: false, leadRequired: false, climbUser: { grade: null, tickType: 3, totalTries: 1, totalTicks: 1, triedFirstAtDate: null, tickedFirstAtDate: null } }
  assert.equal((await client.logAscent('gym', 'user', topped, 1)).totalTicks, 2)
  assert.equal(calls[0]!.climbLogTriesBefore, 0); assert.equal(calls[0]!.climbLogData.topped, true); assert.equal(calls[0]!.climbLogData.foreknowledge, true)
  await assert.rejects(client.logAscent('gym', 'user', topped, 3), /Flash and Onsight/)
})

test('History refresh reuses complete older days, replaces recent logs, and discovers added/deleted days', async () => {
  const day = (daysAgo: number) => new Date(Date.now() - daysAgo * 86400000).toISOString()
  const old = day(30), recent = day(1), added = day(60), removed = day(40), empty = day(50)
  const dates = [old, recent, empty]
  const calls: string[] = []
  let revision = 1
  const client = new TopLoggerClient(async (query, variables) => {
    if (query === REFRESH) return { tokens: tokens() }
    if (query === DAYS) return { climbDaysPaginated: { data: dates.map(statsAtDate => ({ gymId: 'gym', statsAtDate })), pagination: { total: dates.length, page: 1, perPage: 100 } } }
    assert.equal(query, HISTORY)
    const date = variables.date as string
    calls.push(date)
    const logs = date === empty ? [] : [log(`revision-${revision}`, 1, date)]
    return { climbLogs: { data: logs, pagination: { total: logs.length, page: 1, perPage: 100 } } }
  }, undefined, false)
  await client.connect('header.payload.signature')
  const first = await client.history('gym', 'user')
  assert.equal(calls.length, 3)
  dates.push(added); revision = 2; calls.length = 0
  first.sessions.push({ date: removed, at: new Date().toISOString(), logs: [log('deleted', 1, removed)] })
  const second = await client.history('gym', 'user', first)
  assert.deepEqual(calls.sort(), [recent, added].sort())
  assert.equal(second.sessions.find(session => session.date === old)?.logs[0]?.climbId, 'revision-1')
  assert.equal(second.sessions.find(session => session.date === recent)?.logs[0]?.climbId, 'revision-2')
  assert.equal(second.sessions.find(session => session.date === recent)?.logs.length, 1)
  assert.deepEqual(second.sessions.find(session => session.date === empty)?.logs, [])
  assert(!second.sessions.some(session => session.date === removed))
  assert.equal(second.sessions.find(session => session.date === old)?.at, first.sessions.find(session => session.date === old)?.at)

  // Expiry is per session: repeated refreshes must not keep old logs fresh forever.
  second.sessions.find(session => session.date === old)!.at = day(8)
  calls.length = 0
  const third = await client.history('gym', 'user', second)
  assert.deepEqual(calls.sort(), [old, recent].sort())
  assert.equal(third.sessions.find(session => session.date === old)?.logs[0]?.climbId, 'revision-2')
})

test('History refresh replaces deleted ascents and leaves cached days intact when a request fails', async () => {
  const date = new Date(Date.now() - 86400000).toISOString()
  const cached = { sessions: [{ date, at: new Date().toISOString(), logs: [log('deleted', 1, date)] }] }
  const before = JSON.stringify(cached)
  let fail = false
  const client = new TopLoggerClient(async query => {
    if (query === REFRESH) return { tokens: tokens() }
    if (query === DAYS) return { climbDaysPaginated: { data: [{ gymId: 'gym', statsAtDate: date }], pagination: { total: 1, page: 1, perPage: 100 } } }
    if (fail) throw new ApiError('network', 'offline')
    return { climbLogs: { data: [], pagination: { total: 0, page: 1, perPage: 100 } } }
  }, undefined, false)
  await client.connect('header.payload.signature')
  assert.deepEqual((await client.history('gym', 'user', cached)).sessions[0]?.logs, [])
  fail = true
  await assert.rejects(client.history('gym', 'user', cached), (error: ApiError) => error.kind === 'network')
  assert.equal(JSON.stringify(cached), before)
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
