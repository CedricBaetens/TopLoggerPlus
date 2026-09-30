import test from 'node:test'
import assert from 'node:assert/strict'
import { emptyAdventure, updateReward, progress, rewardXp, loadAdventure, saveAdventure, adventureKey, newlyUnlocked } from '../app/utils/adventure'
import { clearCache } from '../app/utils/cache'

test('Rewards, duplicate prevention, levels, milestones, undo and resend', () => {
  assert.deepEqual([-1, 0, 1, 2, 3, 4].map(rewardXp), [0, 0, 100, 125, 150, 0])
  let data = emptyAdventure()
  assert.equal(progress(data).level, 1)
  assert.equal(progress(data).achievements.some(item => item.unlocked), false)
  for (let i = 0; i < 50; i++) {
    data = updateReward(data, 'gym', String(i), 1)
    assert.equal(updateReward(data, 'gym', String(i), 2), data)
    if (i === 3) assert.equal(progress(data).level, 1)
    if (i === 4) { assert.equal(progress(data).level, 2); assert.equal(progress(data).currentXp, 0) }
  }
  assert.equal(progress(data).achievements.some(item => item.id === 'sends:50' && item.unlocked), true)
  data = updateReward(data, 'gym', '49', -1)
  assert.equal(progress(data).achievements.some(item => item.id === 'sends:50' && item.unlocked), false)
  data = updateReward(data, 'gym', '49', 2)
  assert.equal(progress(data).xp, 5025)
  assert.equal(progress(data).achievements.some(item => item.id === 'flashes:1' && item.unlocked), true)
  data = updateReward(data, 'gym', '49', -1)
  assert.equal(progress(data).achievements.some(item => item.id === 'flashes:1' && item.unlocked), false)
  data = updateReward(data, 'other', '0', 3)
  assert.equal(progress(data).xp, 5050)
  assert.equal(progress(data).achievements.some(item => item.id === 'onsights:1' && item.unlocked), true)
  assert.equal(updateReward(data, 'gym', 'try', 0), data)
})

test('Persistence, isolation, cache retention, corrupt data and storage failures', () => {
  const values: Record<string, string> = {}
  const storage = { getItem: (key: string) => values[key] ?? null, setItem: (key: string, value: string) => { values[key] = value }, removeItem: (key: string) => { delete values[key] } }
  globalThis.localStorage = new Proxy(storage, { ownKeys: () => Object.keys(values), getOwnPropertyDescriptor: () => ({ enumerable: true, configurable: true }) }) as unknown as Storage
  const data = updateReward(emptyAdventure(), 'gym', 'route', 3)
  assert.equal(saveAdventure('a', data), true)
  assert.deepEqual(loadAdventure('a'), data)
  assert.deepEqual(loadAdventure('b'), emptyAdventure())
  clearCache('a'); clearCache()
  assert.deepEqual(loadAdventure('a'), data)
  for (const invalid of ['broken', '{"version":2,"rewards":[]}', JSON.stringify({ version: 1, rewards: [{ gymId: 'g', routeId: 'r', tickType: 1, xp: 999 }] }), JSON.stringify({ version: 1, rewards: [data.rewards[0], data.rewards[0]] })]) {
    values[adventureKey('b')] = invalid
    assert.deepEqual(loadAdventure('b'), emptyAdventure())
  }
  storage.setItem = () => { throw new Error('full') }
  assert.equal(saveAdventure('a', emptyAdventure()), false)
  assert.deepEqual(loadAdventure('a'), data)
  storage.getItem = () => { throw new Error('blocked') }
  assert.deepEqual(loadAdventure('a'), emptyAdventure())
})

test('Milestones keep growing, retain earned badges and compare stable identities', () => {
  const data = { version: 1 as const, rewards: Array.from({ length: 13000 }, (_, i) => ({ gymId: 'gym', routeId: String(i), tickType: 2, xp: 125 })) }
  const result = progress(data)
  for (const category of ['sends', 'flashes', 'xp']) {
    const chain = result.achievements.filter(item => item.id.startsWith(`${category}:`))
    assert(chain.length > 10)
    assert.equal(chain.filter(item => !item.unlocked).length, 1)
    assert(chain.at(-1)!.target > chain.at(-1)!.current)
    assert(chain.slice(0, -1).every(item => item.unlocked))
  }
  const before = progress(emptyAdventure())
  const first = progress(updateReward(emptyAdventure(), 'gym', 'route', 2))
  assert.deepEqual(newlyUnlocked(before.achievements, first.achievements).map(item => item.name), ['First Send', 'First Flash', 'First Gym'])
  const four = { version: 1 as const, rewards: data.rewards.slice(0, 4) }
  const five = { version: 1 as const, rewards: data.rewards.slice(0, 5) }
  assert.deepEqual(newlyUnlocked(progress(four).achievements, progress(five).achievements).map(item => item.id), ['sends:5', 'flashes:5'])
  const undone = updateReward(five, 'gym', '4', -1)
  assert.deepEqual(newlyUnlocked(progress(five).achievements, progress(undone).achievements), [])
  assert.deepEqual(newlyUnlocked(progress(undone).achievements, progress(five).achievements).map(item => item.id), ['sends:5', 'flashes:5'])
})
