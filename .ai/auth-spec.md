# Specyfikacja architektury autentykacji (Auth) - HabitFlow

## Kontekst i zrodla
- PRD: .ai/prd.md (US-001, US-002, US-003, US-019, US-020, US-023)
- Tech stack: .ai/tech-stack.md (ASP.NET Core Identity, Blazor Server, MudBlazor)
- Endpointy (stub): HabitFlow.Api/Endpoints/AuthEndpoints.cs
- Rules: .aiassistant/rules/backend.md, .aiassistant/rules/frontend.md, .aiassistant/rules/shared.md

## 1. Architektura interfejsu uzytkownika (Blazor Server + MudBlazor)

### 1.1 Strony, layouty i routing (auth vs non-auth)
- Auth layout (minimalny, bez nawigacji aplikacji):
  - /auth/register
  - /auth/confirm-email (widok sukcesu / bledu po kliknieciu linku)
  - /auth/login
  - /auth/forgot-password
  - /auth/reset-password
- Non-auth (zalogowany) layout:
  - /today (landing po logowaniu)
  - /profile/security (ustawienia konta, w tym usuniecie konta)
  - /logout (akcja wylogowania z przekierowaniem)

Layouty:
- AuthLayout: uproszczona nawigacja (logo + link do logowania/rejestracji).
- MainLayout: standardowa nawigacja aplikacji (Today, Habits, Calendar, Charts, Notifications, Profile).

Wymogi UI:
- MudBlazor jako jedyna biblioteka UI.
- Brak przechowywania wrazliwych danych w storage przegladarki.

### 1.2 Komponenty i odpowiedzialnosci
- Strony auth to cienkie kontenery, ktore:
  - przyjmuja input uzytkownika,
  - wywoluja AuthService (typed HttpClient do Minimal API),
  - mapuja ProblemDetails na komunikaty dla uzytkownika,
  - nawigują do kolejnych krokow.
- Komponenty korzystaja z AuthenticationStateProvider, AuthorizeView i [Authorize] na stronach chronionych.

Komponenty formularzy (jeden publiczny komponent na plik, MudBlazor):
- Components/Auth/RegisterForm.razor
  - Pola: Email, Password, ConfirmPassword.
  - Walidacja klienta: format email, min 8 znakow, zgodnosc hasel.
  - Po sukcesie: pokaz komunikat o wyslaniu linku weryfikacyjnego.
- Components/Auth/LoginForm.razor
  - Pola: Email, Password.
  - Po sukcesie: przekierowanie do /today.
  - Obsluga stanu: isLoading, blokada ponownego wyslania.
- Components/Auth/ForgotPasswordForm.razor
  - Pole: Email.
  - Po sukcesie: neutralny komunikat (nie ujawnia, czy email istnieje).
- Components/Auth/ResetPasswordForm.razor
  - Pola: NewPassword, ConfirmPassword; ukryte token + userId z query.
  - Po sukcesie: przekierowanie do /auth/login z komunikatem.
- Components/Auth/ConfirmEmailResult.razor
  - Odczytuje token i userId z query, wywoluje potwierdzenie.
  - Pokazuje wynik (sukces / invalid / expired / already confirmed).

### 1.3 Nawigacja i scenariusze UI
- Rejestracja:
  - /auth/register -> POST /api/v1/auth/register
  - Po powodzeniu: informacja o wyslanym mailu; link do /auth/login.
- Potwierdzenie e-mail:
  - Klikniecie linku -> /auth/confirm-email?userId=...&token=...
  - Widok wyniku, opcja przejscia do logowania.
- Logowanie:
  - /auth/login -> POST /api/v1/auth/login
  - Sukces: redirect /today; blad: komunikat ogolny.
- Zapomnialem hasla:
  - /auth/forgot-password -> POST /api/v1/auth/forgot-password
  - Po powodzeniu: neutralny komunikat + instrukcja sprawdzenia maila.
- Reset hasla:
  - /auth/reset-password?userId=...&token=...
  - POST /api/v1/auth/reset-password
  - Sukces: redirect do /auth/login.
- Wylogowanie:
  - Akcja w menu -> POST /api/v1/auth/logout -> redirect /auth/login.
- Usuniecie konta (US-019):
  - W /profile/security modal z potwierdzeniem wpisania "DELETE".
  - Po potwierdzeniu: POST /api/v1/auth/delete-account, wylogowanie i redirect.

### 1.4 Walidacja i komunikaty bledow (UI)
- Walidacja klienta (DataAnnotations + MudBlazor):
  - Email: format.
  - Haslo: min 8 znakow.
  - ConfirmPassword: zgodnosc.
- Walidacja serwera (ProblemDetails -> mapowanie na pola):
  - 400/422: bledy walidacji (np. haslo zbyt krotkie, zly format e-mail).
  - 409: konflikt (email zajety, token juz uzyty).
  - 401/403: bledne dane lub niepotwierdzony email.
- Komunikaty bez ujawniania szczegolow:
  - Login: "Nieprawidlowy e-mail lub haslo".
  - ForgotPassword: "Jesli konto istnieje, wyslalismy link".

