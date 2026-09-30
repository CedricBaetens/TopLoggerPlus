export interface Reward { gymId: string; routeId: string; tickType: number; xp: number }
export interface Adventure { version: 1; rewards: Reward[] }
export const emptyAdventure = (): Adventure => ({ version: 1, rewards: [] })
export const adventureKey = (account: string) => `tlp:adventure:v1:${encodeURIComponent(account)}`
export const rewardXp = (type: number) => [0, 100, 125, 150][type] ?? 0

export interface Achievement { id: string; name: string; description: string; unlocked: boolean; current: number; target: number }

function milestones(id: string, current: number, unit: string, firstName?: string, start = 1): Achievement[] {
  const result: Achievement[] = []
  let target = start
  while (true) {
    result.push({ id: `${id}:${target}`, name: target === 1 && firstName ? firstName : `${target.toLocaleString('en-GB')} ${unit}`,
      description: `Reach ${target.toLocaleString('en-GB')} ${unit.toLowerCase()}.`, unlocked: current >= target, current, target })
    if (target > current) break
    target = target === 1 ? 5 : target === 5 ? 10 : target === 10 ? 25 : target === 25 ? 50 : target * 2
  }
  return result
}

export function newlyUnlocked(before: Achievement[], after: Achievement[]): Achievement[] {
  const earned = new Set(before.filter(item => item.unlocked).map(item => item.id))
  return after.filter(item => item.unlocked && !earned.has(item.id))
}

export function progress(data: Adventure) {
  const xp = data.rewards.reduce((sum, reward) => sum + reward.xp, 0)
  const sends = data.rewards.length
  const count = (type: number) => data.rewards.filter(reward => reward.tickType === type).length
  return { xp, sends, level: 1 + Math.floor(xp / 500), currentXp: xp % 500,
    achievements: [
      ...milestones('sends', sends, 'Sends', 'First Send'),
      ...milestones('redpoints', count(1), 'Redpoints', 'First Redpoint'),
      ...milestones('flashes', count(2), 'Flashes', 'First Flash'),
      ...milestones('onsights', count(3), 'Onsights', 'First Onsight'),
      ...milestones('xp', xp, 'XP', undefined, 500),
      ...milestones('gyms', new Set(data.rewards.map(reward => reward.gymId)).size, 'Gyms', 'First Gym'),
    ] }
}

export function updateReward(data: Adventure, gymId: string, routeId: string, tickType: number): Adventure {
  const exists = data.rewards.some(reward => reward.gymId === gymId && reward.routeId === routeId)
  if (tickType === -1) return { version: 1, rewards: data.rewards.filter(reward => reward.gymId !== gymId || reward.routeId !== routeId) }
  if (exists || !rewardXp(tickType)) return data
  return { version: 1, rewards: [...data.rewards, { gymId, routeId, tickType, xp: rewardXp(tickType) }] }
}

export function loadAdventure(account: string): Adventure {
  try {
    const data = JSON.parse(localStorage.getItem(adventureKey(account)) || 'null')
    if (data?.version !== 1 || !Array.isArray(data.rewards)) return emptyAdventure()
    const keys = new Set<string>()
    for (const reward of data.rewards) {
      if (!reward || typeof reward.gymId !== 'string' || !reward.gymId || typeof reward.routeId !== 'string' || !reward.routeId
        || !Number.isInteger(reward.tickType) || !rewardXp(reward.tickType) || reward.xp !== rewardXp(reward.tickType)) return emptyAdventure()
      const key = JSON.stringify([reward.gymId, reward.routeId])
      if (keys.has(key)) return emptyAdventure()
      keys.add(key)
    }
    return data
  } catch { return emptyAdventure() }
}

export function saveAdventure(account: string, data: Adventure): boolean {
  try { localStorage.setItem(adventureKey(account), JSON.stringify(data)); return true }
  catch { return false }
}
