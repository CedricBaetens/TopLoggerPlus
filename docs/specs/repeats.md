# Repeats and ascent log

Status: implemented 2026-10-05; the live-account checks below are still open. Investigated 2026-09-30.

## Goal

1. Once a route is topped, let the climber log it again ("repeat") from the route details.
2. Show the climber's own logs for a route (tries, the first top, repeats) as an explicit, dated list, where each log can be deleted individually. This mirrors the official TopLogger app.

## Findings

These come from the official web app bundle at `app.toplogger.nu` (`tl-climb-logging-form`, `tl-climb-log-edit`, `tl-climb-log`, `tl-climb-log-remove`) and from probing the GraphQL schema. Introspection is disabled; unknown fields produce validation errors, while known ones fail only on authentication.

### Logging a repeat

- The official Repeat button only appears once the route is topped. It emits `buttonLog({ tickType: 1 })`, the same event as the Redpoint button.
- That maps to `climbUserLog` with `climbLogData: { topped: true, foreknowledge: true, zones: 0, lead, climbedAtDate: today }`.
- `climbLogTriesBefore` is forced to `0` whenever the user already has tries on the route.
- There is no separate repeat mutation: **a repeat is another topped log on a route that already has one.**

Our `TopLoggerClient.logAscent(gymId, userId, route, 1)` (`Mobile/app/utils/toplogger.ts`) already sends exactly this for a topped route, and its validation allows it. **No new mutation is needed.** Only the UI blocks it: `Mobile/app/app.vue` disables all send buttons once `climbUser.tickType` is set.

### Listing logs

`climbLogs(gymId, userId, climbId, climbType: route)` returns one entry per log. `ROUTE_LOGS` already uses it (paginated) for Unsend. The official log item (`fragment climbLog`) reads these fields, all of which our schema probe accepts:

| Field | Use |
|---|---|
| `id` | Delete target |
| `climbedAtDate` | Date shown; newest first |
| `topped`, `ticked`, `valid` | Try vs. top; hide invalid logs |
| `tickType` | Onsight / Flash / Redpoint label for the first top |
| `tickIndex` | Which top this was. Expected: `0` = first top, `> 0` = repeat (to verify) |
| `tryIndex` | Ordering within a day |
| `lead` | Lead / Toprope label |
| `autoAdded` | Generated attempt (see below) |

`ClimbUser.totalTicks` also exists. The official app uses it as "number of tops" (it shows "N × points" in competitions), so the route list can show repeats as `totalTicks - 1` without loading every route's logs.

### Deleting a single log

- The official app calls `climbUserDeleteLogs(gymId, userId, ids: [logId])`, the mutation our `UNSEND` already uses, with a single id. It returns the recalculated `ClimbUser`.
- It asks for confirmation when the route has been removed from the wall (`outAt` in the past): "This will remove your ascent from {timeAgo}. This cannot be undone." You can't log that route again, so a deleted ascent can't be recreated.

## Proposed behaviour

### Route details

- **Send buttons:** before the first top, unchanged (Onsight / Flash / Redpoint). Once topped, they show the achieved style as pressed, and a **Repeat** button appears next to **Try**. Repeat is disabled under the same conditions as Try: removed route, busy, offline, or needs login.
- **Unsend:** unchanged. It removes every send and generated attempt at once, like the official Redpoint uncheck. When there is more than one top, it first asks "Remove N ascents?".
- **"Your log" section:** a list below the buttons, loaded with the route details, newest first. Each row shows:
  - the date: "Today", "Yesterday", or e.g. "12 Sep"
  - the kind:
    - `Try` when not topped
    - `Onsight` / `Flash` / `Redpoint` for the first top (`tickIndex === 0`)
    - `Repeat` when `tickIndex > 0`
  - `Lead` or `Toprope`, where the route allows both
  - a **Delete** button
- **Filtering:** invalid logs are hidden. Generated attempts (`autoAdded`) are hidden too; they're internal bookkeeping that Unsend already cleans up.
- **Deleting:**
  - A confirmation is required when the route has been removed from the wall, and when deleting the first top while repeats exist ("Your next ascent will become your first top").
  - Afterwards the route, the list and the history cache are refreshed through the same path as Unsend: invalidate this route's history sessions and update `state.routes` with the returned `ClimbUser`.
- **Empty list:** "No logs yet."

### Route list

- A small "×N" marker on topped routes with repeats (`totalTicks > 1`).

### Data changes

- `ROUTE_FIELDS` and the `climbUser` selections of `LOG_ASCENT`, `UNSEND` and `GRADE_VOTE` get `totalTicks`. The `ClimbUser` type in `domain.ts` gets it too. A missing or non-number value hides the count.
- A new `RouteLog` type and a `routeLogs()` client method use the fields in the table above. Reuse `ROUTE_LOGS` extended with those fields, and keep Unsend on the same query.
- A new `deleteLog(gymId, userId, routeId, logId)` client method uses `climbUserDeleteLogs` with one id. It verifies the log belongs to this route and user before sending, like `unsend()` does.
- Add the new fields to `scripts/verify-api.ts` so schema changes are caught.

### Top 10

Decided: no change. `HISTORY` already returns every log, and a repeat is a valid, ticked, topped log, so `topTen()` already treats it as a candidate ascent. The existing rules (`domain.ts`, covered by `tests/core.test.ts`) are the intended ones:

- A route appears at most once.
- The highest score wins, so an Onsight (grade + 15) beats a more recent Redpoint repeat of the same route.
- Equal scores go to the most recent ascent, because it stays in the window longest.
- Only ascents inside the selected window are candidates. If the Onsight falls out of the window, a repeat inside it keeps the route in the ranking, scored with the repeat's own `tickType` (presumably Redpoint; to verify).

Deleting a log invalidates history like Unsend does, so the Top 10 recalculates.

### Adventure

Out of scope: owned by Cedric. This feature adds no XP rules. The only touch point is the existing Unsend hook (`updateReward(..., -1)`): if deleting logs leaves the route with no top at all, call it exactly as Unsend does, so Adventure behaves the same as today. Repeats and deleting a repeat don't touch Adventure.

## To verify with a live account

Unauthenticated probing only proves the fields exist. These need a real account (a QA build and one test route):

- [ ] `tickIndex` is `0` for the first top and increments per repeat; tries have no tick index (`null`?).
- [ ] `totalTicks` is 1 after the first top and increments per repeat.
- [ ] After a repeat, `climbUser.tickType` keeps the best style (e.g. stays Onsight) and `tickedFirstAtDate` doesn't change.
- [ ] Whether a repeat adds to `totalTries`.
- [ ] The `tickType` of a repeat log in `HISTORY`: expected 1 (Redpoint).
- [ ] Deleting a single try or repeat recalculates `climbUser` and leaves the other logs intact.
- [ ] Deleting the first top while repeats exist: does the next repeat become the first top (and with which `tickType`)?
- [ ] Logs created in Plus show up correctly in the official app, and the other way around.

## Out of scope

Boulders (the app is routes-only), competitions/zones, editing a log's date or style, session summaries, Adventure XP.
