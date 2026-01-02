# API Endpoint Implementation Plan: Get Habit Details by Id

## 1. Przegląd punktu końcowego
Endpoint GET /api/v1/habits/{id} umożliwia pobranie szczegółowych informacji o konkretnym nawyku użytkownika. Endpoint sprawdza, czy nawyk należy do aktualnie zalogowanego użytkownika i zwraca dane w postaci obiektu HabitResponse.

## 2. Szczegóły żądania
- **Metoda HTTP:** GET
- **Struktura URL:** /api/v1/habits/{id}
- **Parametry:**
  - **Wymagane:** 
    - id (int) – Unikalny identyfikator nawyku, przekazywany jako parametr ścieżki.
  - **Opcjonalne:** Brak.
- **Request Body:** Brak

## 3. Wykorzystywane typy
- **DTO:** HabitResponse – zawiera szczegółowe informacje o nawyku (np. Id, UserId, Title, Description, Type, DaysOfWeekMask, CompletionMode, TargetValue, TargetUnit, DeadlineDate, CreatedAtUtc).
- **Query Model/Handler:** GetHabitQuery i GetHabitQueryHandler (logika wyodrębniona do oddzielnego handlera).

## 4. Szczegóły odpowiedzi
- **Kody statusu:**
  - **200 OK:** Jeśli nawyk został znaleziony i użytkownik jest jego właścicielem. Odpowiedź zawiera obiekt HabitResponse.
  - **401 Unauthorized:** Jeśli użytkownik nie jest zalogowany lub nie ma uprawnień.
  - **404 Not Found:** Jeśli nawyk o podanym id nie istnieje lub nie należy do aktualnego użytkownika.

## 5. Przepływ danych
1. Odbieranie requestu z parametrem "id" z URL.
2. Weryfikacja uwierzytelnienia użytkownika i pobranie UserId z kontekstu.
3. Przekazanie zapytania GetHabitQuery do handlera (GetHabitQueryHandler), który wykonuje zapytanie w bazie danych przy użyciu Entity Framework Core.
4. Projekcja wyników zapytania do obiektu HabitResponse przy użyciu zapytań AsNoTracking i Select.
5. Zwracanie odpowiedzi HTTP 200 z danymi lub odpowiedniego błędu (401/404).

## 6. Względy bezpieczeństwa
- Endpoint wymaga autoryzacji, gwarantując, że tylko zalogowani użytkownicy mają dostęp.
- Weryfikacja, czy pobrany nawyk należy do aktualnie zalogowanego użytkownika (sprawdzenie pola UserId).
- Walidacja parametru "id" – musi być większy od zera.
- Zapobieganie atakom SQL injection poprzez użycie parametrów w zapytaniach EF Core.

## 7. Obsługa błędów
- **401 Unauthorized:** Zwracany, gdy użytkownik nie jest uwierzytelniony.
- **404 Not Found:** Zwracany, gdy nawyk o podanym id nie istnieje lub nie jest własnością użytkownika.
- **500 Internal Server Error:** Obsługa nieoczekiwanych wyjątków z rejestrowaniem błędów do systemu logowania błędów.

## 8. Rozważania dotyczące wydajności
- Używanie zapytań AsNoTracking do odczytów w celu zwiększenia wydajności.
- Projekcja danych do DTO poprzez Select, aby pobierać tylko niezbędne kolumny.
- Optymalizacja zapytań przy użyciu wcześniej skonfigurowanych indeksów (np. IX_Habits_UserId_CreatedAtUtc).

## 9. Etapy wdrożenia
1. Utworzenie modelu DTO HabitResponse oraz ewentualnie modelu zapytania GetHabitQuery.
2. Implementacja handlera GetHabitQueryHandler, który pobiera dane z bazy i dokonuje walidacji własności.
3. Aktualizacja mapowania endpointu w HabitEndpoints.cs, aby wywoływał handler zamiast zwracać tymczasowy kod StatusCode(501).
4. Wdrożenie walidacji parametru "id" oraz sprawdzenia zgodności UserId.
5. Dodanie mechanizmu obsługi błędów i rejestrowania wyjątków.
6. Utworzenie testów jednostkowych dla nowego endpointu.
7. Przegląd i testowanie zmian przy użyciu narzędzi CI/CD (np. GitHub Actions).
