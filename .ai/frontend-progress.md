# Frontend - Status implementacji

**Data aktualizacji:** 2026-01-17 (19:30)
**Dokument referencyjny:** `.ai/ui-plan.md`, `.ai/prd.md`

## Przegląd ogólny

Aplikacja Blazor Server jest w trakcie implementacji. Widoki autoryzacji i podstawowe widoki biznesowe (Today, Profile) są gotowe. Brakuje kluczowych widoków do zarządzania nawykami (Habits) oraz powiadomień (Notifications).

---

## ✅ Widoki zaimplementowane

### 1. Auth Views (100% zgodności z ui-plan.md)

#### `/auth/register` - Rejestracja
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `Register.razor`, `AuthLayout`
- **Funkcje:**
  - Walidacja client-side (email, hasło ≥8 znaków, wielka/mała litera, cyfra)
  - Pole DisplayName (opcjonalne)
  - Obsługa błędów 400/409/422
  - Przekierowanie do `/auth/login?registered=true`
- **API:** `POST /api/v1/auth/register`
- **Powiązane wymagania:** F-001, US-001, US-021

#### `/auth/login` - Logowanie
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `Login.razor`, `AuthLayout`
- **Funkcje:**
  - Walidacja email/hasło
  - Obsługa 401 (błędne dane), 403 (niezweryfikowany email)
  - Link do forgot-password
  - Przekierowanie do `/today` po sukcesie
- **API:** `POST /api/v1/auth/login`
- **Powiązane wymagania:** F-001, US-002, US-021

#### `/auth/confirm-email` - Potwierdzenie email
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `ConfirmEmail.razor`, `AuthLayout`
- **Funkcje:**
  - Obsługa linku potwierdzającego
  - Komunikat sukcesu/błędu
  - CTA do logowania
- **API:** `POST /api/v1/auth/confirm-email`
- **Powiązane wymagania:** US-001

#### `/auth/forgot-password` - Zapomniane hasło
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `ForgotPassword.razor`, `AuthLayout`
- **Funkcje:**
  - Pole email
  - Komunikat sukcesu (nie ujawnia czy email istnieje)
- **API:** `POST /api/v1/auth/forgot-password`
- **Powiązane wymagania:** US-003

#### `/auth/reset-password` - Reset hasła
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `ResetPassword.razor`, `AuthLayout`
- **Funkcje:**
  - Pola: email, token, nowe hasło
  - Walidacja hasła
  - CTA do logowania po sukcesie
- **API:** `POST /api/v1/auth/reset-password`
- **Powiązane wymagania:** US-003

#### `/auth/logout` - Wylogowanie
- **Status:** ✅ Zaimplementowane
- **Komponenty:** `Logout.razor`
- **Funkcje:**
  - Zakończenie sesji
  - Przekierowanie do logowania
- **API:** N/A (sesyjne)
- **Powiązane wymagania:** US-020

---

### 2. Today View (90% zgodności)

#### `/today` - Ekran dzisiejszych zadań
- **Status:** ✅ Zaimplementowane (z drobnymi brakami)
- **Komponenty:**
  - `Today.razor` (strona główna)
  - `TodayProgressHeader.razor` (nagłówek X/Y)
  - `TodayChecklist.razor` (lista itemów)
  - `TodayChecklistItem.razor` (pojedynczy item)
  - `CheckinDialog.razor` (modal check-in)
  - `EmptyStateCard.razor` (puste stany)
  - `RefreshButton.razor` (odświeżanie)
- **Funkcje:**
  - Lista dzisiejszych kroków z harmonogramem
  - Loading states (global spinner)
  - Empty state z CTA "Dodaj swój pierwszy nawyk" → `/habits/new` ⚠️
  - Error handling z retry
  - Licznik postępu X/Y completed
  - Check-in przez modal (Binary i Quantitative)
  - Optymistyczna aktualizacja UI
  - Obsługa błędów 400/401/403/404/409/422
- **API:**
  - `GET /api/v1/today`
  - `POST /api/v1/habits/{id}/checkins`
