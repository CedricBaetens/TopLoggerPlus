export const LATEST_RELEASE = 'https://api.github.com/repos/CedricBaetens/TopLoggerPlus/releases/latest'
export type Update = { version: string, url: string }
type Release = { tag_name?: unknown, html_url?: unknown, draft?: unknown, prerelease?: unknown, assets?: { name?: unknown, browser_download_url?: unknown }[] }

function parts(version: string): number[] | null {
  const match = /^v?(\d+)\.(\d+)\.(\d+)$/.exec(version.trim())
  return match ? match.slice(1).map(Number) : null
}

/** True only when both are plain x.y.z versions and latest is higher; QA builds (3.1.0-qa) never match. */
export function isNewer(latest: string, current: string): boolean {
  const a = parts(latest), b = parts(current)
  if (!a || !b) return false
  const index = a.findIndex((value, i) => value !== b[i])
  return index >= 0 && a[index]! > b[index]!
}

export function releaseUpdate(release: Release, current: string): Update | null {
  if (release.draft || release.prerelease || typeof release.tag_name !== 'string' || !isNewer(release.tag_name, current)) return null
  const apk = release.assets?.find(asset => typeof asset.name === 'string' && asset.name.endsWith('.apk') && typeof asset.browser_download_url === 'string')
  const url = apk?.browser_download_url ?? release.html_url
  return typeof url === 'string' && url.startsWith('https://github.com/') ? { version: release.tag_name.replace(/^v/, ''), url } : null
}

const CHECKED = 'tlp:update-check', INTERVAL = 6 * 60 * 60 * 1000
type Storage = Pick<globalThis.Storage, 'getItem' | 'setItem'>

type CheckOptions = { force?: boolean, load?: typeof fetch, storage?: Storage, now?: number }

/**
 * Checks GitHub for a newer release at most every six hours unless forced, reusing the last answer in between
 * (GitHub allows 60 unauthenticated requests per hour per IP). Rejects when GitHub cannot be reached.
 */
export async function checkForUpdate(current: string, { force = false, load = fetch, storage = localStorage, now = Date.now() }: CheckOptions = {}): Promise<Update | null> {
  if (!force) {
    try {
      const saved = JSON.parse(storage.getItem(CHECKED) || 'null') as { at?: unknown, release?: Release } | null
      if (typeof saved?.at === 'number' && saved.release && now - saved.at >= 0 && now - saved.at < INTERVAL) return releaseUpdate(saved.release, current)
    } catch { /* unreadable cache: check again */ }
  }
  const response = await load(LATEST_RELEASE, { headers: { Accept: 'application/vnd.github+json' }, signal: AbortSignal.timeout(10000) })
  if (!response.ok) throw new Error(`GitHub answered ${response.status}`)
  const { tag_name, html_url, draft, prerelease, assets } = await response.json() as Release
  const release = { tag_name, html_url, draft, prerelease, assets: assets?.map(({ name, browser_download_url }) => ({ name, browser_download_url })) }
  storage.setItem(CHECKED, JSON.stringify({ at: now, release }))
  return releaseUpdate(release, current)
}
