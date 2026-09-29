# TopLogger Plus — Android

Nuxt 4 / Vue 3 / TypeScript / Tailwind CSS UI, bundled locally with Capacitor 8. No hosted backend or bundled sample account data. Ascents are saved directly to TopLogger when requested.

## Build and install

Requirements: Node 22+, JDK 21, Android SDK platform 36, Build Tools 35.0.0/36.0.0 and platform-tools. Set `JAVA_HOME` and `ANDROID_HOME`.

```powershell
cd Mobile
./scripts/build-android.ps1
adb install -r android/app/build/outputs/apk/release/app-release.apk
adb shell am start -n com.toploggerplus.app/.MainActivity
```

Personal release signing files are included in `Mobile/signing/` and used automatically. Keep this repository private: the key and password allow signing app updates. Signing JSON contains `keystore` (absolute or relative to the JSON file), `password`, and `alias`. Override with `-SigningFile`, or set `TLP_KEYSTORE`, `TLP_STORE_PASSWORD`, `TLP_KEY_ALIAS`, and optional `TLP_KEY_PASSWORD`. The build script rejects unsigned release builds.

Output: `android/app/build/outputs/apk/release/app-release.apk`. Android 7+ (API 24), application ID `com.toploggerplus.app`. Debug builds use `com.toploggerplus.app.qa` so native tests cannot touch release credentials.

On this workstation, tools are under `C:/Users/Cedric/.codex/android-tools`; the personal signing key and JSON are included in `Mobile/signing/`, with an additional copy under `C:/Users/Cedric/.codex/toplogger-plus-signing`. No existing keystore was available in the checkout. **Back up the signing directory privately**: future updates need the same key.

## Connect TopLogger

Tap **Sign in with TopLogger** on Android. The app opens TopLogger's official HTTPS page; enter your credentials and complete its verification there. Plus reads the resulting refresh token, removes the web session and saves the token in its encrypted Android vault. Live sign-in and session restoration have been verified on Android.

Embedded sign-in allows the official origin only; use email sign-in if a social provider requires an external page. Manual token entry is not available.

Open a route and tap **Redpoint**, **Flash**, or **Onsight** directly below its heading to log today's ascent. Flash and Onsight require an unattempted route. Logging uses Lead when the route requires it, otherwise Top rope. Sent routes offer **Unsend**, which removes all valid sends and automatically generated attempts for that route while retaining genuine attempts. Personal statistics update from TopLogger, and only affected history sessions are invalidated. Connection failures never automatically retry a write; check TopLogger before trying again because the change may have succeeded.

The **Your grade** row offers five one-tap votes: two easier grades, the route's official grade, and two harder grades. Your saved vote is highlighted. At the ends of the French scale, unavailable grades are disabled. A vote updates your personal grade and refreshes community grade counts without reloading history or toppers.

Tap **Try** to add one untopped attempt for today. The attempt count beside the button updates from TopLogger's response and includes all recorded attempts, including sends. Logging a try preserves existing sends and grade votes; after the first attempt, Flash and Onsight are unavailable. Unsend keeps genuine tries.

Tokens are AES-GCM encrypted with a non-exportable AndroidKeyStore key. Android backup is disabled. Non-secret route/history/community snapshots live in WebView local storage, versioned and scoped by user/gym. Browser development keeps tokens in memory only. Logout deletes secure credentials and cached account data; clearing saved data retains the connection.

## Checks

```powershell
npm ci
npm test
npm run typecheck
npm run verify:api
npm run generate
$env:TLP_BROWSER_PATH = 'C:/Program Files/Google/Chrome/Application/chrome.exe'
npm run qa
# Alternatively install Chromium: npx playwright install chromium
cd android
./gradlew.bat :app:connectedDebugAndroidTest
```

