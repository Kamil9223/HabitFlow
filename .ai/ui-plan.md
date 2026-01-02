# Architektura UI dla HabitFlow

## 1. Przeglad struktury UI

HabitFlow to aplikacja webowa oparta o Blazor Server z UI zorganizowanym wokol glownej powloki aplikacji (App Shell) i rozdzielonymi widokami uwierzytelniania. Po zalogowaniu uzytkownik trafia do /today, a reszta widokow biznesowych jest dostepna w ramach jednej nawigacji. UI integruje sie z REST API zgodnie z planem /api/v1 i nie replikuje logiki success_rate, bazujac na wartosciach zwracanych przez API.

Kluczowe zalozenia architektury:
- Dwa warianty ukladu: desktop (sidebar + top bar + content) oraz mobile (top bar + drawer + bottom-nav), projektowanie mobile-first.
- Rozdzielenie UI state i server state poprzez serwisy *Service (komunikacja z API) oraz *State (stan widokow) scoped do circuit.
- Wszystkie akcje mutujace realizowane przez modale/dialogi (HabitForm, Check-in, ConfirmDialog); kalendarz pozostaje readonly.
- Globalna obsluga bledow (ErrorBoundary) i mapowanie ProblemDetails na komunikaty formularzy i snackbary.
- Kontrola dostepu: widoki biznesowe chronione, blokada kluczowych akcji przy emailConfirmed=false.
- Spolne wzorce UX: loading states (global i lokalne), empty states z CTA, ograniczenie klikniec do check-in (2-3).

## 2. Lista widokow

Widok: Landing/Root
- Sciezka widoku: /
- Glowny cel: przekierowanie na /today po zalogowaniu lub /auth/login przy braku sesji.
- Kluczowe informacje do wyswietlenia: brak (redirect).
- Kluczowe komponenty widoku: AppShellRouter, AuthRedirectGuard.
- UX, dostepnosc i wzgledy bezpieczenstwa: brak UI; szybkie przekierowanie; zapobieganie migotaniu widoku niezalogowanego.
- Powiazane wymagania: F-001, US-002, US-020.

Widok: Rejestracja
- Sciezka widoku: /auth/register
- Glowny cel: utworzenie konta i wywolanie weryfikacji e-mail.
- Kluczowe informacje do wyswietlenia: pola email/haslo, wymagania hasla, link do logowania.
- Kluczowe komponenty widoku: AuthRegisterForm, FormErrorSummary, SubmitButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: walidacja client+server, komunikaty 400/409; brak ujawniania szczegolow bledow; focus na pierwszym bledzie.
- Powiazane wymagania: F-001, US-001, US-021.
- API: POST /api/v1/auth/register.

Widok: Potwierdzenie e-mail
- Sciezka widoku: /auth/confirm-email
- Glowny cel: obsluga linku potwierdzajacego i pokazanie statusu.
- Kluczowe informacje do wyswietlenia: komunikat sukcesu/bledu, CTA do logowania.
- Kluczowe komponenty widoku: EmailConfirmStatus, PrimaryActionButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: jasne stany (sukces, blad, juz potwierdzone), brak wrazliwych danych.
- Powiazane wymagania: US-001.
- API: POST /api/v1/auth/confirm-email.

Widok: Logowanie
- Sciezka widoku: /auth/login
- Glowny cel: zalogowanie uzytkownika.
- Kluczowe informacje do wyswietlenia: pola email/haslo, link do resetu hasla, CTA do rejestracji.
- Kluczowe komponenty widoku: AuthLoginForm, FormErrorSummary, SubmitButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: obsluga 401/403, komunikat o niepotwierdzonym emailu; brak szczegolow w bledach.
- Powiazane wymagania: F-001, US-002, US-021.
- API: POST /api/v1/auth/login.

Widok: Zapomniane haslo
- Sciezka widoku: /auth/forgot-password
- Glowny cel: wyslanie linku resetu hasla.
- Kluczowe informacje do wyswietlenia: pole email, komunikat sukcesu.
- Kluczowe komponenty widoku: ForgotPasswordForm, SubmitButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: brak ujawniania czy email istnieje; czytelny komunikat.
- Powiazane wymagania: US-003.
- API: POST /api/v1/auth/forgot-password.

