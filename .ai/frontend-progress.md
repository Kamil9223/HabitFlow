# Frontend - Status implementacji

**Data aktualizacji:** 2026-01-19 (00:01)
**Dokument referencyjny:** `.ai/ui-plan.md`, `.ai/prd.md`, `.ai/api-plan.md`

## Przeglad ogolny

Aplikacja Blazor Server jest w trakcie implementacji. Widoki autoryzacji oraz podstawowe widoki biznesowe (Today, Profile) sa gotowe. Widok listy nawykow (Habits) jest zaimplementowany z CRUD. Brakuje widoku detali nawyku oraz powiadomien (Notifications).

---

## Widoki zaimplementowane

### 1. Auth Views (100% zgodnosci z ui-plan.md)

#### `/auth/register` - Rejestracja
- **Status:** Zaimplementowane
- **Komponenty:** `Register.razor`, `AuthLayout`
- **Funkcje:**
  - Walidacja client-side (email, haslo >=8 znakow, wielka/mala litera, cyfra)
  - Pole DisplayName (opcjonalne)
  - Obsluga bledow 400/409/422
  - Przekierowanie do `/auth/login?registered=true`
- **API:** `POST /api/v1/auth/register`
- **Powiazane wymagania:** F-001, US-001, US-021

#### `/auth/login` - Logowanie
- **Status:** Zaimplementowane
- **Komponenty:** `Login.razor`, `AuthLayout`
- **Funkcje:**
  - Walidacja email/haslo
  - Obsluga 401 (bledne dane), 403 (niezweryfikowany email)
  - Link do forgot-password
  - Przekierowanie do `/today` po sukcesie
- **API:** `POST /api/v1/auth/login`
- **Powiazane wymagania:** F-001, US-002, US-021

#### `/auth/confirm-email` - Potwierdzenie email
- **Status:** Zaimplementowane
- **Komponenty:** `ConfirmEmail.razor`, `AuthLayout`
- **Funkcje:**
  - Obsluga linku potwierdzajacego
  - Komunikat sukcesu/bledu
  - CTA do logowania
- **API:** `POST /api/v1/auth/confirm-email`
- **Powiazane wymagania:** US-001

#### `/auth/forgot-password` - Zapomniane haslo
- **Status:** Zaimplementowane
- **Komponenty:** `ForgotPassword.razor`, `AuthLayout`
- **Funkcje:**
  - Pole email
  - Komunikat sukcesu (nie ujawnia czy email istnieje)
- **API:** `POST /api/v1/auth/forgot-password`
- **Powiazane wymagania:** US-003

#### `/auth/reset-password` - Reset hasla
- **Status:** Zaimplementowane
- **Komponenty:** `ResetPassword.razor`, `AuthLayout`
- **Funkcje:**
  - Pola: email, token, nowe haslo
  - Walidacja hasla
  - CTA do logowania po sukcesie
- **API:** `POST /api/v1/auth/reset-password`
- **Powiazane wymagania:** US-003

#### `/auth/logout` - Wylogowanie
- **Status:** Zaimplementowane
- **Komponenty:** `Logout.razor`
- **Funkcje:**
  - Zakonczenie sesji
  - Przekierowanie do logowania
- **API:** N/A (sesyjne)
- **Powiazane wymagania:** US-020

---

### 2. Today View (95% zgodnosci)

#### `/today` - Ekran dzisiejszych zadan
- **Status:** Zaimplementowane (drobne braki)
- **Komponenty:**
  - `Today.razor` (strona glowna)
  - `TodayProgressHeader.razor` (naglowek X/Y)
  - `TodayChecklist.razor` (lista itemow)
  - `TodayChecklistItem.razor` (pojedynczy item)
  - `CheckinDialog.razor` (modal check-in)
  - `EmptyStateCard.razor` (puste stany)
  - `RefreshButton.razor` (odswiezanie)
- **Funkcje:**
  - Lista dzisiejszych krokow z harmonogramem
  - Loading states (global spinner)
  - Empty state z CTA (na razie button disabled)
  - Error handling z retry
  - Licznik postepu X/Y completed
  - Check-in przez modal (Binary i Quantitative)
  - Optymistyczna aktualizacja UI
  - Obsluga bledow 400/401/403/404/409/422
  - Date picker do backfill (7 dni wstecz)
- **API:**
  - `GET /api/v1/today`
  - `POST /api/v1/habits/{id}/checkins`
- **Powiazane wymagania:** F-004, F-005, F-012, US-010, US-011, US-012, US-025

**Braki:**
- Empty state ma wylaczony przycisk i TODO zamiast otwierania HabitFormDialog

---

### 3. Habits View (Lista) (90%+ zgodnosci)