- **Powiązane wymagania:** F-004, F-005, F-012, US-010, US-011, US-012, US-025

**⚠️ Braki:**
- CheckinDialog nie ma date pickera do backfill (uzupełnianie do 7 dni wstecz) - wymagane przez US-013
- Empty state linkuje do `/habits/new` (strona nie istnieje) zamiast otwierać HabitFormDialog

---

### 3. Profile View (100% zgodności)

#### `/profile` - Profil użytkownika
- **Status:** ✅ Zaimplementowane
- **Komponenty:**
  - `Profile.razor` (strona główna)
  - `ProfileSummary.razor` (podsumowanie: email, createdAt, emailConfirmed, habitsCount)
  - `TimeZoneEditor.razor` (edycja strefy czasowej)
  - `DeleteAccountSection.razor` (sekcja usuwania konta)
  - `ConfirmDeleteAccountDialog.razor` (potwierdzenie usunięcia)
- **Funkcje:**
  - Wyświetlanie danych profilu
  - Zmiana timeZoneId z komunikatem o wejściu od następnej doby
  - Hard delete konta z potwierdzeniem (tekst "DELETE")
  - Obsługa błędów 400/401/422
- **API:**
  - `GET /api/v1/profile`
  - `PATCH /api/v1/profile/timezone`
  - `DELETE /api/v1/profile`
- **Powiązane wymagania:** F-002, F-010, US-004, US-019

---

### 4. Layout & Navigation (100% zgodności) ✅ **ZAKOŃCZONE 2026-01-17**

