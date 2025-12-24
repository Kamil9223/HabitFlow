# REST API Plan

Versioning: All endpoints are prefixed with /api/v1. JSON uses lowerCamelCase properties. Dates use ISO-8601 (YYYY-MM-DD). Times stored/returned in UTC unless explicitly noted as localDate.

## 1. Resources
- Auth (Identity flows) – uses ASP.NET Core Identity tables
- Profile – AspNetUsers (ApplicationUser) with domain fields: timeZoneId, createdAtUtc
- Habits – table: Habits
- Checkins – table: Checkins
- Notifications – table: Notifications
- Today (computed view) – no table, derived from Habits and Checkins
- Progress (computed view) – no table, derived from Checkins and Habits snapshots

## 2. Endpoints

Conventions:
- Pagination: ?page=1&pageSize=20 (page ≥1, pageSize 1..100). Responses include totalCount and items[].
- Sorting: ?sort=createdAtUtc:desc (field:direction). Default sorts noted per endpoint.
- Filtering: simple eq/in parameters where applicable.
- Errors: RFC7807 ProblemDetails. Common statuses: 400, 401, 403, 404, 409, 422, 429, 500.
- Security: All non-auth endpoints require Bearer JWT and enforce per-user isolation; DB session context is set per request to support RLS.

### 2.1 Auth

1) POST /api/v1/auth/register
- Description: Create an account; sends email confirmation link.
- Request:
{
  "email": "user@example.com",
  "password": "string (min 8)"
}
- Response 201:
{
  "userId": "string",
  "email": "user@example.com",
  "emailConfirmed": false
}
- Success: 201 Created
- Errors: 400 (validation), 409 (email already used), 422 (invalid timeZoneId)

2) POST /api/v1/auth/confirm-email
- Description: Confirm email from link.
- Request:
{
  "userId": "string",
  "token": "string"
}
- Response 204 No Content
- Errors: 400 (invalid token), 404 (user not found), 409 (already confirmed)

3) POST /api/v1/auth/login
- Description: Authenticate and issue JWT access (and refresh, if enabled).
- Request:
{
  "email": "user@example.com",
  "password": "string"
}
- Response 200:
{
  "accessToken": "jwt-token",
  "expiresIn": 3600,
  "refreshToken": "optional-string"
}
- Errors: 400 (validation), 401 (invalid credentials), 403 (email not confirmed)

4) POST /api/v1/auth/refresh
- Description: Exchange refresh token for new access token (if refresh tokens are enabled).
- Request:
{
  "refreshToken": "string"
}
- Response 200:
{
  "accessToken": "jwt-token",
  "expiresIn": 3600,
  "refreshToken": "optional-new"
}
- Errors: 400, 401 (invalid/expired), 409 (revoked)

5) POST /api/v1/auth/forgot-password
- Description: Send password reset link.
- Request:
{
  "email": "user@example.com"
}
- Response 204 No Content
- Errors: 400 (invalid email format)

6) POST /api/v1/auth/reset-password
- Description: Reset password with token.
- Request:
{
  "email": "user@example.com",
  "token": "string",
  "newPassword": "string (min 8)"
}
- Response 204 No Content
- Errors: 400 (invalid token/policies)

7) GET /api/v1/auth/me
- Description: Return current user essentials for the app shell.
- Response 200:
{
  "userId": "string",
  "email": "user@example.com",
  "emailConfirmed": true,
  "timeZoneId": "Europe/Warsaw",
  "createdAtUtc": "2025-12-07T15:50:00Z"
}
- Errors: 401

### 2.2 Profile

1) GET /api/v1/profile
- Description: Return full profile details the app needs.
- Response 200:
{
  "userId": "string",
  "email": "user@example.com",
  "emailConfirmed": true,
  "timeZoneId": "Europe/Warsaw",
  "createdAtUtc": "2025-12-07T15:50:00Z"
}
- Errors: 401

2) PATCH /api/v1/profile/timezone
- Description: Update time zone. Takes effect from the next local day for scheduling logic.
- Request:
{
  "timeZoneId": "America/New_York"
}
- Response 204 No Content
- Errors: 400 (missing/invalid), 422 (unsupported time zone)

### 2.3 Habits

