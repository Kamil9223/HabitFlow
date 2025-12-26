# Tech stack – HabitFlow (MVP)

Stos technologiczny i decyzje dla MVP.

1. Backend
- Platforma: .NET 9, ASP.NET Core 9 (C# 13)
- Architektura: aplikacja serwerowa z komponentami Blazor Server
- Warstwa danych: Entity Framework Core (code-first, migracje)
- Autoryzacja/uwierzytelnianie: ASP.NET Core Identity (weryfikacja e-mail, reset hasła)

2. Frontend
- Blazor Server (rendering po stronie serwera, polaczenia SignalR)
- UI: MudBlazor (jedyna biblioteka UI; bez mieszania z Bootstrap/Tailwind)
- Cel: szybkie dostarczenie CRUD, ekran „Dziś”, kalendarz readonly i wykres rolling 7/30

3. Baza danych
- SQL Server 2022
- Model: relacyjny; przechowywanie nawyków, harmonogramów, check-inów, powiadomień
- Transakcje, spójność, indeksy pod listy i raporty

4. Integracje i usługi pomocnicze
- Generowanie treści AI: dostawca LLM przez HTTP z fallbackiem do stałych szablonów
- Zadania w tle: scheduler/background jobs do triggerów „miss due” (np. Hangfire/Quartz) – opcjonalnie w MVP, rekomendowane

5. CI/CD i jakość
- CI/CD: GitHub Actions (build, testy, migracje)
- Testy: unit dla logiki success_rate i naruszeń, E2E dla ścieżki krytycznej

6. Decyzja produktowo-techniczna
- Zostajemy przy Blazor Server app i SQL Server w MVP.
- Uzasadnienie:
  - Szybkie dostarczenie dla solo deva .NET dzięki gotowym klockom (Identity, EF Core, komponenty Blazor).
  - Spójny stack zmniejsza koszt poznawczy i przyspiesza implementację krytycznych funkcji (CRUD, auth, check-in, kalendarz, wykres).
  - SQL Server zapewnia dojrzałe mechanizmy i prostą obsługę relacyjnego modelu danych.

7. Uwagi na przyszłość (po MVP)
- Skalowanie Blazor Server może wymagać sticky sessions/Redis backplane; przy większym ruchu możliwa migracja frontu do Blazor WebAssembly/SPA bez zmiany API.
- Alternatywna baza (np. PostgreSQL) może obniżyć koszty utrzymania; decyzja pozostaje poza MVP.