#### `MainLayout.razor`
- **Status:** ✅ Zaimplementowane
- **Funkcje:**
  - Sidebar z NavMenu (desktop) ✅
  - Top bar z linkiem "About" i przyciskiem Wyloguj/Zaloguj ✅
  - **NotificationsBell w top bar** ✅ (2026-01-17)
  - **Responsive bottom-nav dla mobile (≤768px)** ✅ (2026-01-17)
  - MudBlazor providers (Theme, Dialog, Snackbar, Popover) ✅
  - ErrorBoundary (#blazor-error-ui) ✅
  - Ukrycie sidebara na mobile ✅ (2026-01-17)
- **⚠️ Braki (niski priorytet):**
  - Brak globalnego loadera (GlobalProgressBar)
  - Brak bannera reconnect (SignalR disconnect)

#### `NavMenu.razor`
- **Status:** ✅ Zaimplementowane
- **Linki obecne:**
  - Dziś (/today) ✅
  - **Nawyki (/habits)** ✅ (2026-01-17)
  - **Powiadomienia (/notifications)** ✅ (2026-01-17)
  - Profil (/profile) ✅
- **Zmiany:** Usunięto niepotrzebny link "Home" ✅ (2026-01-17)

#### `BottomNav.razor` - **NOWY komponent** ✅
- **Status:** ✅ Zaimplementowane (2026-01-17)
- **Lokalizacja:** `Components/Shared/BottomNav.razor`
- **Funkcje:**
  - Custom bottom navigation dla mobile
  - 4 przyciski: Dziś, Nawyki, Powiadomienia, Profil
  - Aktywna zakładka podświetlana
  - Badge na ikonie Powiadomień (gdy unreadCount > 0)
  - Widoczny tylko dla zalogowanych użytkowników
  - Responsywny (ukryty >768px, widoczny ≤768px)
  - Dedicated CSS z animacjami i hover states

#### `NotificationsBell.razor` - **NOWY komponent** ✅
- **Status:** ✅ Zaimplementowane (2026-01-17)
- **Lokalizacja:** `Components/Shared/NotificationsBell.razor`
- **Funkcje:**
  - Ikona dzwonka w top bar
  - Badge z licznikiem nieprzeczytanych (TODO: API integration)
  - Link do `/notifications`
  - Widoczny tylko dla zalogowanych użytkowników

#### Strony zastępcze - **NOWE** ✅
- **`/habits`** ✅ (2026-01-17) - Strona "w budowie" w `Pages/Habits/Habits.razor`
- **`/notifications`** ✅ (2026-01-17) - Strona "w budowie" w `Pages/Notifications/Notifications.razor`

#### `AuthLayout.razor`
- **Status:** ✅ Zaimplementowane
- **Funkcje:**
  - Top bar z logo i przyciskami Login/Register
  - Brak sidebara (poprawnie dla auth views)
  - MudBlazor providers

---

## ❌ Widoki BRAKUJĄCE (wymagane przez ui-plan.md i PRD)

### 1. Landing/Root (`/`) - **PRIORYTET: ŚREDNI**

- **Status:** ❌ Obecnie wyświetla "Hello, world!"
- **Wymagane:**
  - Redirect na `/today` dla zalogowanych użytkowników
  - Redirect na `/auth/login` dla niezalogowanych
  - `AppShellRouter` / `AuthRedirectGuard` (logika przekierowania)
- **API:** N/A (tylko logika routing)
- **Powiązane wymagania:** F-001, US-002, US-020
- **Komponenty do utworzenia:** `Home.razor` (refaktoring)

---

### 2. Habits - Lista (`/habits`) - **PRIORYTET: KRYTYCZNY**

- **Status:** ⚠️ Strona zastępcza istnieje (2026-01-17)
- **Funkcje wymagane:**
  - Lista wszystkich nawyków użytkownika
  - Wyświetlanie: tytuł, typ (Start/Stop), harmonogram, skrócony success_rate, deadline, licznik N/20
  - Filtrowanie i paginacja (opcjonalnie)
  - Akcje: Create (FAB/button), Edit, Delete, Quick Check-in, View Details
  - Komunikat o limicie 20 nawyków (F-012)
  - Empty state z CTA "Utwórz pierwszy nawyk"
  - Obsługa błędów 401/403/409 (limit)
- **API:**
  - `GET /api/v1/habits` (lista)
  - `POST /api/v1/habits` (tworzenie)
  - `PATCH /api/v1/habits/{id}` (edycja)
  - `DELETE /api/v1/habits/{id}` (usuwanie)
  - `POST /api/v1/habits/{id}/checkins` (quick check-in)
- **Powiązane wymagania:** F-003, F-012, US-005, US-006, US-007, US-008, US-009, US-021
- **Komponenty do utworzenia:**
  - `Habits.razor` (strona główna)
  - `HabitList.razor` (lista kart)
  - `HabitItem.razor` (pojedyncza karta nawyku)
  - `HabitFormDialog.razor` (modal tworzenia/edycji)
  - `ConfirmDialog.razor` (potwierdzenie usunięcia)
  - Reużycie: `CheckinDialog.razor` (z Today)

**HabitFormDialog - szczegóły:**
- Sekcje: Podstawy (tytuł, typ, opis), Harmonogram (dni tygodnia, targetValue, targetUnit), Deadline (opcjonalny)
- Walidacje: tytuł ≤80, opis ≤280, targetValue 1..100, CompletionMode (Binary/Quantitative)
- Dwa tryby: Create i Edit

---

### 3. Habit Details (`/habits/{id}`) - **PRIORYTET: KRYTYCZNY**

- **Status:** ❌ Całkowicie brak
- **Funkcje wymagane:**
  - Szczegóły nawyku: typ, tryb (Binary/Quantitative), harmonogram, targetValue/unit, deadline, success_rate
  - Dropdown do przełączania między nawykami (HabitSwitchDropdown)
  - Taby: Calendar (readonly kalendarz statusów), Progress (wykres rolling 7/30)
  - Kalendarz:
    - Przyszłość: neutralny (plan)
    - Przeszłość: zielony (wykonane), czerwony (niewykonane), pomarańczowy (częściowo)
    - Tooltips: data, ActualValue/TargetValue, status
    - Readonly (zmiany tylko przez CRUD i check-in)
  - Wykres rolling success rate:
    - Przełącznik 7/30 dni
    - Tooltip: wykonane/zaplanowane w oknie
    - Linia trendu
  - Obsługa błędów 404 (cudzy/nieistniejący zasób)
- **API:**
  - `GET /api/v1/habits/{id}` (szczegóły nawyku)
  - `GET /api/v1/habits/{id}/calendar` (dane kalendarza)
  - `GET /api/v1/habits/{id}/progress/rolling` (dane wykresu 7/30)
- **Powiązane wymagania:** F-006, F-007, F-008, US-015, US-016, US-024
- **Komponenty do utworzenia:**
  - `HabitDetails.razor` (strona główna)
  - `HabitDetailsHeader.razor` (nagłówek z tytułem, success_rate, deadline)
  - `HabitSwitchDropdown.razor` (przełącznik nawyków)
  - `TabCalendar.razor` (zakładka kalendarza)
  - `TabProgress.razor` (zakładka wykresu)
  - `CalendarView.razor` (komponent kalendarza readonly)
  - `RollingSuccessChart.razor` (wykres z przełącznikiem 7/30)

**CalendarView - szczegóły:**
- Grid 7 kolumn (dni tygodnia)
- Kolory wsparty ikonami/tekstami dla dostępności
- Tooltips z datą, wartościami i statusem
- Responsywny dla mobile

**RollingSuccessChart - szczegóły:**
- Biblioteka wykresów: ApexCharts, Chart.js lub Plotly.Blazor
- Przełącznik 7/30 dni
- Oś X: data, Oś Y: success_rate (0-100%)
- Tooltip: "Wykonane X / Zaplanowane Y w oknie"

---

### 4. Notifications (`/notifications`) - **PRIORYTET: WYSOKI**

- **Status:** ⚠️ Strona zastępcza istnieje (2026-01-17)
- **Funkcje wymagane:**
  - Lista powiadomień AI (miss due)
  - Wyświetlanie: tytuł nawyku, data, treść, aiStatus (generated/fallback)
  - Liczba nowych powiadomień (badge)
  - Paginacja (zamiast infinite scroll)
  - Sortowanie chronologiczne (najnowsze pierwsze)
  - Obsługa błędów 401
  - Empty state: "Brak powiadomień"
- **API:**
  - `GET /api/v1/notifications` (lista z paginacją)
  - Opcjonalnie: `PATCH /api/v1/notifications/{id}/mark-read` (oznaczanie jako przeczytane)
- **Powiązane wymagania:** F-009, F-011, US-017, US-018, US-022
- **Komponenty do utworzenia:**
  - `Notifications.razor` (strona główna)
  - `NotificationsList.razor` (lista z paginacją)
  - `NotificationItem.razor` (pojedyncze powiadomienie)
  - `NotificationsBell.razor` (ikona dzwonka w top bar z licznikiem)
  - `Pagination.razor` (komponent stronicowania)

**NotificationsBell - integracja:**
- Dodać do `MainLayout.razor` w top bar
- Licznik nowych (badge): `GET /api/v1/notifications?unreadOnly=true&limit=1` (count)
- Kliknięcie → redirect do `/notifications`

---

### 5. Error Boundary & 404 - **PRIORYTET: ŚREDNI**

- **Status:** ❌ Częściowo (podstawowy `Error.razor` + `#blazor-error-ui`)
- **Funkcje wymagane:**
  - Globalny ErrorBoundary dla aplikacji
  - Dedykowana strona 404 z przyjaznym komunikatem
  - Obsługa 5xx z komunikatem "Spróbuj ponownie później"
  - CTA do `/today` lub "Wróć do strony głównej"
  - Brak ujawniania szczegółów technicznych
- **API:** N/A (tylko UI)
- **Powiązane wymagania:** US-021, US-023
- **Komponenty do utworzenia:**
  - `ErrorBoundary.razor` (komponent globalny w `App.razor`)
  - `ErrorView.razor` (widok błędu)
  - `NotFound.razor` (dedykowana strona 404)
  - `RetryButton.razor` (przycisk retry z loading state)

---

### 6. Email Confirmation Gate - **PRIORYTET: WYSOKI**

- **Status:** ❌ Całkowicie brak
- **Funkcje wymagane:**
  - Globalny banner/modal w App Shell po zalogowaniu
  - Wyświetlany gdy `emailConfirmed=false` (z GET /api/v1/profile)
  - Komunikat: "Potwierdź swój email, aby korzystać ze wszystkich funkcji"
  - CTA: "Wyślij ponownie email" (opcjonalnie)
  - Blokowanie kluczowych akcji: tworzenie nawyków, check-in
  - Nie blokować: przeglądanie profilu, wylogowanie
- **API:**
  - `GET /api/v1/profile` (sprawdzenie emailConfirmed)
  - Opcjonalnie: `POST /api/v1/auth/resend-confirmation` (ponowne wysłanie)
- **Powiązane wymagania:** US-001, US-002
- **Komponenty do utworzenia:**
  - `EmailConfirmationGate.razor` (logika warunkowego wyświetlania)
  - `AlertBanner.razor` (banner ostrzegawczy w MainLayout)
  - Integracja w `MainLayout.razor` lub `App.razor`

---

## 📊 Podsumowanie pokrycia

| Kategoria | Zaimplementowane | Brakujące | Pokrycie | Status |
|-----------|-----------------|-----------|----------|--------|
| **Auth Views** | 6/6 | 0 | **100%** | ✅ Gotowe |
| **Business Views** | 2/5 | 3 (Habits Lista, Details, Notifications) | **40%** | ⚠️ W trakcie |
| **Layout & Navigation** | **Kompletne** ✅ | Global banners (niski priorytet) | **~95%** | ✅ **Gotowe (2026-01-17)** |
| **Komponenty Today** | 6/6 | Date picker w CheckinDialog (backfill) | **~90%** | ✅ Prawie gotowe |
| **Komponenty Profile** | 4/4 | 0 | **100%** | ✅ Gotowe |
| **Komponenty Habits** | 1/7+ (strona zastępcza) | List, Item, FormDialog, CalendarView, Chart, ConfirmDialog | **~10%** | ❌ Brak |
| **Komponenty Notifications** | 2/4+ (strona + Bell) | List, Item, Pagination | **~40%** | ⚠️ Częściowo |
| **Error Handling** | Podstawowy | ErrorBoundary, ErrorView, NotFound dedicated | **~30%** | ❌ Brak |

**Ogólne pokrycie MVP:** ~55% (↑ +5% po Layout & Navigation)

---

## 🚨 Priorytety implementacji (rekomendacja)

### ✅ Zakończone (2026-01-17)
- ~~**NavMenu update**~~ - dodano linki "Nawyki" i "Powiadomienia" ✅
- ~~**Strony zastępcze**~~ - `/habits` i `/notifications` ✅
- ~~**NotificationsBell**~~ - ikona dzwonka w top bar ✅
- ~~**BottomNav**~~ - responsive navigation dla mobile ✅
- ~~**Refaktoring struktury folderów**~~ - feature-based organization ✅

### Sprint 1 - Krytyczne (Habits CRUD)
1. **`/habits` (lista)** - podstawowa funkcjonalność CRUD
   - HabitList, HabitItem, HabitFormDialog, ConfirmDialog
   - API: GET/POST/PATCH/DELETE /api/v1/habits
2. **Landing redirect (`/`)** - UX onboarding
   - AuthRedirectGuard logic

### Sprint 2 - Wysokie (Visualizations & Notifications)
4. **`/habits/{id}` (detale)** - kalendarz readonly i wykres rolling 7/30
   - CalendarView, RollingSuccessChart, HabitDetailsHeader
   - API: GET /api/v1/habits/{id}, /calendar, /progress/rolling
5. **`/notifications`** - powiadomienia AI (F-009 z PRD)
   - NotificationsList, NotificationItem, NotificationsBell
   - API: GET /api/v1/notifications
6. **EmailConfirmationGate** - blokada niepotwierdzonego email (US-001)

### Sprint 3 - Średnie (UX Polish)
7. **Backfill date picker** w CheckinDialog - uzupełnianie historii (US-013)
8. **Mobile bottom-nav** - dostępność na urządzeniach mobilnych
9. **Error Boundary & 404** - dedykowane widoki błędów
10. **Global banners** - reconnect, loading, info

---

## 📝 Notatki implementacyjne

### Biblioteki do rozważenia:
- **Wykresy:** ApexCharts.Blazor, Plotly.Blazor, lub Blazor.FluentUI (Charts)
- **Kalendarz:** Custom z MudBlazor Grid lub FluentUI Calendar
- **Date picker:** MudBlazor DatePicker (już używane)

### Wzorce architektoniczne (zgodnie z ui-plan.md):
- **State management:** Scoped services `*State` (per SignalR circuit) + `*Service` (API calls)
- **Modale:** MudBlazor DialogService (już używane)
- **Loading states:** Globalne (top bar progress) + lokalne (spinner w komponentach)
- **Error handling:** ProblemDetails → snackbary + inline errors w formularzach

### Konwencje nazewnictwa (zgodnie z AGENTS.md):
- **Feature-based organization** (od 2026-01-17):
  - Strony i komponenty: `Components/Pages/{Feature}/{ComponentName}.razor`
  - Shared: `Components/Shared/{ComponentName}.razor`
  - Layout: `Components/Layout/{LayoutName}.razor`
- **Namespace:** Dodaj `@using HabitFlow.Blazor.Components.Pages.{Feature}` w `_Imports.razor`
- **Zasada:** Komponenty które się zmieniają razem, są przechowywane razem

---

## 🔗 Powiązane dokumenty

- **ui-plan.md** - szczegółowa architektura UI, mapa widoków, UX
- **prd.md** - wymagania funkcjonalne i user stories
- **test-plan.md** - strategia testowania komponentów (bUnit)
- **AGENTS.md** - konwencje projektu, styl kodowania, struktura folderów Blazor
- **CLAUDE.md** - główny plik konfiguracyjny (referencja do AGENTS.md)

---

## 📝 Historia zmian

### 2026-01-17 (19:30) - Layout & Navigation + Refaktoring struktury
**Implementacja punktu 4 z dokumentu + refaktoring folderów:**

**Nowe komponenty:**
- `Components/Shared/NotificationsBell.razor` - Ikona dzwonka w top bar z badge
- `Components/Shared/BottomNav.razor` + CSS - Responsive bottom navigation dla mobile
- `Components/Pages/Habits/Habits.razor` - Strona zastępcza "w budowie"
- `Components/Pages/Notifications/Notifications.razor` - Strona zastępcza "w budowie"

**Zmiany w istniejących komponentach:**
- `NavMenu.razor` - Usunięto "Home", dodano "Nawyki" i "Powiadomienia"
- `MainLayout.razor` - Dodano NotificationsBell i BottomNav, ukrycie sidebara na mobile
- `MainLayout.razor.css` - Media queries dla mobile (≤768px)
- `_Imports.razor` - Dodano namespace dla Pages/Today, Pages/Habits, Pages/Notifications

**Refaktoring struktury folderów (feature-based organization):**
- Przeniesiono `Components/Today/*` → `Pages/Today/`
- Przeniesiono `Pages/Today.razor` → `Pages/Today/Today.razor`
- Przeniesiono `Pages/Habits.razor` → `Pages/Habits/Habits.razor`
- Przeniesiono `Pages/Notifications.razor` → `Pages/Notifications/Notifications.razor`
- Zaktualizowano using statements w `Today.razor`
- Usunięto pusty folder `Components/Today/`

**Dokumentacja:**
- Zaktualizowano `AGENTS.md` - dodano sekcję "Struktura folderów Blazor"
- Przywrócono `CLAUDE.md` do stanu `@file:AGENTS.md`

**Rezultat:**
- Build: ✅ Sukces (0 błędów)
- Pokrycie Layout & Navigation: 60% → 95% ✅
- Ogólne pokrycie MVP: 50% → 55% ↑
- Struktura folderów: spójna, feature-based, skalowalna ✅

---

**Ostatnia aktualizacja:** 2026-01-17 (19:30)
**Autor analizy:** Claude (agent AI)
