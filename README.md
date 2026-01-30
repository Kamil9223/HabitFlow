# HabitFlow

A simple web-based habit tracker that helps users build and maintain positive behaviors while eliminating negative habits.

## Table of Contents

- [Project Description](#project-description)
- [Tech Stack](#tech-stack)
- [Getting Started Locally](#getting-started-locally)
- [Available Scripts](#available-scripts)
- [Project Scope](#project-scope)
- [Project Status](#project-status)
- [License](#license)

## Project Description

HabitFlow is a B2C web application designed for individuals who want to implement and monitor their habits—whether it's exercising, learning, improving sleep, or any other behavioral change. The application provides:

- **Unified habit model** supporting two types of habits:
  - **Start habits** (things to begin doing)
  - **Stop habits** (things to stop doing)

- **Clear progress visualization** with rolling success rate metrics (7-day and 30-day windows)

- **AI-powered motivational notifications** displayed in-app, triggered when users miss scheduled habit completions

- **Daily check-ins** to track progress with a simple, intuitive interface

- **Read-only calendar view** showing past performance and future planning

The MVP focuses on simplicity and clarity, helping beginners easily adopt habit tracking without overwhelming complexity.

## Tech Stack

### Backend
- **.NET 9** with **ASP.NET Core 9** (C# 13)
- **Blazor Server** for server-side rendering with SignalR connections
- **Entity Framework Core** (code-first approach with migrations)
- **ASP.NET Core Identity** for authentication and authorization (email verification, password reset)

### Database
- **SQL Server 2022**
- Relational model storing habits, schedules, check-ins, and notifications
- Optimized with transactions, consistency guarantees, and indexes for efficient queries

### Integrations & Services
- **LLM provider** via HTTP for AI-generated motivational content with fallback to static templates
- **Background job scheduler**: Hangfire for triggering "miss due" notifications

### CI/CD & Quality
- **GitHub Actions** for continuous integration and deployment (build, tests, migrations)
- **Testing framework**: XUnit with NSubstitute for mocking
- **Unit tests** for validators, command/query handlers, and business logic (success_rate calculations)
- **Integration tests** with TestContainers (SQL Server) for API endpoints testing
- **Component tests** with bUnit for Blazor UI components
- **End-to-end tests** with Playwright for critical user paths (registration → habit creation → check-in → calendar/chart → notification)
- **Code coverage**: Target ≥80% for Core layer (business logic)
- **Test plan**: Detailed strategy available in `.ai/test-plan.md`

## Getting Started Locally

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (recommended) for local SQL Server and SMTP test inbox
  - Alternatively: [SQL Server 2022](https://www.microsoft.com/sql-server/sql-server-downloads) or SQL Server Express (if not using Docker)
- Git
- [.NET EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (for database migrations):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd HabitFlow
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Start local infrastructure (SQL Server + SMTP test inbox):**
   ```bash
   cd .docker
   docker-compose up -d
   ```

4. **Configure connection strings:**
   - The default configuration in `HabitFlow.Api/appsettings.Development.json` works with the Docker SQL Server
   - For custom connection strings, use `dotnet user-secrets`:
     ```bash
     cd HabitFlow.Api
     dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
     ```

5. **Optional: run the dev setup script (user-secrets):**
   ```bash
   # from repo root (PowerShell)
   .\dev-setup.ps1
   ```

6. **Configure SMTP for local email testing:**
   - `smtp4dev` web UI is available at `http://localhost:3000`
   - Default configuration in `HabitFlow.Api/appsettings.Development.json` already points to smtp4dev:
      - `Email:Smtp:Host = localhost`
      - `Email:Smtp:Port = 2525`
      - `Email:Smtp:EnableSsl = false`
      - `Email:LinkBaseUrl = https://localhost:7231` (Blazor app URL for email links)
      - `Email:FromEmail = no-reply@habitflow.local`
     - `Email:FromName = HabitFlow`
   - **Note:** smtp4dev doesn't require authentication (Username/Password are optional and can be left empty)

7. **Trust the HTTPS development certificate:**
   ```bash
   dotnet dev-certs https --trust
   ```

8. **Apply database migrations:**
   ```bash
   dotnet ef database update --project HabitFlow.Api
   ```
   **Important:** This must be done before running the application for the first time.

9. **Run the application:**

   **Option A:** Run API and Blazor app separately:
   ```bash
   # Terminal 1 - API
   dotnet run --project HabitFlow.Api

   # Terminal 2 - Blazor app
   dotnet run --project HabitFlow.Blazor
   ```

   **Option B:** Use hot-reload during development:
   ```bash
   dotnet watch run --project HabitFlow.Api
   # or
   dotnet watch run --project HabitFlow.Blazor
   ```

10. **Access the application:**
   - Blazor app: `https://localhost:7231` (or `http://localhost:5287`)
   - API: `https://localhost:7044` (or `http://localhost:5191`)
   - API documentation (Development mode): `https://localhost:7044/swagger`
   - SMTP inbox (to view sent emails): `http://localhost:3000`

### AI Notifications (Optional)

AI-generated notifications are optional. The application works without AI configuration and will use static templates as fallback. For local development, do not commit API keys.

**Set API key via user-secrets:**
```bash
cd HabitFlow.Api
dotnet user-secrets set "LlmSettings:ApiKey" "sk-..."
dotnet user-secrets set "LlmSettings:Enabled" "true"
```

**Or set via environment variables (PowerShell):**
```powershell
$env:LlmSettings__ApiKey="sk-..."
$env:LlmSettings__Enabled="true"
```

**Cost guard (MVP):**
- `LlmSettings:MaxDailyRequests` (default: 50) limits AI calls per daily job run
- After the limit, the system automatically falls back to static templates
- `NotificationSettings:AiNotificationsPerUserPerDay` (default: 3) limits AI notifications per user

## Available Scripts

- **Restore dependencies:**
  ```bash
  dotnet restore
  ```

- **Build the solution:**
  ```bash
  dotnet build
  ```

- **Run the API:**
  ```bash
  dotnet run --project HabitFlow.Api
  ```

- **Run the Blazor app:**
  ```bash
  dotnet run --project HabitFlow.Blazor
  ```

- **Hot-reload during development:**
  ```bash
  dotnet watch run --project <ProjectDir>
  ```

- **Run tests:**
  ```bash
  dotnet test
  ```

- **Run tests with coverage:**
  ```bash
  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
  ```

- **Start Docker infrastructure (SQL Server + SMTP):**
  ```bash
  cd .docker
  docker-compose up -d
  ```

- **Stop Docker infrastructure:**
  ```bash
  cd .docker
  docker-compose down
  ```

- **Apply database migrations:**
  ```bash
  dotnet ef database update --project HabitFlow.Api
  ```

- **Create a new migration:**
  ```bash
  dotnet ef migrations add <MigrationName> --project HabitFlow.Api
  ```

- **Format code:**
  ```bash
  dotnet format
  ```

- **Run E2E tests (requires Docker and built projects):**
  ```powershell
  .\run-e2e.ps1
  ```

## Project Scope

### MVP Features (MUST)

**Authentication & Authorization:**
- User registration with email verification (one-time link)
- Login with password authentication (minimum 8 characters)
- Password reset via email
- Secure session management for user resources

**Habit Management:**
- Create, read, update, and delete habits (CRUD)
- Two habit types: "start" (begin doing) and "stop" (stop doing)
- Habit properties: title (≤80 chars), description (≤280 chars), schedule (days of week), repetitions per day (1-100), optional deadline
- Maximum 20 habits per user

**Daily Check-ins:**
- One-time daily entry per habit
- For "start" habits: record number completed (0 to max repetitions)
- For "stop" habits: record violations (0 to max repetitions)
- Backfill capability up to 7 days
- No editing after submission

**"Today" Screen:**
- List of today's scheduled habit steps
- Quick access to check-in for each habit

**Read-only Calendar:**
- Future days: show planned schedule (neutral)
- Past days: color-coded by performance (green = completed, red = missed, partial completion supported)

**Progress Tracking:**
- Success rate is based on a per-day "daily_score"
  - Binary: daily_score = 1 if done, else 0
  - Quantitative/Checklist: ratio = Actual/Target clamped to [0,1]
  - Start habits: daily_score = ratio; Stop habits: daily_score = 1 - ratio
- Rolling window (7/30 days): success_rate = sum(daily_score in window) / planned_days_in_window; non-planned days are excluded
- If no planned days in window, success_rate = 0 (MVP convention)
- 75% success threshold for habits with deadlines
- Rolling success rate chart (7-day and 30-day windows with toggle)
- Tooltip shows sum(daily_score) and planned_days for the selected window

**AI-Powered Notifications:**
- In-app notification tab with message list
- Trigger: "miss due" (scheduled day not completed)
- One notification per habit per missed day
- Context-aware AI-generated content with fallback to static templates
- No email/SMS/push notifications outside the app

**Data Management:**
- Hard delete for habits and user accounts
- Secure access to user-owned resources only

### Out of Scope (Post-MVP)

- External integrations (Google Calendar, CSV export)
- Advanced visualizations and charts
- Email/SMS/push notifications
- Achievement system, rewards, gamification
- Advanced notification rate-limiting and personalization
- Account lockout after multiple failed login attempts
- Habit pausing functionality
- Operational backup/restore features

## Project Status

**Current Phase:** MVP Development

**Completed:**
- ✅ Initial project setup
- ✅ Solution structure (Backend and Frontend)
- ✅ Project documentation (PRD, Tech Stack, Agents)
- ✅ Authentication and authorization (registration, login, email verification, password reset)
- ✅ Database schema and Entity Framework setup
- ✅ Core habit CRUD operations (create, read, update, delete)
- ✅ Daily check-in functionality
- ✅ Calendar and progress visualization
- ✅ AI notification system with Hangfire background jobs
- ✅ Unit tests for business logic and validators
- ✅ Integration tests with TestContainers
- ✅ Component tests with bUnit
- ✅ End-to-end tests with Playwright
- ✅ CI/CD pipeline with GitHub Actions

**Definition of Done for MVP:**
- ✅ All MUST features implemented and tested
- ✅ Business logic covered by unit tests (≥80% coverage for Core layer)
- ✅ End-to-end test passing in CI/CD (registration → habit creation → check-in → calendar/chart → notification)
- ✅ GitHub Actions workflow configured

## License

This project is currently under development as an MVP. License information will be added upon public release.

---

**Contributions:** This is currently a solo development project. Contribution guidelines will be established after MVP completion.

**Feedback & Issues:** Please report issues or suggestions via GitHub Issues.

**Documentation:**
- Product Requirements Document (PRD): `.ai/prd.md`
- Tech Stack: `.ai/tech-stack.md`
- Agent Guidelines: `AGENTS.md` and `CLAUDE.md`
- Test Plan: `.ai/test-plan.md`
