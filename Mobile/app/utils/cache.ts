const PREFIX = 'tlp:v1:'
export const cacheKey = (account: string, gym: string, kind: string) => `${PREFIX}${encodeURIComponent(account)}:${encodeURIComponent(gym)}:${kind}`
export interface CacheEntry<T> { version: 1; at: string; data: T }
export function readCache<T>(key: string): CacheEntry<T> | null {
  try {
    const raw = localStorage.getItem(key)
    if (!raw) return null
    const value = JSON.parse(raw)
    if (value.version !== 1 || typeof value.at !== 'string' || !Number.isFinite(Date.parse(value.at)) || !('data' in value)) return null
    return value
  } catch { return null }
}
export function writeCache<T>(key: string, data: T): string {
  const at = new Date().toISOString()
  // setItem replaces one complete snapshot; a failed write leaves the old snapshot intact.
  localStorage.setItem(key, JSON.stringify({ version: 1, at, data }))
  return at
}
export function clearCache(account?: string): void {
  const prefix = account ? `${PREFIX}${encodeURIComponent(account)}:` : PREFIX
  for (const key of Object.keys(localStorage)) if (key.startsWith(prefix)) localStorage.removeItem(key)
}
