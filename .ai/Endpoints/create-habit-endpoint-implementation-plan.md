# API Endpoint Implementation Plan: Create Habit

## 1. Przegląd punktu końcowego
Endpoint służy do tworzenia nowego nawyku użytkownika. Pozwala dodać maksymalnie 20 nawyków na użytkownika, przy czym walidacja danych (długość pól, zakresy liczbowych wartości, maska dni tygodnia) odzwierciedla zarówno wymagania warstwy biznesowej, jak i ograniczenia bazy danych.

## 2. Szczegóły żądania
- **Metoda HTTP:** POST
- **Struktura URL:** `/api/v1/habits`
- **Parametry:**
    - **Wymagane:**
        - `title` – string, maksymalnie 80 znaków
        - `type` – int, np. 1 (Start) lub 2 (Stop)
        - `completionMode` – int, np. 1 (Binary), 2 (Quantitative) lub 3 (Checklist)
        - `daysOfWeekMask` – int, wartość od 1 do 127 (reprezentuje maskę bitową dni tygodnia)
        - `targetValue` – int, w zakresie 1..100 (zgodnie ze specyfikacją API, chociaż w DB jest zakres 1..1000)
    - **Opcjonalne:**
        - `description` – string, maksymalnie 280 znaków (dla żądania, a w bazie do 1000 znaków)
        - `targetUnit` – string, maksymalnie 32 znaki
        - `deadlineDate` – string w formacie YYYY-MM-DD

- **Request Body Przykład:**
  ```json
  {
      "title": "Czytanie książki",
      "description": "Codzienne czytanie 10 stron",
      "type": 1,
      "completionMode": 2,
      "daysOfWeekMask": 85,
      "targetValue": 10,
      "targetUnit": "pages",
      "deadlineDate": "2026-03-31"
  }
  ```

## 3. Wykorzystywane typy
- **DTO:**
    - `CreateHabitRequest` – rekord zdefiniowany w `CreateHabitRequest.cs`
- **Command Model:**
    - Komenda przekazywana do Handlera, która może być rozszerzeniem DTO (np. `CreateHabitCommand`) uwzględniająca dodatkowe dane kontekstowe (np. UserId z Identity)
- **Odpowiedź (Response DTO):**
    - `HabitResponse` – reprezentujący utworzony nawyk z dodatkowymi polami jak `id`, `createdAtUtc` itp.

## 4. Przepływ danych
1. **Przychodzące żądanie:**
    - Uwierzytelniony użytkownik wysyła żądanie POST na `/api/v1/habits` wraz z danymi w formacie JSON.
2. **Walidacja:**
    - Walidacja długości pól (np. `title` ≤80 znaków, `description` ≤280 znaków) oraz zakresów liczbowych (maski dni, targetValue).
    - Sprawdzenie, czy użytkownik nie przekroczył limitu 20 nawyków.
3. **Przekazanie do Command Handlera:**
    - Po poprawnej walidacji, żądanie jest mapowane do obiektu komendy (CreateHabitCommand) i wysyłane do odpowiadającego handlera.
4. **Logika biznesowa:**
    - Handler wykonuje sprawdzenie limitu nawyków dla danego użytkownika.
    - Jeśli limit nie jest przekroczony, następuje zapis nawyku w bazie danych przy wykorzystaniu EF Core.
5. **Odpowiedź:**
    - Po utworzeniu nawyku, zwracany jest response DTO `HabitResponse` ze statusem HTTP 201.
    - W przypadku niepowodzenia walidacji, zwracany jest kod 400 lub 409 (gdy limit nawyków jest osiągnięty).

## 5. Względy bezpieczeństwa
- **Uwierzytelnienie i autoryzacja:** Endpoint wymaga autoryzacji, co zapewnia middleware ASP.NET Core Identity.
- **Walidacja wejścia:** Dane wejściowe są walidowane przed przetwarzaniem (format, długość, zakresy) aby zapobiec SQL Injection oraz nieprawidłowym danym.
- **Ochrona przed przekroczeniem limitu:** Logika sprawdzająca liczbę istniejących nawyków dla danego użytkownika zapobiega przekroczeniu ustalonego limitu.

## 6. Obsługa błędów
- **400 Bad Request:** Zwracany w przypadku niepoprawnych danych wejściowych – błędy walidacji, nieprawidłowe zakresy lub formaty.
- **401 Unauthorized:** Gdy żądanie jest wysyłane przez niezautoryzowanego użytkownika.
- **409 Conflict:** Gdy użytkownik próbuje dodać nawyk, mimo że osiągnięto limit dozwolonych (20 nawyków).
- **Globalny handler błędów:** Użycie globalnego mapowania ProblemDetails zapewniającego zachowanie zgodne z RFC 7807.

## 7. Rozważania dotyczące wydajności
- **Indeksowanie:** Korzystanie z indeksu `IX_Habits_UserId_CreatedAtUtc` zapewnia szybkie wyszukiwanie nawyków użytkownika.
- **Efektywne zapisy:** Użycie EF Core, `AsNoTracking` dla zapytań oraz projekcji do DTO, aby ograniczyć obciążenie w scenariuszach odczytu.
- **Ograniczenie liczby rekordów:** Sprawdzenie limitu 20 nawyków odbywa się w obrębie jednej operacji, co zabezpiecza przed potencjalnymi przeciążeniami.

## 8. Etapy wdrożenia
1. **Analiza i przygotowanie:**
    - Przegląd dokumentów specyfikacji (specyfikacja API, struktura bazy danych, typy DTO).
    - Zidentyfikowanie pól wymaganych i opcjonalnych według `CreateHabitRequest.cs`.

2. **Implementacja walidacji wejścia:**
    - Sprawdzenie długości `title` (max 80) oraz `description` (max 280).
    - Walidacja zakresów liczbowych dla `daysOfWeekMask` (1..127) oraz `targetValue` (1..100).
    - Weryfikacja formatu daty dla `deadlineDate`.
    - Dodanie sprawdzenia limitu ilości nawyków dla danego użytkownika.

3. **Stworzenie Command Handlera:**
    - Utworzenie klasy `CreateHabitCommand` (ewentualnie mapowanie CreateHabitRequest) zawierającej dodatkowe informacje (np. UserId).
    - Implementacja handlera, który wykonuje logikę biznesową (sprawdzenie, zapis do bazy).
    - Implementacja obsługi potencjalnych błędów (konflikt z limitem nawyków).

4. **Integracja z Minimal API:**
    - Modyfikacja endpointu w `HabitEndpoints.cs` aby wywołać odpowiedni handler poprzez dyspozytora komend.
    - Mapowanie odpowiedzi z handlera do `HabitResponse` i odpowiednich kodów HTTP (201, 400, 401, 409).

5. **Testowanie i walidacja:**
    - Stworzenie testów jednostkowych dla Command Handlera.
    - Scenariusze testowe obejmujące poprawne żądanie, błędne dane wejściowe oraz przekroczenie limitu nawyków.
    - Testy integracyjne sprawdzające autoryzację i pełny przepływ danych.

6. **Dokumentacja i Code Review:**
    - Uaktualnienie dokumentacji API.
    - Review kodu i walidacja zgodności ze stylem kodowania.

7. **Deployment:**
    - Wdrożenie zmian na środowisku testowym (development/staging).
    - Monitorowanie logów i ewentualna korekta błędów przed wdrożeniem na produkcję.