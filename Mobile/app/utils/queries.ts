export const TOKEN_FIELDS = 'access { token expiresAt } refresh { token expiresAt }'
export const REFRESH = `mutation authSigninRefreshToken($refreshToken: JWT!) { tokens: authSigninRefreshToken(refreshToken: $refreshToken) { ${TOKEN_FIELDS} } }`
export const USER = 'query PlusUser { userMe { id fullName gym { id name } gymUserFavorites { gym { id name } } } }'
export const ROUTE_FIELDS = `id grade name label inAt outAt outPlannedAt setterName
  climbSetters { gymAdmin { name } } wall { nameLoc } holdColor { color nameLoc }
  climbUser(userId: $userId) { grade tickType totalTries triedFirstAtDate tickedFirstAtDate }`
export const ROUTES = `query PlusRoutes($gymId: ID!, $userId: ID!, $pagination: PaginationInputClimbs) {
  climbs(gymId: $gymId, climbType: route, pagination: $pagination) { pagination { total page perPage } data { ${ROUTE_FIELDS} } }
}`
export const ROUTE = `query PlusRoute($gymId: ID!, $id: ID!, $userId: ID!) { climb(gymId: $gymId, id: $id) { ${ROUTE_FIELDS} } }`
export const DAYS = `query PlusDays($userId: ID!, $from: DateTime!, $until: DateTime!, $pagination: PaginationInputClimbDays) {
  climbDaysPaginated(userId: $userId, totalTriesMin: 1, routesTotalTriesMin: 1, statsAtDateMin: $from, statsAtDateMax: $until, pagination: $pagination) {
    pagination { total page perPage } data { id gymId statsAtDate }
  }
}`
export const HISTORY = `query PlusHistory($gymId: ID!, $userId: ID!, $date: DateTime!, $pagination: PaginationInputClimbLogs) {
  climbLogs(gymId: $gymId, userId: $userId, climbedAtDate: $date, climbType: route, pagination: $pagination) { pagination { total page perPage } data {
    id gymId climbId climbType tickType climbedAtDate valid ticked topped
  } }
}`
export const COMMUNITY = `query PlusCommunity($gymId: ID!, $id: ID!) {
  climb(gymId: $gymId, id: $id) { gradeVoteStats { grade count } ratingVoteStats { stars count } }
}`
export const TOPPERS = `query PlusToppers($gymId: ID!, $climbId: ID!, $pagination: PaginationInputClimbUsers) {
  climbUsers(gymId: $gymId, climbId: $climbId, pagination: $pagination) {
    pagination { total page perPage } data { id user { id fullName } tickType grade }
  }
}`
