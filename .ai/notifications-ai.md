# AI Notification Generation - Implementation Plan

**Status:** Planning
**Related PRD Requirements:** US-017, US-022, US-026
**Estimated Effort:** 14-20 hours
**Last Updated:** 2026-01-24

---

## 1. Overview

This document describes the architecture and implementation flow for the AI-powered notification generation system. The system automatically detects "miss due" events (when users fail to complete habits on scheduled days) and generates personalized, motivational notifications using an LLM integration with a fallback mechanism.

### Key Capabilities

- **Automatic Detection**: Background job identifies overdue habit completions
- **AI Generation**: LLM creates personalized motivational messages
- **Fallback Mechanism**: Template-based messages when AI fails
- **Deduplication**: Prevents duplicate notifications for same habit/date
- **User Timezone Awareness**: Respects user's local timezone for "miss due" calculations
- **Status Tracking**: Records AI generation success/failure/fallback for diagnostics

---

## 2. System Architecture

### 2.1 Components Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Background Job Scheduler                     │
│                    (Runs daily at 00:30 UTC)                    │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              NotificationGenerationService                       │
│  - Queries users with pending habits                            │
│  - Determines which habits are "miss due"                       │
│  - Orchestrates notification creation                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                  INotificationContentGenerator                   │
│  - Interface for notification content generation                │
└────────────────────────┬────────────────────────────────────────┘
                         │
            ┌────────────┴────────────┐
            ▼                         ▼
┌──────────────────────┐    ┌──────────────────────┐
│ AiContentGenerator   │    │ FallbackGenerator    │
│ - LLM integration    │    │ - Template library   │
│ - Retry logic        │    │ - Pattern matching   │
│ - Prompt engineering │    │ - Randomization      │
└──────────────────────┘    └──────────────────────┘
            │                         │
            └────────────┬────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    NotificationRepository                        │
│  - Stores notification in database                              │
│  - Records AiStatus (Success/Fallback/Error)                    │
│  - Prevents duplicates (UserId + HabitId + LocalDate)           │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Key Interfaces

#### INotificationGenerationService
- **Responsibility**: Main orchestrator for notification creation
- **Methods**:
  - `GenerateNotificationsAsync(CancellationToken)` - Entry point for background job
  - Returns summary of generated notifications (count, errors)

#### INotificationContentGenerator
- **Responsibility**: Abstract content generation strategy
- **Methods**:
  - `GenerateAsync(userId, habitId, habitName, context, cancellationToken)` - Returns (content, aiStatus, aiError?)
  - Implementations: `AiContentGenerator`, `FallbackContentGenerator`

#### ILlmClient
- **Responsibility**: Communication with LLM API (e.g., OpenAI, Anthropic)
- **Methods**:
  - `GenerateCompletionAsync(prompt, maxTokens, temperature, cancellationToken)` - Returns response text
  - Handles API authentication, rate limiting, retries

#### INotificationRepository
- **Responsibility**: Database access for notifications
- **Methods**:
  - `CreateAsync(notification)` - Persists new notification
  - `ExistsAsync(userId, habitId, localDate)` - Checks for duplicates
  - `GetPendingHabitsAsync(localDate)` - Queries habits without completions for given date

---

## 3. Notification Generation Flow

### 3.1 Background Job Trigger

**Frequency**: Daily at 00:30 UTC
**Rationale**: After midnight in most timezones, allowing time for users to still complete yesterday's habits

**Job Configuration**:
- Uses Quartz.NET or Hangfire for scheduling
- Registered in `Program.cs` as hosted service
- Scoped service injection for database access
- Timeout: 30 minutes
- Retry policy: 3 attempts with exponential backoff

### 3.2 Detection Algorithm

**Step 1: Identify Candidate Habits**

For each user in the system:
1. Calculate user's "current local date" using `User.TimeZoneId`
2. Calculate "yesterday's local date" (currentLocalDate - 1 day)
3. Query habits where:
   - `Habit.UserId = user.Id`
   - `Habit.IsActive = true`
   - Yesterday was a scheduled day according to habit's recurrence pattern (DaysOfWeek)

**Step 2: Filter Out Completed Habits**