Widok: Reset hasla
- Sciezka widoku: /auth/reset-password
- Glowny cel: ustawienie nowego hasla.
- Kluczowe informacje do wyswietlenia: pola email, token, nowe haslo.
- Kluczowe komponenty widoku: ResetPasswordForm, SubmitButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: walidacja hasla, obsluga 400; po sukcesie CTA do logowania.
- Powiazane wymagania: US-003.
- API: POST /api/v1/auth/reset-password.

Widok: Today
- Sciezka widoku: /today
- Glowny cel: pokazanie dzisiejszych krokow i szybki check-in.
- Kluczowe informacje do wyswietlenia: data lokalna, lista dzisiejszych itemow, statusy check-in, licznik postepu dnia (X/Y).
- Kluczowe komponenty widoku: TodayChecklist, TodayProgressHeader, CheckinDialog, EmptyStateCard, RefreshButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: 2-3 klikniecia do check-in; blokada podwojnego wyslania; obsluga 401/403; widoczne komunikaty limitow i bledow 409/422.
- Powiazane wymagania: F-004, F-005, F-012, US-010, US-011, US-012, US-013, US-025.
- API: GET /api/v1/today, POST /api/v1/habits/{id}/checkins.

Widok: Habits (lista)
- Sciezka widoku: /habits
- Glowny cel: przeglad i zarzadzanie nawykami.
- Kluczowe informacje do wyswietlenia: tytul, typ, harmonogram, skrócony success_rate, deadline, licznik N/20.
- Kluczowe komponenty widoku: HabitList, HabitItem, HabitFormDialog, ConfirmDialog, CheckinDialog.
- UX, dostepnosc i wzgledy bezpieczenstwa: filtrowanie i paginacja; widoczne limity; obsluga 409 przy limicie; modalne potwierdzenia przy usunieciu.
- Powiazane wymagania: F-003, F-012, US-005, US-006, US-007, US-008, US-009, US-021.
- API: GET/POST/PATCH/DELETE /api/v1/habits, POST /api/v1/habits/{id}/checkins.

Widok: Habit Details
- Sciezka widoku: /habits/{id}
- Glowny cel: szczegoly jednego nawyku, kalendarz i postep.
- Kluczowe informacje do wyswietlenia: typ, tryb, harmonogram, targetValue/unit, deadline, success_rate.
- Kluczowe komponenty widoku: HabitDetailsHeader, HabitSwitchDropdown, TabCalendar, TabProgress, CalendarView, RollingSuccessChart, CheckinDialog.
- UX, dostepnosc i wzgledy bezpieczenstwa: kalendarz readonly; tooltipy wyjasniajace start/stop; obsluga 404 dla cudzych/nieistniejacych zasobow.
- Powiazane wymagania: F-006, F-007, F-008, US-015, US-016, US-024.
- API: GET /api/v1/habits/{id}, GET /api/v1/habits/{id}/calendar, GET /api/v1/habits/{id}/progress/rolling.

Widok: Notifications
- Sciezka widoku: /notifications
- Glowny cel: przeglad powiadomien miss due.
- Kluczowe informacje do wyswietlenia: lista powiadomien (tytul nawyku, data, tresc, aiStatus), liczba nowych.
- Kluczowe komponenty widoku: NotificationsList, NotificationItem, Pagination, NotificationsBell.
- UX, dostepnosc i wzgledy bezpieczenstwa: paginacja zamiast infinite scroll; obsluga 401; jasne rozroznienie nowych.
- Powiazane wymagania: F-009, F-011, US-017, US-018, US-022.
- API: GET /api/v1/notifications.

Widok: Profile
- Sciezka widoku: /profile
- Glowny cel: podglad i edycja profilu (strefa czasu) oraz usuniecie konta.
- Kluczowe informacje do wyswietlenia: email, createdAt, aktualna timeZoneId, informacja o wejściu zmiany od nastepnego dnia.
- Kluczowe komponenty widoku: ProfileSummary, TimeZonePicker, ConfirmDeleteAccountDialog.
- UX, dostepnosc i wzgledy bezpieczenstwa: potwierdzenie usuniecia tekstem "DELETE"; obsluga 400/422 przy timeZoneId.
- Powiazane wymagania: F-002, F-010, US-004, US-019.
- API: GET /api/v1/profile, PATCH /api/v1/profile/timezone, DELETE /api/v1/profile.

