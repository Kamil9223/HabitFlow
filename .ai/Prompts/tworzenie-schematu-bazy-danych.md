Jesteś architektem baz danych, którego zadaniem jest stworzenie schematu bazy danych SQL Server na podstawie informacji dostarczonych z sesji planowania, dokumentu wymagań produktu (PRD) i stacku technologicznym. Twoim celem jest zaprojektowanie wydajnej i skalowalnej struktury bazy danych, która spełnia wymagania projektu.

1. <prd>
@prd.md
</prd>

Jest to dokument wymagań produktu, który określa cechy, funkcjonalności i wymagania projektu.

2. <session_notes>
   <conversation_summary>
1. Uwierzytelnianie: używamy pełnego ASP.NET Core Identity; pola domenowe w AspNetUsers (TimeZoneId, CreatedAtUtc).
2. Harmonogram: maska dni tygodnia (7-bit), z co najmniej jednym dniem ustawionym.
3. Check-in tylko w dni zaplanowane (walidacja po stronie aplikacji).
4. Czas: momenty w UTC; kalendarzowe pola jako LocalDate/LocalTime (date/time w DB).
5. Typy nawyków „start/stop” nie zmieniają zakresu dopuszczalnych wartości Value w check-in.
6. Brak edycji check-inów; zmiana planu = nowy nawyk (stary nieedytowalny).
7. Powiadomienia „miss due”: brak limitowania (MVP); treść ograniczona długością.
8. Usuwanie: wyłącznie hard delete, kaskady FK.
9. Indeksy: jak rekomendowano (Habits, Checkins, Notifications).
10. RLS: włączone; predicate po UserId z SESSION_CONTEXT; brak tabeli Profile, używamy Users.
11. Deadline: opcjonalny; po jego osiągnięciu blokujemy dalsze check-iny dla nawyku (aplikacyjnie).
12. Brak widoków/TVF pod wykres; agregacje liczone w aplikacji (EF Core).

<matched_recommendations>
1. Trzymać TimeZoneId i CreatedAtUtc w AspNetUsers; domenowe dane bez osobnej tabeli profilu.
2. Habits: Title (<=80), Description (<=280), Type tinyint, DaysOfWeekMask tinyint (CHECK 1–127), RepetitionsPerDay smallint (1–100), DeadlineDate date NULL, CreatedAtUtc datetime2; FK UserId z CASCADE.
3. Checkins: UNIQUE(HabitId, LocalDate); kolumny: UserId (denormalizacja), Value int, RepetitionsSnapshot smallint, HabitTypeSnapshot tinyint, IsPlanned bit, CreatedAtUtc datetime2; indeksy (HabitId, LocalDate) i (UserId, LocalDate).
4. Notifications: Content nvarchar(1024) max; Type tinyint (MissDue), AiStatus/AiError; unikalność (HabitId, LocalDate, Type); indeks (UserId, CreatedAtUtc DESC).
5. Wszystkie „momenty” w UTC; LocalDate dla doby użytkownika zgodnie z TimeZoneId.
6. Weryfikacja „LocalDate zgodny z DaysOfWeekMask” w aplikacji, nie CHECK.
7. RLS: kolumna UserId w każdej tabeli domenowej; polityki FILTER/BLOCK z SESSION_CONTEXT('user_id').
8. Indeksy pod „Dziś” i wykres: Checkins (UserId, LocalDate, HabitId) jako klastrowy; dodatkowe INCLUDE dla wartości/snapshotów.
9. Limit 20 nawyków per user – walidacja aplikacyjna w transakcji.
10. Brak partycjonowania w MVP; możliwość dodania po LocalDate w przyszłości. 
/matched_recommendations>

<database_planning_summary> Główne wymagania schematu:
- Pełny schemat Identity; AspNetUsers rozszerzone o TimeZoneId (nvarchar(64)), CreatedAtUtc (datetime2).
- Domenowe tabele: Habits, Checkins, Notifications z kolumną UserId i kaskadowymi FK.
- Harmonogram jako DaysOfWeekMask (1–127) i RepetitionsPerDay (1–100).
- DeadlineDate (date) opcjonalny; po jego dacie system blokuje nowe check-iny (logika aplikacyjna).

Kluczowe encje i relacje:
- AspNetUsers 1—N Habits (FK CASCADE).
- Habits 1—N Checkins (FK CASCADE), UNIQUE(HabitId, LocalDate).
- Habits 1—N Notifications (dla MissDue HabitId NOT NULL), UNIQUE(HabitId, LocalDate, Type).
- Denormalizacja UserId do Checkins/Notifications dla RLS i zapytań.

