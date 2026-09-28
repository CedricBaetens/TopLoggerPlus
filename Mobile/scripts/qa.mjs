import assert from 'node:assert/strict'
import { createServer } from 'node:http'
import { readFile, mkdir } from 'node:fs/promises'
import { resolve, extname, sep } from 'node:path'
import { chromium } from 'playwright'

// Test fixtures only; no sample data is bundled in the application.
const root = resolve('.output/public'), output = resolve('artifacts/qa')
await mkdir(output, { recursive: true })
const server = createServer(async (request, response) => {
  try {
    const name = decodeURIComponent(new URL(request.url, 'http://localhost').pathname)
    let path = resolve(root, `.${name === '/' ? '/index.html' : name}`)
    if (!path.startsWith(root + sep)) { response.writeHead(403).end(); return }
    let body
    try { body = await readFile(path) } catch { path = resolve(root, 'index.html'); body = await readFile(path) }
    response.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' })[extname(path)] || 'application/octet-stream' }).end(body)
  } catch { response.writeHead(500).end() }
})
await new Promise(done => server.listen(0, '127.0.0.1', done))
const browser = await chromium.launch({ headless: true, ...(process.env.TLP_BROWSER_PATH ? { executablePath: process.env.TLP_BROWSER_PATH } : {}) })
try {
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, colorScheme: 'light' })
  const page = await context.newPage(), errors = []
  page.on('pageerror', error => errors.push(error.message))
  let offline = false, expired = false, refreshes = 0
  const today = new Date().toISOString().slice(0, 10), future = new Date(Date.now() + 5 * 86400000).toISOString().slice(0, 10)
  const route = (id, grade, color, label, tickType, outPlannedAt = null) => ({ id, grade, name: null, label, wall: { nameLoc: 'Main wall' }, holdColor: { color, nameLoc: ({ '#c46935': 'Orange', '#667aac': 'Blue', '#646b64': 'Black' })[color] }, setterName: 'Alex', climbSetters: [], inAt: '2026-09-01', outAt: null, outPlannedAt, climbUser: tickType === null ? null : { grade, tickType, totalTries: tickType ? 2 : 3, triedFirstAtDate: today, tickedFirstAtDate: tickType ? today : null } })
  const routes = [route('a', 617, '#c46935', '12', 2, future), route('b', 650, '#667aac', '18', 0, future), route('c', 700, '#646b64', '24', null)]
  routes[1].wall.nameLoc = 'Side wall'; routes[2].wall.nameLoc = 'Other wall'
  const removed = { ...route('archived', 683, '#667aac', '9', 3), outAt: today }
  const user = { id: 'test-account', fullName: 'Test Climber', gym: { id: 'gym', name: 'Test Climbing Gym' }, gymUserFavorites: [{ gym: { id: 'second', name: 'Second Gym' } }] }
  await page.route('https://app.toplogger.nu/graphql', async request => {
    if (offline) { await request.abort(); return }
    const { query, variables } = request.request().postDataJSON()
    let data
    if (query.includes('mutation')) { refreshes++; data = { tokens: { access: { token: 'test-access', expiresAt: '2099-01-01T00:00:00Z' }, refresh: { token: 'test.refresh.signature', expiresAt: '2099-01-01T00:00:00Z' } } } }
    else if (expired) { await request.fulfill({ json: { errors: [{ extensions: { code: 'UNAUTHENTICATED' } }] } }); return }
    else if (query.includes('PlusUser')) data = { userMe: user }
    else if (query.includes('PlusRoutes')) { const list = variables.gymId === 'gym' ? routes : [route('second-route', 583, '#c46935', '3', 1)]; data = { climbs: { data: list, pagination: { total: list.length, page: 1, perPage: 200 } } } }
    else if (query.includes('PlusDays')) data = { climbDaysPaginated: { data: [{ id: 'day', gymId: 'gym', statsAtDate: today }], pagination: { total: 1, page: 1, perPage: 200 } } }
    else if (query.includes('PlusHistory')) { const logs = [routes[0], removed].map(item => ({ id: `${item.id}-log`, climbId: item.id, gymId: 'gym', climbType: 'route', tickType: item.climbUser.tickType, climbedAtDate: today, valid: true, topped: true, ticked: true })); data = { climbLogs: { data: logs, pagination: { total: logs.length, page: 1, perPage: 200 } } } }
    else if (query.includes('PlusRoute(')) data = { climb: removed }
    else if (query.includes('PlusCommunity')) data = { climb: { gradeVoteStats: [{ grade: 617, count: 12 }, { grade: 633, count: 3 }], ratingVoteStats: [{ stars: 4, count: 8 }, { stars: 5, count: 4 }] } }
    else if (query.includes('PlusToppers')) data = { climbUsers: { data: [{ id: 'topper', user: { id: 'other', fullName: 'Test Climber Two' }, tickType: 1, grade: 617 }], pagination: { total: 1, page: 1, perPage: 200 } } }
    else throw new Error('Unhandled test operation')
    await request.fulfill({ json: { data } })
  })
  const screenshot = name => page.screenshot({ path: resolve(output, name), fullPage: true })
  await page.goto(`http://127.0.0.1:${server.address().port}`)
  await page.getByRole('heading', { name: 'Connect TopLogger' }).waitFor()
  await screenshot('01-connect.png')
  await page.getByLabel('Refresh token', { exact: true }).fill('test.refresh.signature')
  await page.getByRole('button', { name: 'Connect my account' }).click()
  await page.locator('.route-card').first().waitFor(); assert.equal(await page.locator('.route-card').count(), 3)
  assert.equal(await page.locator('.route-done').count(), 1)
  assert.equal(await page.getByRole('button', { name: /Rope 12.*Flash/ }).count(), 1)
  assert(await page.locator('.route-status span').evaluateAll(spans => spans.filter(span => span.textContent?.trim()).every(span => span.getBoundingClientRect().width <= 1)), 'Status words must be visually hidden')
  await page.getByRole('button', { name: /^To do / }).click(); assert.equal(await page.locator('.route-card').count(), 2)
  await page.getByRole('button', { name: /^Done / }).click(); assert.equal(await page.locator('.route-card').count(), 1)
  await page.getByRole('button', { name: /^All / }).click()
  await screenshot('02-routes.png')
  await page.getByLabel('Search routes').fill('24'); assert.equal(await page.locator('.route-card').count(), 1)
  await page.getByLabel('Search routes').fill('')
  await page.getByRole('button', { name: 'Filters', exact: true }).click()
  await page.getByRole('checkbox', { name: 'Main wall', exact: true }).check()
  await page.getByRole('checkbox', { name: 'Side wall', exact: true }).check()
  assert.equal(await page.locator('.route-card').count(), 2)
  await page.getByRole('button', { name: 'All walls', exact: true }).click()
  assert.equal(await page.locator('.route-card').count(), 3)
  await page.getByLabel('Ascent status', { exact: true }).selectOption('topped'); assert.equal(await page.locator('.route-card').count(), 1)
  await page.getByRole('button', { name: 'Reset filters' }).click()
  await page.getByRole('button', { name: 'Filters', exact: true }).click()
  assert.equal(await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('button').count(), 3)
  assert.equal(await page.getByRole('button', { name: 'Leaving Soon', exact: true }).count(), 0)
  await page.getByRole('button', { name: 'Top 10', exact: true }).click()
  await page.locator('.route-card').first().waitFor(); assert.equal(await page.locator('.route-card').count(), 2)
  assert((await page.locator('.route-card').first().innerText()).includes('Rope 9'))
  assert.equal(await page.getByRole('button', { name: /Rope 9.*Onsight/ }).count(), 1)
  await page.getByRole('button', { name: '4 months' }).click(); await page.getByRole('button', { name: '6 months' }).click()
  await screenshot('04-top-ten.png')
  await page.locator('.route-card').last().click(); await page.getByText('Test Climber Two').waitFor()
  await screenshot('05-details.png'); await page.getByRole('button', { name: 'Back to Top 10' }).click()
  await page.getByRole('button', { name: 'Account', exact: true }).click(); await page.getByRole('button', { name: 'Dark', exact: true }).click()
  await page.getByRole('button', { name: 'Routes', exact: true }).click(); await screenshot('06-dark.png')
  for (const width of [320, 390, 768]) {
    await page.setViewportSize({ width, height: 844 }); await page.evaluate(() => { document.documentElement.style.fontSize = '24px' })
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `Horizontal overflow at ${width}px with 150% text`)
  }
  await page.evaluate(() => { document.documentElement.style.fontSize = '' }); await page.setViewportSize({ width: 390, height: 844 })
  await page.getByLabel('Selected gym').selectOption('second'); await page.getByRole('button', { name: /Rope 3/ }).waitFor(); assert.equal(await page.locator('.route-card').count(), 1)
  await page.getByLabel('Selected gym').selectOption('gym'); await page.getByRole('button', { name: /Rope 12/ }).waitFor()
  offline = true; await page.getByRole('button', { name: 'Refresh TopLogger data' }).click()
  await page.getByText('Could not reach TopLogger.', { exact: false }).waitFor(); assert.equal(await page.locator('.route-card').count(), 3)
  offline = false; expired = true; await page.getByRole('button', { name: 'Refresh TopLogger data' }).click()
  await page.getByText('Reconnect to refresh your data.', { exact: false }).waitFor(); assert.equal(await page.locator('.route-card').count(), 3); assert.equal(refreshes, 2)
  expired = false; await page.getByRole('button', { name: 'Account', exact: true }).click()
  await page.getByRole('button', { name: 'Sign out and remove account data' }).click()
  await page.getByRole('heading', { name: 'Connect TopLogger' }).waitFor()
  assert.equal(await page.evaluate(() => Object.keys(localStorage).filter(key => key.startsWith('tlp:v1:')).length), 0)
  assert.deepEqual(errors, [])
  console.log('UI checks passed: login, all destinations, filters, history, community, gym switching, dark mode, 150% text, offline, expired session, logout. Screenshots contain test fixtures only.')
} finally { await browser.close(); await new Promise(done => server.close(done)) }