For each candidate habit:
1. Check if `HabitCompletion` exists for (HabitId, yesterday's LocalDate)
2. If completion exists → SKIP (habit was completed)
3. If no completion exists → "MISS DUE" event detected

**Step 3: Check for Existing Notifications**

Before generating notification:
1. Query `Notification` table for (UserId, HabitId, yesterday's LocalDate)
2. If notification exists → SKIP (prevent duplicates)
3. If no notification exists → PROCEED to generation

### 3.3 Content Generation Strategy

**Primary Path: AI Generation**

1. **Context Gathering**:
   - Habit name, current streak, total completions
   - User's historical completion rate for this habit
   - Time since last completion
   - User's timezone for personalization

2. **Prompt Construction**:
   ```
   System Role: You are a supportive habit coach

   User Context:
   - Habit: "{habitName}"
   - Streak before miss: {streakDays} days
   - Total completions: {totalCount}
   - Last completed: {daysAgo} days ago

   Task: Generate a SHORT (max 100 words), empathetic, motivational message
   that acknowledges the missed day without guilt-tripping, and encourages
   the user to get back on track. Use a warm, personal tone.

   Rules:
   - Do NOT use emojis
   - Do NOT use user's name (we don't have it)
   - Focus on the habit's value, not the failure
   - End with actionable encouragement
   ```

3. **LLM Invocation**:
   - Send prompt to configured LLM API
   - Max tokens: 150
   - Temperature: 0.7 (balance creativity and consistency)
   - Timeout: 10 seconds

4. **Response Validation**:
   - Check content length ≤ 1024 characters (database constraint)
   - Basic profanity/harmful content filter
   - If validation fails → fallback to templates

5. **Success Recording**:
   - Store content in `Notification.Content`
   - Set `AiStatus = AiGenerationStatus.Success`
   - Set `AiError = null`

**Fallback Path: Template-Based Generation**

Triggered when:
- LLM API is unavailable (connection error, timeout)
- LLM returns error response (rate limit, invalid API key)
- Generated content fails validation
- AI feature is disabled in configuration

**Fallback Process**:

1. **Template Selection**:
   - Maintain library of 10-15 pre-written templates
   - Templates categorized by: streak length, completion rate
   - Random selection from appropriate category

2. **Template Personalization**:
   - Replace placeholders: `{habitName}`, `{streakDays}`, `{totalCompletions}`
   - Example template: "You had a great {streakDays}-day streak with {habitName}! One missed day doesn't erase your progress. Ready to continue today?"

3. **Fallback Recording**:
   - Store template-generated content in `Notification.Content`
   - Set `AiStatus = AiGenerationStatus.Fallback`
   - Set `AiError = "AI generation unavailable, used template"`

**Error Path: Complete Failure**

If both AI and fallback fail (database error, critical bug):
- Store generic message: "You missed your habit yesterday. Let's get back on track!"
- Set `AiStatus = AiGenerationStatus.Error`
- Set `AiError = "{exception message}"` (max 512 chars)
- Log error for investigation
- Continue processing other habits (isolation)

### 3.4 Persistence

**Create Notification Entity**:
```
Notification:
  Id: (auto-generated)
  UserId: {userId}
  HabitId: {habitId}
  LocalDate: {yesterday's date}
  Type: NotificationType.MissDue
  Content: {generated content}
  AiStatus: Success|Fallback|Error
  AiError: {error message if applicable}
  CreatedAtUtc: {current UTC timestamp}
```

**Database Constraints**:
- Unique index on (UserId, HabitId, LocalDate, Type) prevents duplicate notifications
- Constraint violation should be caught and logged (race condition indicator)

---

## 4. Configuration & Settings

### 4.1 LLM Provider Configuration

**Options Pattern** in `appsettings.json`:

```
LlmSettings:
  Provider: "OpenAI" | "Anthropic" | "AzureOpenAI"
  ApiKey: "{secret - use User Secrets / Azure Key Vault}"
  Model: "gpt-4o-mini" | "claude-3-5-haiku-latest"
  MaxTokens: 150
  Temperature: 0.7
  TimeoutSeconds: 10
  MaxRetries: 2
  MaxDailyRequests: 100
  Enabled: true/false
```

### 4.2 Job Configuration

```
NotificationJobSettings:
  Enabled: true/false
  CronSchedule: "0 30 0 * * ?" (daily at 00:30 UTC)
  BatchSize: 100 (process users in batches)
  MaxExecutionMinutes: 30
```

### 4.3 Feature Flags

- `Features:AiNotifications:Enabled` - Master switch for AI generation
- `Features:AiNotifications:FallbackOnly` - Skip AI, use only templates (testing)
- `Features:Notifications:Enabled` - Disable entire notification system

---

## 5. Error Handling & Resilience

### 5.1 LLM API Failures

**Retry Strategy**:
- Transient errors (503, 429): Retry with exponential backoff (1s, 2s, 4s)
- Authentication errors (401, 403): Log error, skip retries, use fallback
- Rate limits (429): Respect Retry-After header, use fallback after max retries
- Timeouts: 10s timeout per request, use fallback after timeout

**Circuit Breaker Pattern**:
- After 5 consecutive LLM failures, open circuit for 5 minutes
- During open circuit, skip AI generation and use fallback directly
- Reduces unnecessary API calls when service is clearly down

**Cost Guard (MVP)**:
- `MaxDailyRequests` caps the number of AI calls per daily job run
- After reaching the limit, the system uses fallback templates for the rest of the run

### 5.2 Database Failures

**Transactional Safety**:
- Each notification creation is independent transaction
- Failure in one notification doesn't roll back others
- Log all database errors with context (userId, habitId, date)

**Duplicate Prevention**:
- Check for existing notification BEFORE content generation (optimization)
- Handle unique constraint violations gracefully (log and continue)

### 5.3 Job Execution Failures

**Partial Success**:
- Job tracks: total habits processed, notifications created, errors encountered
- Log summary at job completion
- Alert if error rate > 10%

**Idempotency**:
- Job can be re-run for same date without creating duplicates
- Useful for manual recovery after failed job

---

## 6. Testing Strategy

### 6.1 Unit Tests

**NotificationGenerationService Tests**:
- Mock dependencies (INotificationRepository, INotificationContentGenerator, ILlmClient)
- Test cases:
  - Habits with no completion yesterday → notification created
  - Habits completed yesterday → no notification
  - Existing notification for habit/date → skip
  - User timezone handling (edge cases: UTC-12, UTC+14)
  - Multiple habits for single user

**AiContentGenerator Tests**:
- Mock ILlmClient
- Test cases:
  - Successful AI response → AiStatus.Success
  - LLM API error → fallback to template → AiStatus.Fallback
  - Content too long → truncate or regenerate
  - Prompt construction with different habit contexts

**FallbackContentGenerator Tests**:
- Test cases:
  - Template selection based on streak length
  - Placeholder replacement accuracy
  - Template coverage for all scenarios

### 6.2 Integration Tests

**Background Job Test**:
- Use TestContainers for SQL Server
- Seed database with:
  - 3 users with different timezones
  - 5 habits (some completed yesterday, some not)
  - Existing notifications for some habits
- Run NotificationGenerationService
- Assert:
  - Correct number of notifications created
  - No duplicates
  - Correct AiStatus values
  - Content not empty

**LLM Integration Test** (Optional, requires API key):
- Test against real LLM API
- Verify prompt/response handling
- Tagged as `[Trait("Category", "External")]` - excluded from CI
 - **Nice to have (post-MVP)**: keep as optional/manual test when API keys are available

### 6.3 End-to-End Test (US-026)

**Test Scenario**:
1. User creates habit "Morning Run" with Monday-Friday schedule
2. User completes habit on Monday, Tuesday
3. User does NOT complete on Wednesday
4. Simulate job execution on Thursday 00:30 UTC
5. Verify notification appears in GET /api/v1/notifications
6. Verify notification content contains "Morning Run"
7. Verify AiStatus is either Success or Fallback (not Error)
8. Verify no duplicate notification created if job runs again

**Tools**:
- Playwright for E2E automation
- Test database with seeded data
- Mock LLM API for deterministic content

---

## 7. Monitoring & Observability

### 7.1 Metrics

**Application Insights / Prometheus Metrics**:
- `habitflow.notifications.generated.total` - Counter
- `habitflow.notifications.ai_status` - Counter by status (Success/Fallback/Error)
- `habitflow.notifications.generation_duration_ms` - Histogram
- `habitflow.notifications.llm_api_calls.total` - Counter
- `habitflow.notifications.llm_api_errors.total` - Counter by error type

### 7.2 Logging

**Structured Logging** (Serilog):
- Job start/completion: `Information` level
- Each notification created: `Debug` level
- LLM API errors: `Warning` level (with retry attempts)
- Database errors: `Error` level
- Job failure: `Error` level with full exception

**Log Context**:
- UserId (hashed for privacy)
- HabitId
- LocalDate
- NotificationType
- AiStatus
- Execution duration

### 7.3 Alerts

**Critical Alerts**:
- Job fails to complete (30min timeout exceeded)
- Error rate > 25% in single job execution
- LLM API returns 401/403 (authentication broken)

**Warning Alerts**:
- Error rate > 10% in single job execution
- Circuit breaker opened (LLM service down)
- Fallback usage > 50% (AI degradation)

---

## 8. Implementation Phases

### Phase 1: Core Infrastructure (4-6 hours)
- Create interfaces (INotificationGenerationService, INotificationContentGenerator, ILlmClient)
- Implement FallbackContentGenerator with 10 templates
- Create NotificationRepository methods (CreateAsync, ExistsAsync, GetPendingHabitsAsync)
- Add configuration sections to appsettings.json
- Unit tests for fallback generator

### Phase 2: AI Integration (3-4 hours)
- Implement ILlmClient for OpenAI/Anthropic (choose one)
- Implement AiContentGenerator with prompt engineering
- Add retry logic and circuit breaker
- Unit tests with mocked LLM client
- Integration test with real API (optional, manual)

### Phase 3: Generation Service (3-4 hours)
- Implement NotificationGenerationService orchestration
- Add timezone-aware date calculations
- Implement miss-due detection algorithm
- Add deduplication logic
- Unit tests with mocked dependencies

### Phase 4: Background Job (2-3 hours)
- Configure Quartz.NET/Hangfire job scheduler
- Create hosted service for job execution
- Add job execution logging and metrics
- Test job scheduling (trigger manually)

### Phase 5: E2E Testing & Refinement (2-3 hours)
- Create E2E test scenario (US-026)
- Test with multiple timezones, edge cases
- Refine prompt based on real LLM outputs
- Add additional templates based on feedback
- Performance optimization if needed

---

## 9. Security & Privacy Considerations

### 9.1 Data Privacy

**LLM API Calls**:
- Do NOT send user email or personally identifiable information to LLM
- Send only: habit name, numerical metrics (streak, completions)
- Ensure LLM provider has acceptable data processing terms
- Consider on-premise LLM for sensitive deployments

### 9.2 API Key Management

**Secrets Storage**:
- Development: User Secrets (dotnet user-secrets)
- Production: Azure Key Vault or AWS Secrets Manager
- Never commit API keys to source control
- Rotate keys regularly (quarterly)

### 9.3 Rate Limiting

**LLM API Usage**:
- Respect provider's rate limits
- Implement client-side rate limiting (e.g., max 100 requests/minute)
- Batch processing to smooth out load
- Monitor monthly API costs

---

## 10. Future Enhancements

### 10.1 Personalization Improvements
- Learn user's preferred message tone from feedback (thumbs up/down on notifications)
- A/B test different prompt strategies
- Multi-language support based on user locale

### 10.2 Advanced AI Features
- Generate habit-specific tips based on completion patterns
- Predict high-risk miss days and send preemptive encouragement
- Use RAG (Retrieval-Augmented Generation) with habit research articles

### 10.3 Delivery Channels
- Push notifications (mobile app)
- Email digest (daily/weekly summary)
- SMS integration for critical streaks

### 10.4 Analytics Dashboard
- Admin view of notification effectiveness
- AI vs Fallback performance comparison
- User engagement metrics (notification click-through rate)

### 10.5 Deferred for post-MVP
- Retry-After handling for LLM rate limits
- Expanded telemetry/metrics and alerting
- Advanced content safety filtering
- Performance optimizations (compiled queries, additional indexing for reporting)
- Optional LLM integration test suite (manual, requires API key)

---

## 11. PRD Requirements Mapping

| PRD Requirement | Implementation Component | Status |
|-----------------|--------------------------|--------|
| **US-017**: Użytkownik otrzymuje powiadomienie "miss due" | NotificationGenerationService + Background Job | Planned |
| **US-022**: Mechanizm fallback przy błędzie AI | FallbackContentGenerator + AiContentGenerator error handling | Planned |
| **US-026**: E2E test generowania powiadomień AI | Playwright test scenario in Phase 5 | Planned |
| **F-005**: AI generuje motywacyjne treści | AiContentGenerator + ILlmClient | Planned |
| **F-006**: Powiadomienia z AI/Fallback statusem | Notification.AiStatus field (already exists) | ✅ Schema ready |
| **F-007**: Diagnostyka błędów AI | Notification.AiError field + logging | ✅ Schema ready |

---

## 12. Dependencies & Prerequisites

**External Services**:
- OpenAI API account (or Anthropic Claude API)
- API key with sufficient quota (~$5-10/month estimated for small user base)

**NuGet Packages**:
- No additional nuget for Background job - use BackgroundService
- `Microsoft.Extensions.Http.Polly` - Retry policies and circuit breaker
- OpenAI SDK or Anthropic SDK (depending on chosen provider)

**Database**:
- Notification table schema already exists ✅
- No migrations needed

**Configuration**:
- Add LlmSettings and NotificationJobSettings to appsettings.json
- Configure User Secrets for development API keys
- Configure Azure Key Vault for production

---

## 13. Risk Assessment

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| LLM API quota exceeded | HIGH | MEDIUM | Implement rate limiting, monitor usage, fallback to templates |
| LLM generates inappropriate content | MEDIUM | LOW | Content validation filters, user reporting, template fallback |
| Background job fails silently | HIGH | LOW | Comprehensive logging, alerting, job execution metrics |
| High AI API costs | MEDIUM | MEDIUM | Cost monitoring, monthly budgets, fallback-only mode option |
| Timezone calculation bugs | MEDIUM | MEDIUM | Extensive unit tests, E2E tests with multiple timezones |
| Notification spam (duplicates) | HIGH | LOW | Unique database constraint, idempotency checks |

---

**End of Implementation Plan**