Bezpieczeństwo i skalowalność:
- RLS: polityki FILTER/BLOCK porównujące UserId z SESSION_CONTEXT('user_id'); ustawiane po autoryzacji.
- Hard delete z kaskadami; brak audytu w MVP.
- Indeksy:
    - Habits(UserId, CreatedAtUtc DESC) INCLUDE (Title, Type, DaysOfWeekMask, RepetitionsPerDay, DeadlineDate).
    - Checkins CLUSTERED (UserId, LocalDate, HabitId); nonclustered (HabitId, LocalDate) INCLUDE (Value, RepetitionsSnapshot, HabitTypeSnapshot).
    - Notifications(UserId, CreatedAtUtc DESC) INCLUDE (Content, Type, HabitId, LocalDate).

- Wydajność: obliczenia rolling 7/30 i ekran „Dziś” wykonywane w aplikacji (EF Core) z użyciem powyższych indeksów; brak widoków/TVF w MVP.
- Brak partycjonowania; możliwość rozważenia później po LocalDate.

Nierelacyjne zasady biznesowe egzekwowane aplikacyjnie:
- Check-in tylko dla dni zgodnych z DaysOfWeekMask.
- Zakaz check-in po DeadlineDate.
- Limit 20 nawyków per user.
- Okno uzupełniania check-in do 7 dni wstecz zgodnie ze strefą czasu użytkownika.
- Brak edycji check-in; snapshoty Repetitions/HabitType w momencie zapisu.

Dane czasowe:
- Wszystkie momenty w datetime2 UTC; LocalDate (date) dla doby lokalnej; koniec doby stały 23:59 lokalnie. 
</database_planning_summary>

<unresolved_issues>
1. Czy przechowywać EndedAtUtc/Status w Habits dla raportowania zamkniętych nawyków (mimo braku archiwizacji)?
2. Dokładna semantyka MissDue przy zmianie strefy czasu użytkownika w trakcie dnia (moment cięcia).
3. Maksymalna długość AiError/diagnoz i ewentualne logowanie techniczne poza DB. </unresolved_issues> 
</conversation_summary>
</session_notes>

Są to notatki z sesji planowania schematu bazy danych. Mogą one zawierać ważne decyzje, rozważania i konkretne wymagania omówione podczas spotkania.

3. <tech_stack>
   @tech-stack.md
4. @backend.md
   </tech_stack>

Opisuje stack technologiczny, który zostanie wykorzystany w projekcie, co może wpłynąć na decyzje dotyczące projektu bazy danych.

Wykonaj następujące kroki, aby utworzyć schemat bazy danych:

1. Dokładnie przeanalizuj notatki z sesji, identyfikując kluczowe jednostki, atrybuty i relacje omawiane podczas sesji planowania.
2. Przejrzyj PRD, aby upewnić się, że wszystkie wymagane funkcje i funkcjonalności są obsługiwane przez schemat bazy danych.
3. Przeanalizuj stack technologiczny i upewnij się, że projekt bazy danych jest zoptymalizowany pod kątem wybranych technologii.

4. Stworzenie kompleksowego schematu bazy danych, który obejmuje
   a. Tabele z odpowiednimi nazwami kolumn i typami danych
   b. Klucze podstawowe i klucze obce
   c. Indeksy poprawiające wydajność zapytań
   d. Wszelkie niezbędne ograniczenia (np. unikalność, not null)

5. Zdefiniuj relacje między tabelami, określając kardynalność (jeden-do-jednego, jeden-do-wielu, wiele-do-wielu) i wszelkie tabele łączące wymagane dla relacji wiele-do-wielu.

6. Opracowanie zasad PostgreSQL dla zabezpieczeń na poziomie wiersza (RLS), jeśli dotyczy, w oparciu o wymagania określone w notatkach z sesji lub PRD.

7. Upewnij się, że schemat jest zgodny z najlepszymi praktykami projektowania baz danych, w tym normalizacji do odpowiedniego poziomu (zwykle 3NF, chyba że denormalizacja jest uzasadniona ze względu na wydajność).

Ostateczny wynik powinien mieć następującą strukturę:
```markdown
1. Lista tabel z ich kolumnami, typami danych i ograniczeniami
2. Relacje między tabelami
3. Indeksy
4. Zasady PostgreSQL (jeśli dotyczy)
5. Wszelkie dodatkowe uwagi lub wyjaśnienia dotyczące decyzji projektowych
```

W odpowiedzi należy podać tylko ostateczny schemat bazy danych w formacie markdown, który zapiszesz w pliku .ai/db-plan.md bez uwzględniania procesu myślowego lub kroków pośrednich. Upewnij się, że schemat jest kompleksowy, dobrze zorganizowany i gotowy do wykorzystania jako podstawa do tworzenia migracji baz danych.