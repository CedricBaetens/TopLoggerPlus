import assert from 'node:assert/strict'
import { REFRESH, USER, ROUTES, ROUTE, ROUTE_LOGS, DAYS, HISTORY, COMMUNITY, TOPPERS } from '../app/utils/queries'

// Unauthenticated probes validate operation shapes, not account-level permissions/data.
for (const [name, query] of Object.entries({ USER, ROUTES, ROUTE, ROUTE_LOGS, DAYS, HISTORY, COMMUNITY, TOPPERS, REFRESH })) {
  const response = await fetch('https://app.toplogger.nu/graphql', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query, variables: { gymId: 'plus-schema-probe', userId: 'plus-schema-probe', id: 'plus-schema-probe', climbId: 'plus-schema-probe', refreshToken: 'invalid', from: '2026-04-01T00:00:00Z', until: '2026-09-28T23:59:59Z', date: '2026-09-20T00:00:00Z', pagination: { page: 1, perPage: 200 } } }),
    signal: AbortSignal.timeout(30000),
  })
  const body = await response.json() as { data?: unknown; errors?: { extensions?: { code?: string } }[] }
  assert(!body.errors?.some(error => error.extensions?.code === 'GRAPHQL_VALIDATION_FAILED'), `${name}: schema has changed`)
  assert(response.status < 500 && (body.data || body.errors?.length), `${name}: server did not respond normally`)
  console.log(`${name}: schema accepted${body.errors?.length ? ' (authentication/data still requires a connected account)' : ''}`)
}
