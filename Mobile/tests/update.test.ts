import test from 'node:test'
import assert from 'node:assert/strict'
import { checkForUpdate, isNewer, releaseUpdate } from '../app/utils/update'

const release = (tag: string, extra = {}) => ({ tag_name: tag, html_url: `https://github.com/CedricBaetens/TopLoggerPlus/releases/tag/${tag}`, draft: false, prerelease: false, assets: [{ name: `TopLoggerPlus-${tag.slice(1)}.apk`, browser_download_url: `https://github.com/CedricBaetens/TopLoggerPlus/releases/download/${tag}/TopLoggerPlus-${tag.slice(1)}.apk` }], ...extra })

test('Versions compare numerically and ignore QA or malformed versions', () => {
  assert(isNewer('v3.1.1', '3.1.0')); assert(isNewer('3.10.0', '3.9.9')); assert(isNewer('v4.0.0', '3.99.99'))
  assert(!isNewer('v3.1.0', '3.1.0')); assert(!isNewer('v3.0.9', '3.1.0'))
  assert(!isNewer('v3.2.0', '3.1.0-qa')); assert(!isNewer('latest', '3.1.0')); assert(!isNewer('v3.2', '3.1.0'))
})

test('Updates point to the APK, falling back to the release page', () => {
  assert.deepEqual(releaseUpdate(release('v3.2.0'), '3.1.0'), { version: '3.2.0', url: 'https://github.com/CedricBaetens/TopLoggerPlus/releases/download/v3.2.0/TopLoggerPlus-3.2.0.apk' })
  assert.equal(releaseUpdate(release('v3.2.0', { assets: [] }), '3.1.0')?.url, 'https://github.com/CedricBaetens/TopLoggerPlus/releases/tag/v3.2.0')
  assert.equal(releaseUpdate(release('v3.1.0'), '3.1.0'), null)
  assert.equal(releaseUpdate(release('v3.2.0', { prerelease: true }), '3.1.0'), null)
  assert.equal(releaseUpdate(release('v3.2.0', { assets: [], html_url: 'https://example.com/evil.apk' }), '3.1.0'), null)
})

const memory = () => { const items = new Map<string, string>(); return { getItem: (key: string) => items.get(key) ?? null, setItem: (key: string, value: string) => { items.set(key, value) } } }

test('Update checks reject when GitHub cannot be reached', async () => {
  await assert.rejects(checkForUpdate('3.1.0', { load: async () => { throw new TypeError('offline') }, storage: memory() }))
  await assert.rejects(checkForUpdate('3.1.0', { load: async () => new Response('rate limited', { status: 403 }), storage: memory() }), /403/)
  await assert.rejects(checkForUpdate('3.1.0', { load: async () => new Response('not json'), storage: memory() }))
  const broken = memory(); broken.setItem('tlp:update-check', '{bad')
  assert.equal((await checkForUpdate('3.1.0', { load: async () => Response.json(release('v3.1.1')), storage: broken }))?.version, '3.1.1')
})

test('Update checks reach GitHub at most every six hours unless forced', async () => {
  const storage = memory(), hour = 60 * 60 * 1000
  let calls = 0
  const load = async () => { calls++; return Response.json(release('v3.2.0')) }
  assert.equal((await checkForUpdate('3.1.0', { load, storage, now: 0 }))?.version, '3.2.0')
  assert.equal((await checkForUpdate('3.1.0', { load, storage, now: 5 * hour }))?.version, '3.2.0')
  assert.equal(await checkForUpdate('3.2.0', { load, storage, now: 5 * hour }), null, 'installed update hides the prompt without a new request')
  assert.equal(calls, 1)
  await checkForUpdate('3.1.0', { load, storage, now: 5 * hour, force: true })
  assert.equal(calls, 2)
  await checkForUpdate('3.1.0', { load, storage, now: 11 * hour })
  assert.equal(calls, 3)
})