### 1.5 Integracja i stan (UI)
- AuthService (Scoped) odpowiada za:
  - wywolania HttpClient do /api/v1/auth/*,
  - mapowanie ProblemDetails na bloki UI.
- Cookie auth (ASP.NET Core Identity): sesja po stronie serwera, brak tokenow po stronie klienta.
- Wszystkie operacje async przyjmuja CancellationToken, anulowane przy nawigacji.
- Brak async void w eventach; blokada podwojnych submitow.

## 2. Logika backendowa (Minimal API + Clean Architecture + Identity)

### 2.1 Warstwy i kontrakty
- Domain: modele domenowe i wartosci wlasne (bez detali Identity).
- Application:
  - Commands (CQS):
    - RegisterUserCommand -> Result<RegisterResult>
    - ConfirmEmailCommand -> Result
    - LoginCommand -> Result<LoginResult>
    - ForgotPasswordCommand -> Result
    - ResetPasswordCommand -> Result
    - LogoutCommand -> Result
    - DeleteAccountCommand -> Result
  - Queries:
    - GetMeQuery -> Result<MeDto>
  - Handlery: ICommandHandler / IQueryHandler z CancellationToken.
  - Walidacja w dispatcherach przed handlerem.
- Infrastructure:
  - Integracja z ASP.NET Core Identity (UserManager/SignInManager).
  - Tokeny reset/confirm: Identity token provider.
  - EmailSender (IEmailSender) do wysylki linkow.
  - Repozytoria tylko dla komend; DbContext jako Unit of Work.
- Api:
  - Minimal API (AuthEndpoints.cs) mapuje endpointy na dispatchery.
  - Mapper ProblemDetails dla Result/Result<T>.

### 2.2 Zasady zapytan i danych
- Query side: DbContext bezposrednio lub read-store w Application; zawsze AsNoTracking i projekcja Select do DTO.
- Zapobiegaj N+1 przez Include/ThenInclude tylko tam, gdzie konieczne.

### 2.3 Endpointy i zachowanie
- POST /api/v1/auth/register
  - Wejscie: RegisterRequest (Email, Password)
  - Akcje:
    - Create user, ustaw EmailConfirmed = false.
    - Wyslij link potwierdzajacy (token jednorazowy, wygasa np. 60 min).
  - Wynik:
    - 201 RegisterResponse (np. userId, email)
    - 409 gdy email zajety
    - 422 walidacja
- POST /api/v1/auth/confirm-email
  - Wejscie: ConfirmEmailRequest (userId, token)
  - Wynik: 204 po sukcesie
  - 404 gdy user nie istnieje
  - 409 gdy email juz potwierdzony lub token zuzyty
- POST /api/v1/auth/login
  - Wejscie: LoginRequest (Email, Password)
  - Wymaga potwierdzonego emaila
  - Wynik:
    - 200 LoginResponse (cookie auth)
    - 401 bledne dane
    - 403 email niepotwierdzony
- POST /api/v1/auth/forgot-password
  - Wejscie: ForgotPasswordRequest (Email)
  - Zawsze 204, bez ujawniania czy email istnieje
  - Wysylka maila z linkiem resetu
- POST /api/v1/auth/reset-password
  - Wejscie: ResetPasswordRequest (userId, token, newPassword)
  - Wynik: 204 po sukcesie
  - 400 invalid/expired token
- GET /api/v1/auth/me
  - Wymaga autoryzacji
  - Zwraca MeResponse (userId, email, emailConfirmed)
- POST /api/v1/auth/logout
  - Wymaga autoryzacji
  - Inwalidacja sesji/cookie; 204
- POST /api/v1/auth/delete-account
  - Wymaga autoryzacji
  - Hard delete konta i danych; 204

Konwencje mapowania:
- POST/PUT/PATCH/DELETE -> ICommandDispatcher
- GET -> IQueryDispatcher

### 2.4 Model danych (Identity)
- AspNetUsers (IdentityUser) z polami:
  - Email, EmailConfirmed, PasswordHash
  - (opcjonalnie) TimeZoneId
- Indeksy:
  - unikatowy indeks na Email

### 2.5 Bezpieczenstwo i autoryzacja (US-023)
- Autoryzacja zasobow: kazdy endpoint domenowy weryfikuje userId z kontekstu.
- Email musi byc potwierdzony przed logowaniem i tworzeniem danych.
- Tokeny resetu i potwierdzenia:
  - jednorazowe, z limitem czasu.
  - tokeny kodowane Base64Url.
- Brak wycieku informacji:
  - ForgotPassword zawsze 204.
  - Login zawsze ogolny komunikat.

### 2.6 Obsluga bledow i Result/ProblemDetails
- Handlery zwracaja Result/Result<T> z kodem bledu i komunikatem.
- Globalny mapper ProblemDetails:
  - 400/422: walidacja
  - 401: unauthorized
  - 403: forbidden (email niepotwierdzony)
  - 404: not found
  - 409: conflict
  - 500: nieoczekiwane bledy

### 2.7 Integracje (Email)
- EmailSender wysyla:
  - Link potwierdzajacy: /auth/confirm-email?userId=...&token=...
  - Link resetu: /auth/reset-password?userId=...&token=...
- Tresci e-mail proste i zgodne z MVP.

## 3. Scenariusze krytyczne
- US-001 Rejestracja:
  - Register -> wysylka maila -> confirm-email -> login -> /today.
- US-002 Logowanie:
  - Poprawne dane + email confirmed -> sesja -> /today.
- US-003 Reset hasla:
  - Forgot -> link -> reset -> login.
- US-019 Usuniecie konta:
  - Potwierdzenie "DELETE" -> hard delete danych -> wylogowanie.
- US-020 Wylogowanie:
  - Inwalidacja sesji/cookie -> przekierowanie do /auth/login.
- US-023 Dostep do zasobow:
  - Kazde zapytanie zwraca 403/404 dla cudzych zasobow.

## 4. Uwagi implementacyjne i decyzje
- Wybrany mechanizm auth: cookie auth zgodny z ASP.NET Core Identity.
- Endpoint usuniecia konta jest w grupie /api/v1/auth (POST /delete-account).
- Testy: obowiazuja wytyczne z backend rules (HabitFlow.Tests, UnitTests/IntegrationTests).