The API check validates live GraphQL shapes without credentials; it does **not** verify account permissions or returned history. Browser QA uses explicit fixtures and writes screenshots to ignored `artifacts/qa/`. The native test checks encryption, random IVs, tamper rejection and clearing in the separate QA package.

## Feature parity and acceptance

| Feature | Implementation | Verification |
| --- | --- | --- |
| Login / refresh / logout | Official embedded sign-in, serialized refresh, one auth retry, secure storage | Live sign-in and route loading confirmed on S26 Ultra; Android vault/origin/capture tests pass; live logout pending |
| Account and favorite gyms | Local selection and scoped caches; TopLogger preference unchanged | Browser switch/isolation checks |
| Routes and details | Grade, personal grade, color/name, wall, rope/label, setters, attempts, dates, status, score | Browser checks; schema accepted |
| Refresh / offline | Button and pull gesture, last successful sync, cache preserved on failure | Browser failure checks; gesture/device pending |
| Top 10 | 60/120/180 days; gym/session-scoped history; best valid ticked top per route; bonuses; latest-date ties; archived recovery | Unit + browser checks; Live S26: 60/120/180-day periods each load 10 routes; confirmed after restart in 3.0.2. Fixed API page limit (100). Detailed historical parity comparison pending |
| Community | Grade/rating distributions, toppers, explicit unavailable states | Browser checks; schema accepted; account permissions pending |
| Accessibility / themes | Light/dark/system, semantic controls, safe areas, 48px controls, large text | Browser at 320/390/768px and 150% text; TalkBack/device pending |
| Packaging | Signed release, custom icon, no cleartext traffic or backup | Release build; signature verification recorded separately |

**Remaining parity checks:** history access now works with the live account. Complete historical ranking comparison, metadata for removed climbs, and the complete toppers list still need verification. An inaccessible archived route fails history loading rather than silently returning a partial Top 10. Saved rankings remain available with an error message when a later sync fails.

First-top dates are displayed in details, never substituted for history. Ranking uses current route grades, matching the old rule. Schema changes produce API errors, not fabricated data. Cache updates replace complete snapshots atomically. Web deployment, desktop packaging and iOS builds are deferred.

History refresh always checks the 180-day session list, but reuses cached logs for older sessions for up to seven days. Sessions within the last seven days and newly discovered sessions are fetched on every refresh. Deleted sessions are dropped; refreshed sessions replace their previous logs, including when all ascents were deleted. Older edits can take up to a week to appear. **Account → Clear saved data**, followed by refreshing Top 10, forces a full reload. Existing installations perform one full history reload to populate the per-session cache.

### Physical-device checklist

- [x] Install/launch the signed APK on the Samsung S26 Ultra; open official sign-in on the Android 16 emulator.
- [x] Complete official sign-in with a live account; Routes displayed 248 active routes and 94 personal tops at Klimax.
- [ ] Compare profile, gyms, routes and details with TopLogger.
- [ ] Compare Top 10 windows, repeats/bonuses, and removed routes with actual logs.
- [ ] Compare community votes/toppers; verify restricted data is clearly indicated.
- [x] Force-stop/reopen to verify encrypted session restoration; routes and cached Top 10 return without login.
- [ ] Test offline relaunch, failed refresh/retry and cached gym changes.
- [ ] Verify Android back, pull refresh, themes, large text, TalkBack and system insets.
- [ ] Clear saved data, refresh, sign out; verify the connection is absent after restart.

Tooling note: npm reports moderate advisories in Capacitor CLI's development-only Xcode/uuid chain. They are not bundled in the Android UI or APK; reassess before adding iOS tooling.

### 3.0.2 live history fix

TopLogger rejects history/session pagination above 100 records. Shared pagination now requests 100 and continues through all pages. Live Klimax results: 60 days = 6b · 82%, 120 days = 6c · 13%, 180 days = 6c · 75%; each has 10 qualifying routes. Live grade votes and ratings were also visible in route details. These checks confirm data loads; they do not substitute for a full comparison against the original logs.