#### `/habits` - Lista nawykow
- **Status:** Zaimplementowane
- **Komponenty:**
  - `Habits.razor`, `Habits.razor.cs`
  - `HabitList.razor`, `HabitItem.razor`
  - `HabitFilters.razor`, `HabitFormDialog.razor`
  - `ConfirmDialog.razor`, `EmptyState.razor`
- **Funkcje:**
  - Lista nawykow z harmonogramem, deadline i skroconym success_rate
  - Paginacja + wybor rozmiaru strony
  - Filtry: typ, tryb, status, szukaj, sortowanie
  - Akcje: Create, Edit, Delete, Quick Check-in
  - Limit 20 nawykow + alert o limicie
  - Empty state
  - Obsluga bledow 401/404/409/422
- **API:**
  - `GET /api/v1/habits` (lista)
  - `POST /api/v1/habits` (tworzenie)
  - `PATCH /api/v1/habits/{id}` (edycja)
  - `DELETE /api/v1/habits/{id}` (usuwanie)
  - `POST /api/v1/habits/{id}/checkins` (quick check-in)
  - `GET /api/v1/habits/{id}/progress/rolling` (success_rate)
- **Powiazane wymagania:** F-003, F-012, US-005, US-006, US-007, US-008, US-009, US-021

---

### 4. Profile View (100% zgodnosci)

#### `/profile` - Profil uzytkownika
- **Status:** Zaimplementowane
- **Komponenty:**
  - `Profile.razor` (strona glowna)
  - `ProfileSummary.razor` (podsumowanie: email, createdAt, emailConfirmed, habitsCount)
  - `TimeZoneEditor.razor` (edycja strefy czasowej)
  - `DeleteAccountSection.razor` (sekcja usuwania konta)
  - `ConfirmDeleteAccountDialog.razor` (potwierdzenie usuniecia)
- **Funkcje:**
  - Wyswietlanie danych profilu
  - Zmiana timeZoneId z komunikatem o wejsciu od nastepnej doby
  - Hard delete konta z potwierdzeniem (tekst "DELETE")
  - Obsluga bledow 400/401/422
- **API:**
  - `GET /api/v1/profile`
  - `PATCH /api/v1/profile/timezone`
  - `DELETE /api/v1/profile`
- **Powiazane wymagania:** F-002, F-010, US-004, US-019

---

### 5. Landing/Root (`/`) - ZAKONCZONE 2026-01-18

#### `Home.razor` - Przekierowanie root
- **Status:** Zaimplementowane
- **Lokalizacja:** `Components/Pages/Home.razor`
- **Funkcje:**
  - Sprawdzenie stanu autentykacji w OnInitializedAsync
  - Przekierowanie zalogowanych -> `/today` (replace: true)
  - Przekierowanie niezalogowanych -> `/auth/login` (replace: true)
  - Integracja z ApiAuthenticationStateProvider
- **API:** N/A (client-side routing)
- **Powiazane wymagania:** F-001, US-002, US-020

---

### 6. Layout & Navigation (100% zgodnosci) - ZAKONCZONE 2026-01-17

