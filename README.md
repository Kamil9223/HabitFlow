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
- **Background job scheduler** (e.g., Hangfire or Quartz) for triggering "miss due" notifications

### CI/CD & Quality
- **GitHub Actions** for continuous integration and deployment (build, tests, migrations)
- **Unit tests** for success rate calculations and business logic
- **End-to-end tests** for critical user paths (registration → habit creation → check-in → calendar/chart → notification)

## Getting Started Locally

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server 2022](https://www.microsoft.com/sql-server/sql-server-downloads) or SQL Server Express
- Git

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/HabitFlow.git
   cd HabitFlow
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure connection strings:**
   - Update `appsettings.Development.json` in both `HabitFlow.Api/` and `HabitFlow.Blazor/` with your SQL Server connection string
   - For sensitive data, use `dotnet user-secrets`:
     ```bash
     cd HabitFlow.Api
     dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
     ```

4. **Apply database migrations:**
   ```bash
   dotnet ef database update --project HabitFlow.Api
   ```

5. **Trust the HTTPS development certificate:**
   ```bash
   dotnet dev-certs https --trust
   ```

6. **Run the application:**

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

7. **Access the application:**
   - Blazor app: `https://localhost:5001` (or port specified in launch settings)
   - API documentation (Development mode): `https://localhost:7001/swagger` (or port specified in launch settings)

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

- **Apply database migrations:**
  ```bash
  dotnet ef database update --project HabitFlow.Api
  ```

- **Format code:**
  ```bash
  dotnet format
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
- Success rate calculation: completed/scheduled (or 0 if no scheduled days)
- For "stop" habits: daily contribution = 1 - violations/scheduled
- 75% success threshold for habits with deadlines
- Rolling success rate chart (7-day and 30-day windows with toggle)
- Tooltip showing completed/scheduled in the selected window

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

**Current Phase:** MVP Development (Sprint 1 of 2)

**Completed:**
- ✅ Initial project setup
- ✅ Solution structure (Backend and Frontend)
- ✅ Project documentation (PRD, Tech Stack, Agents)

**In Progress:**
- 🚧 Authentication and authorization implementation
- 🚧 Database schema and Entity Framework setup
- 🚧 Core habit CRUD operations

**Upcoming:**
- ⏳ Daily check-in functionality
- ⏳ Calendar and progress visualization
- ⏳ AI notification system
- ⏳ End-to-end testing
- ⏳ CI/CD pipeline setup

**Definition of Done for MVP:**
- All MUST features implemented and tested
- At least one business logic function covered by unit tests
- End-to-end test passing in CI/CD (registration → habit creation → check-in → calendar/chart → notification)
- GitHub Actions workflow configured

**Timeline:**
- Sprint 1: 2 weeks (Authentication, CRUD, basic check-ins)
- Sprint 2: 2 weeks (Calendar, charts, notifications, testing, deployment)

## License

This project is currently under development as an MVP. License information will be added upon public release.

---

**Contributions:** This is currently a solo development project. Contribution guidelines will be established after MVP completion.

**Feedback & Issues:** Please report issues or suggestions via the [GitHub Issues](https://github.com/yourusername/HabitFlow/issues) page.

**Documentation:** Detailed PRD and technical specifications available in the `.ai/` directory.