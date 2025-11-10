Jesteś doświadczonym menedżerem produktu, którego zadaniem jest stworzenie kompleksowego dokumentu wymagań produktu (PRD) w oparciu o poniższe opisy:

<project_description>
# Aplikacja - HabitFlow (MVP)

## Główny problem
Wprowadzanie nowych korzystnych nawyków jest trudne. Inteligentny tracker nawyków HabitFlow pozwala użytkownikowi tworzyć i śledzić nawyki (sport, nauka, sen itp.) z analizą postępów.
Dzięki wykorzystaniu potencjału, kreatywności i wiedzy AI, HabitFlow pozwala także motywować użytkownika wysyłając motywacyjne powiadomienia.

## Najmniejszy zestaw funkcjonalności
- Zapisywanie, odczytywanie, przeglądanie i usuwanie nawyków do wypracowania
- Prosty system kont użytkowników z logowaniem i rejestracją
- Różne typy nawyku:
    - nawyk typu "chcę coś zacząć robić"
    - nawyk typu "chcę coś przestać robić"
- Prosty kalendarz nawyków
- Powiadomienia motywacyjne wykorzystujące AI które adekwatnie do nawyku wysyłają powiadomienie o motywującej treści w kontekście nawyku (powiadomienia w aplikacji)
- Prosty wykres postępów (jeden typ)

## Co NIE wchodzi w zakres MVP
- Integracja z innymi systemami jak np Google Calendar / pliki CSV eksportujące dane.
- Bogate wykresy, wizualizacja danych
- powiadomienia live przez email, sms czy inne formy zewnętrznej komunikacji
- system osiągnięć, nagród odhaczania zrealizowanych celów

## Kryteria sukcesu
- 90% użytkowników posiada zdefiniowane nawyki i śledzi swóje postępy
</project_description>

<project_details>
<conversation_summary>
<decisions>

1. Target & platform: B2C, start jako aplikacja web (bez natywnych aplikacji).
2. Typy nawyków: „zacząć robić” oraz „przestać robić”; mierzone w ten sam sposób.
3. Definicja success_rate: `success_rate = (liczba_zaplanowanych_wykonań_w_okresie > 0 ? liczba_wykonanych / zaplanowane : 0)`.
4. Próg sukcesu nawyku: 75% realizacji do dnia deadline’u.
5. Harmonogram nawyku: konfiguracja dni tygodnia + opcjonalna liczba „powtórzeń” na dzień (np. 10 stron/dzień).
6. Naruszenia (dla liczb powtórzeń): dzienna formuła `1 - naruszenia/zaplanowane`, przy czym gdy `naruszenia > zaplanowane`, wtedy `naruszenia = zaplanowane`. Jeśli naruszenie > 0 to wtedy w success_rate to należy uwzględnić, czyli dla dnia gdzie naruszenie > 0 to  jako "wykonany" za ten dzień liczymy 1 - naruszenie
7. Kalendarz: readonly, per nawyk; przyszłe kroki=plan, przeszłość: wykonane (zielony), niewykonane (czerwony). Zmiany wynikają z operacji CRUD i check-in.
8. Edycja historii: użytkownik może uzupełnić maks. 7 dni wstecz; po zapisie brak edycji wpisu.
9. Powiadomienia AI: tekstowe, wyświetlane wyłącznie w aplikacji (zakładka Notifications). Trigger: „miss due”. Wykonania → komunikaty rzadsze; niewykonania → za każdym razem. (Bez e-mail/SMS/push poza aplikacją).
10. Auth: weryfikacja e-mail (link jednorazowy), reset hasła, jeden typ roli („user”).
11. Wykres: „rolling success rate” z przełącznikiem 7/30 dni, jedna linia, tooltip z wykonane/zaplanowane.
12. Usuwanie danych: twarde (konto i nawyki – hard delete). Eksport danych poza MVP.
13. Priorytetyzacja MVP: MUST = CRUD nawyków, kalendarz, 1 wykres, logowanie/rejestracja, podstawowe powiadomienia AI; reszta = COULD.
14. Testy: E2E scenariusz „Rejestracja → utworzenie nawyku → check-in → kalendarz i wykres”; dodatkowo Nice-to-have: test notyfikacji przy miss.
15. Organizacja pracy: 1 osoba, 2 sprinty po 2 tygodnie. DoD: CRUD, auth, jedna funkcja z logiką biznesową, działający sensowny test (unit lub E2E), CI/CD na GitHub Actions.
16. Plan sprintów:

* Sprint 1: Auth, model nawyku, CRUD, ekran „Dziś”, check-in, kalendarz readonly.
* Sprint 2: kalkulacja 75% success_rate, wykres 7/30, notyfikacje (oraz planowany rate-limit), E2E ścieżka z notyfikacją (miss).

</decisions>

<matched_recommendations>

1. Ujednolicenie schematu nawyku (typ, tytuł, opis, harmonogram, metryka, target, reguła sukcesu) → przyjęto i doprecyzowano (dni tygodnia + powtórzenia/dzień, 75% sukcesu).
2. Kalendarz readonly per nawyk, kolory stanów, wsteczne uzupełnianie z limitem → przyjęto (max 7 dni, bez późniejszej edycji).
3. Jednolity wykres „rolling success rate” 7/30 dni z tooltipem → przyjęto.
4. Minimalny Auth (weryfikacja e-mail + reset hasła) → przyjęto.
5. In-app, tekstowe powiadomienia AI z jasnym triggerem (miss due) → przyjęto; ograniczanie spamu odłożone/niekonsekwentne (patrz kwestie otwarte).
6. E2E ścieżka krytyczna w testach → przyjęto (włącznie z notyfikacją miss w Sprincie 2).
7. Twarde usuwanie danych i brak eksportu w MVP → przyjęto.
8. Zakres MVP w dwóch sprintach i DoD z CI/CD → przyjęto.
   </matched_recommendations>

<prd_planning_summary>
a. Główne wymagania funkcjonalne:

* Rejestracja/logowanie z weryfikacją e-mail i resetem hasła.
* CRUD nawyków w dwóch typach („zacząć”/„przestać”) z harmonogramem (dni tygodnia + powtórzenia).
* Check-in (zapis wykonania/naruszeń); możliwość uzupełnienia wstecz maks. 7 dni; brak edycji po zapisie.
* Kalendarz readonly per nawyk (plan → przyszłość, wykonane → zielony, niewykonane → czerwony).
* Obliczanie `success_rate` i próg sukcesu 75% do deadline’u.
* Wykres rolling success rate 7/30 dni z tooltipem (wykonane/zaplanowane).
* Powiadomienia AI w aplikacji (tekstowe) z triggerem miss due; komunikaty rzadsze przy regularnym wykonywaniu.
* Ekran „Dziś” (lista dzisiejszych kroków do odhaczenia).
* Twarde usuwanie konta i nawyków.

b. Kluczowe historie użytkownika / ścieżki:

* Rejestracja → logowanie → konfiguracja profilu (opcjonalnie) → utworzenie nawyku (typ, harmonogram, powtórzenia, deadline) → check-in dzienny → podgląd kalendarza i wykresu.
* „Przestać robić”: ustawienie harmonogramu i rejestrowanie naruszeń (z przeliczeniem dziennego wskaźnika).
* Odczyt powiadomień w aplikacji (sekcja Notifications) przy „miss due”.
* Uzupełnienie wstecz maks. 7 dni (pojedyncze wpisy); brak możliwości edycji po zapisie.
* Usunięcie nawyku lub konta (hard delete).

c. Kryteria sukcesu i metryki:

* Produktowe: osiągnięcie 75% success_rate dla nawyków do dnia deadline’u (na poziomie nawyku).
* Adopcja/aktywacja (MVP – wysoki poziom): odsetek użytkowników z ≥1 nawykiem i ≥1 check-in w 24–48h.
* Testowe: zaliczony E2E scenariusz „Rejestracja → nawyk → check-in → kalendarz/wykres → notyfikacja (miss)”.
* Procesowe: spełnienie DoD (CRUD, auth, 1 funkcja biznesowa, test, CI/CD).

d. Nierozwiązane obszary (skrót w sekcji poniżej) wpływają m.in. na szczegóły logiki notyfikacji, ramy danych oraz doprecyzowanie okien czasu i TZ.
</prd_planning_summary>

<unresolved_issues>