#### `MainLayout.razor`
- **Status:** Zaimplementowane
- **Funkcje:**
  - Sidebar z NavMenu (desktop)
  - Top bar z linkiem "About" i przyciskiem Wyloguj/Zaloguj
  - NotificationsBell w top bar
  - Responsive bottom-nav dla mobile (<=768px)
  - MudBlazor providers (Theme, Dialog, Snackbar, Popover)
  - ErrorBoundary (#blazor-error-ui)
  - Ukrycie sidebara na mobile
- **Braki (niski priorytet):**
  - Brak globalnego loadera (GlobalProgressBar)
  - Brak bannera reconnect (SignalR disconnect)

#### `NavMenu.razor`
- **Status:** Zaimplementowane
- **Linki obecne:**
  - Dzis (/today)
  - Nawyki (/habits)
  - Powiadomienia (/notifications)
  - Profil (/profile)
- **Zmiany:** Usunieto niepotrzebny link "Home"

#### `BottomNav.razor`
- **Status:** Zaimplementowane (2026-01-17)
- **Lokalizacja:** `Components/Shared/BottomNav.razor`
- **Funkcje:**
  - Custom bottom navigation dla mobile
  - 4 przyciski: Dzis, Nawyki, Powiadomienia, Profil
  - Aktywna zakladka podswietlana
  - Badge na ikonie Powiadomien (gdy unreadCount > 0)
  - Widoczny tylko dla zalogowanych uzytkownikow
  - Responsywny (ukryty >768px, widoczny <=768px)
  - Dedicated CSS z animacjami i hover states

#### `NotificationsBell.razor`
- **Status:** Zaimplementowane (2026-01-17)
- **Lokalizacja:** `Components/Shared/NotificationsBell.razor`
- **Funkcje:**
  - Ikona dzwonka w top bar
  - Badge z licznikiem nieprzeczytanych (TODO: API integration)
  - Link do `/notifications`
  - Widoczny tylko dla zalogowanych uzytkownikow

#### Strony zastepcze
- `/notifications` (2026-01-17) - strona "w budowie" w `Pages/Notifications/Notifications.razor`

---

## Widoki brakujace (wymagane przez ui-plan.md i PRD)

### 1. Habit Details (`/habits/{id}`) - PRIORYTET: KRYTYCZNY

- **Status:** Brak
- **Funkcje wymagane:**
  - Szczegoly nawyku: typ, tryb, harmonogram, targetValue/unit, deadline, success_rate
  - Dropdown do przelaczania miedzy nawykami (HabitSwitchDropdown)
  - Taby: Calendar (readonly), Progress (wykres rolling 7/30)
  - Kalendarz: statusy done/miss/partial + tooltips
  - Wykres rolling success rate z przelacznikiem 7/30
  - Obsluga bledow 404
- **API:**
  - `GET /api/v1/habits/{id}`
  - `GET /api/v1/habits/{id}/calendar`
  - `GET /api/v1/habits/{id}/progress/rolling`
- **Powiazane wymagania:** F-006, F-007, F-008, US-015, US-016, US-024
- **Komponenty do utworzenia:**
  - `HabitDetails.razor`
  - `HabitDetailsHeader.razor`
  - `HabitSwitchDropdown.razor`
  - `TabCalendar.razor`
  - `TabProgress.razor`
  - `CalendarView.razor`
  - `RollingSuccessChart.razor`

### 2. Notifications (`/notifications`) - PRIORYTET: WYSOKI

- **Status:** Strona zastepcza istnieje (2026-01-17)
- **Funkcje wymagane:**
  - Lista powiadomien AI (miss due)
  - Wyswietlanie: tytul nawyku, data, tresc, aiStatus
  - Liczba nowych powiadomien (badge)
  - Paginacja (zamiast infinite scroll)
  - Sortowanie chronologiczne (najnowsze pierwsze)
  - Obsluga bledow 401
  - Empty state: "Brak powiadomien"
- **API:**
  - `GET /api/v1/notifications` (lista z paginacja)
  - Opcjonalnie: `PATCH /api/v1/notifications/{id}/mark-read`
- **Powiazane wymagania:** F-009, F-011, US-017, US-018, US-022
- **Komponenty do utworzenia:**
  - `Notifications.razor`
  - `NotificationsList.razor`
  - `NotificationItem.razor`
  - `Pagination.razor`

### 3. Error Boundary & 404 - PRIORYTET: SREDNI

- **Status:** Czesciowo (podstawowy `Error.razor` + `#blazor-error-ui`)
- **Funkcje wymagane:**
  - Globalny ErrorBoundary
  - Dedykowana strona 404
  - Obsluga 5xx z komunikatem "Sprobuj ponownie pozniej"
  - CTA do `/today` lub "Wroc do strony glownej"
  - Brak ujawniania szczegolow technicznych
- **Komponenty do utworzenia:**
  - `ErrorBoundary.razor`
  - `ErrorView.razor`
  - `NotFound.razor`
  - `RetryButton.razor`

### 4. Email Confirmation Gate - PRIORYTET: WYSOKI

- **Status:** Brak
- **Funkcje wymagane:**
  - Globalny banner/modal po zalogowaniu
  - Widoczny gdy `emailConfirmed=false`
  - CTA: "Wyslij ponownie email" (opcjonalnie)
  - Blokowanie tworzenia nawykow i check-in
  - Nie blokowac: profil, wylogowanie
- **API:**
  - `GET /api/v1/profile`
  - Opcjonalnie: `POST /api/v1/auth/resend-confirmation`
- **Komponenty do utworzenia:**
  - `EmailConfirmationGate.razor`
  - `AlertBanner.razor`
  - Integracja w `MainLayout.razor` lub `App.razor`

---

## Podsumowanie pokrycia

| Kategoria | Zaimplementowane | Brakujace | Pokrycie | Status |
|-----------|-----------------|-----------|----------|--------|
| **Auth Views** | 6/6 | 0 | **100%** | Gotowe |
| **Business Views** | 3/5 | 2 (Habits Details, Notifications) | **60%** | W trakcie |
| **Layout & Navigation** | Kompletne | Global banners (niski priorytet) | **~95%** | Gotowe |
| **Landing/Root Redirect** | 1/1 | 0 | **100%** | Gotowe |
| **Komponenty Today** | 7/7 | 0 | **100%** | Gotowe |
| **Komponenty Profile** | 4/4 | 0 | **100%** | Gotowe |
| **Komponenty Habits (Lista)** | 7/7 | 0 | **100%** | Gotowe |
| **Komponenty Habits (Detale)** | 0/6+ | CalendarView, RollingSuccessChart, HabitDetailsHeader, SwitchDropdown | **0%** | Brak |
| **Komponenty Notifications** | 2/4+ (strona + Bell) | List, Item, Pagination | **~40%** | Czescowo |
| **Error Handling** | Podstawowy | ErrorBoundary, ErrorView, NotFound | **~30%** | Brak |

**Ogolne pokrycie MVP:** ~70% (po implementacji widoku listy nawykow)

---

## Zakonczone (2026-01-19)
- **NavMenu update** - dodano linki "Nawyki" i "Powiadomienia"
- **Strony zastepcze** - `/notifications`
- **NotificationsBell** - ikona dzwonka w top bar
- **BottomNav** - responsive navigation dla mobile
- **Today View (95%+)** - backfill date picker (empty state nadal TODO)
- **Habits List (CRUD)** - lista, filtry, paginacja, dialogi create/edit/delete, quick check-in

### Sprint 1 - Krytyczne (Habits CRUD)
1. **`/habits` (lista)** - ZAKONCZONE 2026-01-19 (CRUD, filtry, paginacja, quick check-in)
2. **Landing redirect (`/`)** - ZAKONCZONE 2026-01-18 (UX onboarding)

### Sprint 2 - Wysokie (Visualizations & Notifications)
3. **`/habits/{id}` (detale)** - kalendarz readonly i wykres rolling 7/30
4. **`/notifications`** - powiadomienia AI (F-009 z PRD)
5. **EmailConfirmationGate** - blokada niepotwierdzonego email (US-001)

### Sprint 3 - Srednie (UX Polish)
6. **Backfill date picker** w CheckinDialog - zrobione (US-013)
7. **Mobile bottom-nav** - zrobione
8. **Error Boundary & 404** - dedykowane widoki bledow
9. **Global banners** - reconnect, loading, info

---

## Notatki implementacyjne

### Biblioteki do rozwazenia:
- **Wykresy:** ApexCharts.Blazor, Plotly.Blazor, lub Chart.js
- **Kalendarz:** Custom z MudBlazor Grid
- **Date picker:** MudBlazor DatePicker (juz uzywane)

### Wzorce architektoniczne (zgodnie z ui-plan.md):
- **State management:** Scoped services `*State` (per SignalR circuit) + `*Service` (API calls)
- **Modale:** MudBlazor DialogService
- **Loading states:** Globalne (top bar progress) + lokalne (spinner w komponentach)
- **Error handling:** ProblemDetails -> snackbary + inline errors w formularzach

### Konwencje nazewnictwa (zgodnie z AGENTS.md):
- **Feature-based organization**:
  - Strony i komponenty: `Components/Pages/{Feature}/{ComponentName}.razor`
  - Shared: `Components/Shared/{ComponentName}.razor`
  - Layout: `Components/Layout/{LayoutName}.razor`
- **Namespace:** Dodaj `@using HabitFlow.Blazor.Components.Pages.{Feature}` w `_Imports.razor`
- **Zasada:** Komponenty ktore sie zmieniaja razem, sa przechowywane razem

---

## Powiazane dokumenty

- **ui-plan.md** - architektura UI, mapa widokow, UX
- **prd.md** - wymagania funkcjonalne i user stories
- **test-plan.md** - strategia testowania komponentow (bUnit)
- **AGENTS.md** - konwencje projektu, styl kodowania, struktura folderow Blazor
- **CLAUDE.md** - glowny plik konfiguracyjny

---

## Historia zmian

### 2026-01-17 (19:30) - Layout & Navigation + refaktoring struktury
- Dodano `NotificationsBell`, `BottomNav`
- Dodano strony zastepcze `/habits` i `/notifications`
- Zmieniono `NavMenu` (usunieto Home, dodano Nawyki/Powiadomienia)
- Uporzadkowano strukture folderow (feature-based)

### 2026-01-18 (10:45) - Today View (backfill)
- Dodano date picker do CheckinDialog (7 dni wstecz)
- Aktualizacja obsugi check-in o wybrana date

### 2026-01-18 (11:30) - Landing/Root redirect
- Dodano przekierowanie z `/` do `/today` lub `/auth/login`
- Aktualizacja dokumentacji postepu

### 2026-01-19 (00:01) - Habits List (CRUD) + poprawki kompilacji
- Zaimplementowano liste nawykow z filtrami i paginacja
- Dodano dialogi create/edit/delete + quick check-in
- Stabilizacja kompilacji: `IMudDialogInstance`, `SelectedDays` zgodne z MudChipSet

**Ostatnia aktualizacja:** 2026-01-19 (00:01)
**Autor analizy:** Claude (agent AI)