Entity enums:
- type: 1 = Start, 2 = Stop
- completionMode: 1 = Binary, 2 = Quantitative, 3 = Checklist

1) GET /api/v1/habits
- Description: List current user’s habits with filters and pagination.
- Query:
  - page, pageSize
  - type=1|2 (optional)
  - completionMode=1|2|3 (optional)
  - active=true|false (optional; default true means deadline is null or >= today local)
  - search=string (title contains, optional)
  - sort=createdAtUtc:desc|title:asc|deadlineDate:asc (default createdAtUtc:desc)
- Response 200:
{
  "totalCount": 2,
  "items": [
    {
      "id": 101,
      "title": "Read",
      "description": "Up to 280 chars",
      "type": 1,
      "completionMode": 2,
      "daysOfWeekMask": 85,
      "targetValue": 10,
      "targetUnit": "pages",
      "deadlineDate": "2026-03-31",
      "createdAtUtc": "2025-12-07T15:57:00Z"
    }
  ]
}
- Errors: 401

2) POST /api/v1/habits
- Description: Create a new habit (max 20 per user).
- Request:
{
  "title": "string (<=80 chars)",
  "description": "string (<=280 chars, optional)",
  "type": 1,
  "completionMode": 1,
  "daysOfWeekMask": 1..127,
  "targetValue": 1..100,
  "targetUnit": "string (<=32, optional)",
  "deadlineDate": "YYYY-MM-DD (optional)"
}
- Response 201:
{
  "id": 101,
  "title": "string",
  "description": "string or null",
  "type": 1,
  "completionMode": 1,
  "daysOfWeekMask": 85,
  "targetValue": 10,
  "targetUnit": "pages",
  "deadlineDate": "2026-03-31",
  "createdAtUtc": "2025-12-07T16:00:00Z"
}
- Errors: 400 (validation), 401, 409 (habit limit reached)

3) GET /api/v1/habits/{id}
- Description: Get habit details by id (owned by current user).
- Response 200: Habit object (as above)
- Errors: 401, 404

4) PATCH /api/v1/habits/{id}
- Description: Update mutable fields; affects only future days; past checkins remain immutable.
- Request (any subset):
{
  "title": "string (<=80)",
  "description": "string (<=280)",
  "type": 1|2,
  "completionMode": 1|2|3,
  "daysOfWeekMask": 1..127,
  "targetValue": 1..100,
  "targetUnit": "string (<=32|null)",
  "deadlineDate": "YYYY-MM-DD|null"
}
- Response 200: Updated habit
- Errors: 400, 401, 404

5) DELETE /api/v1/habits/{id}
- Description: Hard delete habit (cascades to checkins and notifications).
- Response 204 No Content
- Errors: 401, 404

6) GET /api/v1/habits/{id}/calendar
- Description: Read-only calendar statuses for a date range per habit.
- Query:
  - from=YYYY-MM-DD (required)
  - to=YYYY-MM-DD (required, inclusive, max range suggested 90 days)
- Response 200:
{
  "habitId": 101,
  "from": "2025-12-01",
  "to": "2025-12-31",
  "days": [
    {
      "date": "2025-12-01",
      "isPlanned": true,
      "actualValue": 7,
      "targetValueSnapshot": 10,
      "completionModeSnapshot": 2,
      "habitTypeSnapshot": 1,
      "dailyScore": 0.7
    },
    {
      "date": "2025-12-02",
      "isPlanned": true,
      "actualValue": 0,
      "dailyScore": 0.0
    }
  ]
}
- Errors: 400 (range), 401, 404

### 2.4 Today (computed)

1) GET /api/v1/today
- Description: Returns today’s planned items in the user’s local time zone with check-in shortcuts.
- Query:
  - date=YYYY-MM-DD (optional; default “today” in user’s time zone)
- Response 200:
{
  "date": "2025-12-07",
  "items": [
    {
      "habitId": 101,
      "title": "Read",
      "type": 1,
      "completionMode": 2,
      "targetValue": 10,
      "targetUnit": "pages",
      "isPlanned": true,
      "hasCheckin": false
    }
  ]
}
- Errors: 401

### 2.5 Checkins