Widok: Blokada niepotwierdzonego e-maila
- Sciezka widoku: stan globalny po zalogowaniu (np. banner lub modal w App Shell)
- Glowny cel: zablokowanie kluczowych akcji i wskazanie potrzeby potwierdzenia e-mail.
- Kluczowe informacje do wyswietlenia: komunikat o koniecznosci potwierdzenia i CTA do ponownego wyslania (jesli dostepne).
- Kluczowe komponenty widoku: EmailConfirmationGate, AlertBanner.
- UX, dostepnosc i wzgledy bezpieczenstwa: czytelny komunikat, brak ujawniania tokenow.
- Powiazane wymagania: US-001, US-002.
- API: GET /api/v1/auth/me.

Widok: Not Found / Error
- Sciezka widoku: /404 oraz globalny ErrorBoundary
- Glowny cel: bezpieczna obsluga bledow i nieistniejacych zasobow.
- Kluczowe informacje do wyswietlenia: przyjazny komunikat, CTA do /today.
- Kluczowe komponenty widoku: ErrorView, ErrorBoundary, RetryButton.
- UX, dostepnosc i wzgledy bezpieczenstwa: brak wycieku informacji; obsluga 404/5xx.
- Powiazane wymagania: US-021, US-023.

## 3. Mapa podrozy uzytkownika

Sciezka krytyczna (E2E):
1. /auth/register: rejestracja konta, komunikat o koniecznosci potwierdzenia e-mail.
2. /auth/confirm-email: potwierdzenie z linku, przejscie do logowania.
3. /auth/login: logowanie; przekierowanie do /today.
4. /today: puste stany -> CTA "Utworz pierwszy nawyk" (otwiera HabitFormDialog).
5. /habits (modal z /today): zapis nowego nawyku, lista uzupelnia sie; uzytkownik wraca do /today.
6. /today: klik "Check-in" -> CheckinDialog -> zapis.
7. /habits/{id}: przeglad szczegolow, tab Calendar i Progress; przełączenie okna 7/30.
8. /notifications: po miss due pojawia sie komunikat; uzytkownik przeglada powiadomienia.

Sciezki alternatywne:
- Backfill: /today lub /habits/{id} -> CheckinDialog z wyborem daty do 7 dni wstecz.
- Edycja i usuniecie: /habits -> HabitFormDialog lub ConfirmDialog.
- Zmiana strefy czasu: /profile -> TimeZonePicker -> zapis.
- Brak autoryzacji: kazda proba wejscia do widokow biznesowych -> przekierowanie do /auth/login.

## 4. Uklad i struktura nawigacji

- App Shell (po zalogowaniu):
  - Desktop: lewy sidebar z nawigacja (Today, Habits, Notifications, Profile) + top bar z tytulem widoku i ikonami (np. dzwonek powiadomien).
  - Mobile: top bar + drawer dla pelnej nawigacji oraz bottom-nav (Today, Habits, Notifications).
- Widoki auth sa poza shellem, z minimalna nawigacja (linki do login/register/forgot).
- Habit Details ma wewnetrzne taby (Calendar, Progress) i dropdown do wyboru nawyku.
- Globalne elementy: banner reconnect, global loader, error boundary, snackbar.

## 5. Kluczowe komponenty

- AppShell: zarzadza layoutem, zabezpieczeniami, stanem polaczenia i globalnymi bannerami.
- Navigation: SidebarNav, TopBar, BottomNav, NotificationsBell (licznik nowych).
- TodayChecklist: lista dzisiejszych krokow z akcjami check-in i naglowkiem postepu.
- HabitList + HabitItem: karta/lista nawykow z akcjami (check-in, edytuj, usun, detale).
- HabitFormDialog: tworzenie/edycja nawyku (sekcje: Podstawy, Harmonogram, Deadline).
- CheckinDialog: wpis dzienny z limitem 0..targetValue i data do 7 dni wstecz.
- CalendarView: readonly kalendarz statusow z tooltipami i kolorami wspartymi ikonami/tekstami.
- RollingSuccessChart: wykres rolling 7/30 z przelacznikiem i tooltipem.
- ConfirmDialog: potwierdzenia usuniecia nawyku i konta.
- ProfileSummary + TimeZonePicker: podglad danych i edycja strefy czasu.
- ErrorBoundary + ErrorView: globalne bledy i przyjazne komunikaty.
- LoadingStates: GlobalProgressBar, ButtonLoading, SkeletonList.
- EmptyStateCard: puste stany z CTA (np. utworzenie pierwszego nawyku).