1. Ograniczanie spamu w notyfikacjach: w decyzjach MVP „nie zajmujemy się”, ale plan Sprintu 2 zawiera „rate-limit”. Potrzebna decyzja: czy rate-limit jest w MVP (S2), czy odkładamy poza MVP.
2. Strefy czasowe i okna dnia: brak jednoznacznego zdefiniowania TZ użytkownika i godzin „dnia” (00:00–23:59 vs. okna wykonania). Wpływa na rozliczanie „miss due”.
3. Deadline nawyku: czy jest globalny dla nawyku, czy opcjonalny? Co z cyklicznymi nawykami bez twardego deadline’u – jak liczyć 75%?
4. Zachowanie przy braku zaplanowanych dni (np. choroba/„pauza”): czy przewidujemy pauzowanie nawyku (nie liczy się do mianownika) w MVP?
5. „Powtórzenia” a granularność check-in: czy check-in dla 10 stron jest wielokrotny (1/10, 2/10, …) w ciągu dnia, czy jednorazowy z wartością liczbową (np. 6/10)?
6. Dostępność i fallback AI: co w przypadku niedostępności generatora (czas odpowiedzi, błąd) – czy stosujemy stałe teksty i log błędów w MVP (procesowo już zasugerowane, brak finalnej decyzji)?
7. Walidacje i limity: maksymalna liczba nawyków na użytkownika, maks. długość tytułu/opisu, sensowne granice liczby powtórzeń/dzień.
8. Bezpieczeństwo minimalne: czy wprowadzamy lockout po X błędnych logowaniach oraz zasady dla haseł (złożoność, długość)? Wspomniano tylko weryfikację e-mail i reset.
9. Telemetria minimalna: czy mierzymy aktywację (≥1 nawyk i ≥1 check-in do 24/48h) oraz podstawowe eventy (utworzenie nawyku, check-in, miss due) w MVP?
10. Kopie zapasowe i odtwarzanie: przy hard delete – czy potrzebne są wewnętrzne mechanizmy backup/restore (nawet tylko operacyjne) na czas MVP?
</unresolved_issues>
</conversation_summary>
</project_details>

Wykonaj następujące kroki, aby stworzyć kompleksowy i dobrze zorganizowany dokument:

1. Podziel PRD na następujące sekcje:
   a. Przegląd projektu
   b. Problem użytkownika
   c. Wymagania funkcjonalne
   d. Granice projektu
   e. Historie użytkownika
   f. Metryki sukcesu

2. W każdej sekcji należy podać szczegółowe i istotne informacje w oparciu o opis projektu i odpowiedzi na pytania wyjaśniające. Upewnij się, że:
    - Używasz jasnego i zwięzłego języka
    - W razie potrzeby podajesz konkretne szczegóły i dane
    - Zachowujesz spójność w całym dokumencie
    - Odnosisz się do wszystkich punktów wymienionych w każdej sekcji

3. Podczas tworzenia historyjek użytkownika i kryteriów akceptacji
    - Wymień WSZYSTKIE niezbędne historyjki użytkownika, w tym scenariusze podstawowe, alternatywne i skrajne.
    - Przypisz unikalny identyfikator wymagań (np. US-001) do każdej historyjki użytkownika w celu bezpośredniej identyfikowalności.
    - Uwzględnij co najmniej jedną historię użytkownika specjalnie dla bezpiecznego dostępu lub uwierzytelniania, jeśli aplikacja wymaga identyfikacji użytkownika lub ograniczeń dostępu.
    - Upewnij się, że żadna potencjalna interakcja użytkownika nie została pominięta.
    - Upewnij się, że każda historia użytkownika jest testowalna.

Użyj następującej struktury dla każdej historii użytkownika:
- ID
- Tytuł
- Opis
- Kryteria akceptacji

4. Po ukończeniu PRD przejrzyj go pod kątem tej listy kontrolnej:
    - Czy każdą historię użytkownika można przetestować?
    - Czy kryteria akceptacji są jasne i konkretne?
    - Czy mamy wystarczająco dużo historyjek użytkownika, aby zbudować w pełni funkcjonalną aplikację?
    - Czy uwzględniliśmy wymagania dotyczące uwierzytelniania i autoryzacji (jeśli dotyczy)?

5. Formatowanie PRD:
    - Zachowaj spójne formatowanie i numerację.
    - Nie używaj pogrubionego formatowania w markdown ( ** ).
    - Wymień WSZYSTKIE historyjki użytkownika.
    - Sformatuj PRD w poprawnym markdown.

Przygotuj PRD z następującą strukturą:

```markdown
# Dokument wymagań produktu (PRD) - {{app-name}}
## 1. Przegląd produktu
## 2. Problem użytkownika
## 3. Wymagania funkcjonalne
## 4. Granice produktu
## 5. Historyjki użytkowników
## 6. Metryki sukcesu
```

Pamiętaj, aby wypełnić każdą sekcję szczegółowymi, istotnymi informacjami w oparciu o opis projektu i nasze pytania wyjaśniające. Upewnij się, że PRD jest wyczerpujący, jasny i zawiera wszystkie istotne informacje potrzebne do dalszej pracy nad produktem.

Ostateczny wynik powinien składać się wyłącznie z PRD zgodnego ze wskazanym formatem w markdown, który zapiszesz w pliku .ai/prd.md