Rules:
- One checkin per habit per localDate (unique).
- Backfill up to 7 days back from user’s local “today”.
- No updates after creation. No edit endpoint.
- Value clamped by TargetValueSnapshot; app enforces range.

1) POST /api/v1/habits/{habitId}/checkins
- Description: Create daily checkin for a habit and local date.
- Request:
{
  "localDate": "YYYY-MM-DD",
  "actualValue": 0..N
}
- Response 201:
{
  "id": 9876,
  "habitId": 101,
  "userId": "string",
  "localDate": "2025-12-07",
  "actualValue": 7,
  "targetValueSnapshot": 10,
  "completionModeSnapshot": 2,
  "habitTypeSnapshot": 1,
  "isPlanned": true,
  "createdAtUtc": "2025-12-07T22:01:00Z"
}
- Success: 201 Created
- Errors:
  - 400 (invalid localDate format, out-of-range actualValue)
  - 401
  - 403 (not owner)
  - 404 (habit not found)
  - 409 (duplicate checkin for date)
  - 422 (not allowed: after deadline, not planned day, >7 days back)

2) GET /api/v1/habits/{habitId}/checkins
- Description: List checkins for a date range (for charts, history).
- Query: from=YYYY-MM-DD, to=YYYY-MM-DD (required)
- Response 200:
{
  "habitId": 101,
  "from": "2025-11-01",
  "to": "2025-11-30",
  "items": [
    {
      "id": 1,
      "localDate": "2025-11-02",
      "actualValue": 1,
      "targetValueSnapshot": 1,
      "completionModeSnapshot": 1,
      "habitTypeSnapshot": 1,
      "isPlanned": true
    }
  ]
}
- Errors: 400, 401, 404

3) GET /api/v1/checkins
- Description: Optional convenience list for a specific localDate across all habits (today view backfill).
- Query: date=YYYY-MM-DD (required)
- Response 200:
{
  "date": "2025-12-07",
  "items": [
    {
      "id": 9876,
      "habitId": 101,
      "localDate": "2025-12-07",
      "actualValue": 7,
      "isPlanned": true
    }
  ]
}
- Errors: 400, 401

### 2.6 Progress (computed)

1) GET /api/v1/habits/{habitId}/progress/rolling
- Description: Rolling success rate series for 7 or 30 days ending at until (inclusive, in user local).
- Query:
  - windowDays=7|30 (required)
  - until=YYYY-MM-DD (optional; default today in user’s local time)
- Response 200:
{
  "habitId": 101,
  "windowDays": 7,
  "until": "2025-12-07",
  "points": [
    {
      "date": "2025-12-01",
      "plannedDays": 3,
      "sumDailyScore": 2.1,
      "successRate": 0.7
    }
  ]
}
- Errors: 400, 401, 404

### 2.7 Notifications

Enums:
- type: 1 = MissDue
- aiStatus: 1 = Success, 2 = Fallback, 3 = Error (optional diagnostic)

1) GET /api/v1/notifications
- Description: List notifications for the current user.
- Query:
  - page, pageSize
  - sort=createdAtUtc:desc (default)
- Response 200:
{
  "totalCount": 3,
  "items": [
    {
      "id": 555,
      "habitId": 101,
      "localDate": "2025-12-06",
      "type": 1,
      "content": "You missed yesterday...",
      "aiStatus": 2,
      "createdAtUtc": "2025-12-07T00:30:00Z"
    }
  ]
}
- Errors: 401

2) GET /api/v1/notifications/{id}
- Description: Get a notification by id (owned by current user).
- Response 200: Notification object
- Errors: 401, 404

Notes:
- API is read-only for notifications in MVP. Generation is handled by a background job.
- Optional internal endpoint (dev/admin only) can exist to trigger miss-due scan, but it’s not part of public API.

## 3. Authentication and Authorization