### 3.0.3 compact UI

Routes use compact rows with explicit Done checks and To do/Attempted indicators. All/To do/Done tabs replace the large summary panel. Removed slogans and reduced spacing across screens. Browser checks cover completion tabs, filters, route details, 150% text, themes and error states. Signed APK installed on the S26 Ultra; physical visual review awaits unlocking.

### 3.0.5 wall selection

Select multiple wall checkboxes under Filters. Klimax initially selects Sector 1 through Sector 7, matching case-insensitively and preserving API names such as sector 2; other gyms initially show all walls. All walls and Reset filters remove the wall restriction. Explicit choices survive refresh; changing gyms restores that gym’s default. Unit and browser checks cover the default rule and multiple-wall filtering. Signed update installed on the phone; live visual review awaits unlocking.

### 3.0.6 visual refinement

Flat route rows, restrained typography, neutral surfaces, underlined tabs and simpler detail/account sections replace decorative panels. Completion markers and multiple-wall selection remain available. Type checking and browser checks passed, including dark mode, 150% text, filters and error states.

### 3.0.7 reference styling

Applied the supplied game-companion visual style to existing screens: near-black blue panels, purple controls/navigation, mint completion markers and gold Top 10 highlights. Light/dark/system themes remain available. No gamification features were added. Browser checks passed for navigation, filters, history, offline errors and 150% text.

### 3.0.8 completion visibility

Completed routes use a green-tinted row, a strong left edge and a filled check marker. Unfinished routes keep neutral panels and empty circles, with To do/Attempted labels. Browser UI checks passed in both themes and at 150% text.

### 3.0.9 stronger completion contrast

Completed rows have a stronger green background, outlined edge and filled DONE badge. Neutral TO DO and amber ATTEMPTED badges make incomplete routes explicit. Browser checks passed, including 150% text and both themes.

### 3.0.10 completed-route palette

Changed the completed-route palette to restrained teal, with white text/checkmarks on the DONE badge. Full-row tint and strong outline remain. Browser checks passed in light/dark mode and with large text.

### 3.0.11 ascent icons

Route rows use icons without visible status words: a single check for redpoint, double check for onsight, lightning for flash, and an empty circle for unfinished routes (amber when attempted). Accessible status labels remain. UI checks cover hidden status text and accessible ascent labels.

### Current 3.0.0 navigation

Leaving Soon has been removed at user request. Main destinations are Routes, Top 10 and Account. Removal dates remain in route details. Version remains 3.0.0.

### Local Adventure

Adventure rewards new sends confirmed through Plus: Redpoint 100 XP, Flash 125 XP, Onsight 150 XP. Every 500 XP gains a level. Achievement chains for sends, redpoints, flashes, onsights, total XP and gyms derive from current rewards. Count milestones start at 1, 5, 10, 25, 50, then double without a fixed final tier; XP milestones start at 500 and double. Completed badges and progress toward the next target remain visible. Unsend removes the route reward. Attempts, grade votes and imported history earn no XP.

Progress is stored per account across gyms on this device. Offline cache clears and sign-outs preserve it; Adventure offers a confirmed reset. No server sync or backup is provided.

A new release signing key was generated on this workstation on 2026-09-29. The build script uses the included signing files by default, with `JAVA_HOME` pointing to `C:/Users/Cedric/.codex/android-tools/jdk-21.0.12.1+1`, and `ANDROID_HOME` pointing to `C:/Users/Cedric/.codex/android-tools/sdk`. This key cannot update installations signed with the previous key. Keep a private backup of the signing directory.

### Tailwind styling

Tailwind CSS is integrated with the official Vite plugin. Vue templates use utility classes for layouts, component states and responsive styling. `app/assets/main.css` contains theme colour tokens and shared element defaults; existing marker classes remain for browser QA selectors. Light, dark and system themes and device safe areas remain supported.