- Mechanism: JWT Bearer tokens issued by ASP.NET Core Identity.
- Token contents: sub (userId), email, emailConfirmed, iat, exp; optional roles (user).
- Storage: Access token stored on client; refresh token (if used) is rotating and revocable.
- Authorization:
  - All /api/v1/** except /auth/** require Authorization: Bearer <token>.
  - Per-request DB session context is set: EXEC sp_set_session_context @key=N'user_id', @value=@currentUserId, @read_only=1 to enforce row-level security (RLS) at the database layer.
  - Ownership: Every resource access is filtered by currentUserId via RLS and application-level checks, returning 404 when accessing others’ resources.
- CSRF: Not applicable for pure Bearer-based API; Blazor Server UI uses its own session model.
- Rate Limiting:
  - Default: 60 requests/min per client.
  - Auth endpoints: 10 requests/min per client.
  - Returns 429 ProblemDetails when exceeded.
- Transport: HTTPS only. HSTS enabled in production.

## 4. Validation and Business Logic

### 4.1 Common Validation
- Strings: title <= 80; description <= 280; targetUnit <= 32; email valid format.
- Enumerations: type ∈ {1,2}; completionMode ∈ {1,2,3}; notification type ∈ {1}.
- Time zone: timeZoneId must be a valid IANA identifier supported by the runtime.
- Dates: localDate uses user’s time zone calendar; deadlineDate optional; from/to ranges max 90 days for public endpoints (recommendation).

### 4.2 Habit Rules
- daysOfWeekMask: 1..127 (bitmask: Mon=1, ..., Sun=64); API validates 1 ≤ mask ≤ 127.
- targetValue: 1..100 (per PRD; DB allows up to 1000, API tightens to 100).
- Limit: Max 20 habits per user; creation returns 409 when limit exceeded.
- Editing: Affects future computation only; historical checkins keep snapshot values unchanged.
- Deletion: Hard delete; cascades to checkins and notifications.

### 4.3 Checkin Rules
- Uniqueness: One checkin per (habitId, localDate); duplicate returns 409.
- Planning: Checkin allowed only if the day is planned by daysOfWeekMask (isPlanned=true), else 422.
- Deadline: If deadlineDate exists, localDate must be ≤ deadlineDate; otherwise 422.
- Backfill window: localDate must be within [today-7, today] in user’s time zone; otherwise 422.
- Range: 0 ≤ actualValue ≤ TargetValueSnapshot. Values above are clamped to TargetValueSnapshot before save; invalid negative values return 400.
- Snapshots: On creation, copy targetValue, completionMode, and type from Habit to snapshot fields in Checkins; no updates later.
- Immutability: No update/delete endpoint for checkins in MVP.

### 4.4 Daily Score and Success Rate
- For completionMode=1 (Binary): dailyScore = actualValue > 0 ? 1.0 : 0.0 (actualValue ∈ {0,1}).
- For completionMode ∈ {2,3} (Quantitative/Checklist):
  - ratio = actualValue / TargetValueSnapshot
  - ratioClamped = min(max(ratio, 0), 1)
- For type=Start: dailyScore = ratioClamped.
- For type=Stop: dailyScore = 1 - ratioClamped.
- Rolling successRate (7/30):
  - successRate = sum(dailyScore in window) / plannedDaysInWindow (0 if plannedDays=0).
- The API computes and returns successRate in progress endpoints; storage remains in normalized tables without views in MVP.

### 4.5 Notifications
- Trigger: Background job runs after end of user’s local day and generates MissDue for planned but missing checkins.
- Uniqueness: One notification per (habitId, localDate, type); unique constraint enforced. Attempted duplicates are ignored or yield 409 in internal ops.
- AI Generation: Primary: LLM-based content. On timeout/error, fallback static templates are used; aiStatus and aiError (truncated) recorded for diagnostics.
- Exposure: Read-only via API; users can’t mutate content in MVP.

### 4.6 Error Responses (ProblemDetails examples)
- 400 Bad Request:
{
  "type": "https://habitflow/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "title": ["Max length is 80."]
  }
}
- 401 Unauthorized: Bearer token missing/invalid.
- 403 Forbidden: Attempted action is not allowed for the authenticated user.
- 404 Not Found: Resource does not exist or not accessible.
- 409 Conflict: Duplicate checkin, habit limit reached, or unique constraint violation.
- 422 Unprocessable Entity: Business rule violation (not planned day, after deadline, >7 days back).
- 429 Too Many Requests: Rate limit exceeded.

## 5. Data Models (DTOs)

Note: These are representative JSON contracts; server may include additional metadata.

- HabitCreateRequest/HabitUpdateRequest:
{
  "title": "string",
  "description": "string|null",
  "type": 1,
  "completionMode": 2,
  "daysOfWeekMask": 85,
  "targetValue": 10,
  "targetUnit": "pages",
  "deadlineDate": "YYYY-MM-DD|null"
}

- HabitResponse:
{
  "id": 101,
  "title": "string",
  "description": "string|null",
  "type": 1,
  "completionMode": 2,
  "daysOfWeekMask": 85,
  "targetValue": 10,
  "targetUnit": "pages",
  "deadlineDate": "YYYY-MM-DD|null",
  "createdAtUtc": "2025-12-07T16:00:00Z"
}

- CheckinCreateRequest:
{
  "localDate": "YYYY-MM-DD",
  "actualValue": 0
}

- CheckinResponse:
{
  "id": 9876,
  "habitId": 101,
  "localDate": "YYYY-MM-DD",
  "actualValue": 7,
  "targetValueSnapshot": 10,
  "completionModeSnapshot": 2,
  "habitTypeSnapshot": 1,
  "isPlanned": true,
  "createdAtUtc": "2025-12-07T22:01:00Z"
}

- TodayResponse:
{
  "date": "YYYY-MM-DD",
  "items": [
    {
      "habitId": 101,
      "title": "Read",
      "type": 1,
      "completionMode": 2,
      "targetValue": 10,
      "targetUnit": "pages",
      "isPlanned": true,
      "hasCheckin": false
    }
  ]
}

- ProgressRollingResponse:
{
  "habitId": 101,
  "windowDays": 7,
  "until": "YYYY-MM-DD",
  "points": [
    {
      "date": "YYYY-MM-DD",
      "plannedDays": 3,
      "sumDailyScore": 2.1,
      "successRate": 0.7
    }
  ]
}

- NotificationResponse:
{
  "id": 555,
  "habitId": 101,
  "localDate": "YYYY-MM-DD",
  "type": 1,
  "content": "string",
  "aiStatus": 2,
  "createdAtUtc": "2025-12-07T00:30:00Z"
}

- PagedResponse<T>:
{
  "totalCount": 123,
  "items": [ ...T... ]
}

## 6. Performance and Index Usage

- Habits list: IX_Habits_UserId_CreatedAtUtc supports user-scoped listing with createdAt desc; include columns minimize lookups.
- Today and rolling windows: Checkins clustered index on (UserId, LocalDate, HabitId) supports fast range scans for “today” and window series.
- Habit calendar: IX_Checkins_HabitId_LocalDate supports per-habit range scans and covers aggregates via INCLUDE columns.
- Notifications list: IX_Notifications_UserId_CreatedAtUtc supports user-scoped chronological listing.

## 7. Security and Multi-Tenancy

- DB Row-Level Security (RLS): Per-connection session context sets user_id to authenticated user; the database filters/blocks cross-user access.
- Application layer mirrors RLS with ownership checks and avoids exposing existence of foreign resources (404 preferred over 403 where appropriate).
- Secrets: Email tokens and refresh tokens are short-lived and stored securely server-side (hashed) if rotation is used.
- Transport: HTTPS enforced; production with HSTS; cookie flags secure if cookies are used in the UI.

## 8. Rate Limiting and Abuse Mitigation

- Global: 60 RPM per client; burst bucket can be configured.
- Auth: 10 RPM per client for login, confirm, reset endpoints.
- Responses: 429 with Retry-After when available.
- Optional S2: refined notification rate limits (positive “congrats” throttling); “miss due” remains one-per-day-per-habit.

## 9. Assumptions

- API consumers will use Bearer JWT; Blazor Server UI may rely on a separate session, but the REST layer is token-based.
- Time zone identifiers follow IANA database; conversion is performed server-side for localDate logic.
- For description, the API enforces <=280 chars to match PRD (DB allows longer, but API tightens the constraint).
- Calendar and progress endpoints are read-only and computed in the application layer without DB views/TVFs in MVP.

## 10. Non-Goals (MVP)

- External push/email notifications (in-app only).
- Checkin editing/undo; no soft deletes.
- Advanced analytics/telemetry dashboards; only basic metrics may be logged internally.
