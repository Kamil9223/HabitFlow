# Dokument wymagań produktu (PRD) - HabitFlow

## 1. Przegląd produktu

1.1. Cel produktu
- HabitFlow to prosty, webowy tracker nawyków wspierający użytkowników w budowaniu i utrzymywaniu korzystnych zachowań oraz eliminowaniu złych nawyków.
- MVP dostarcza: rejestrację/logowanie, CRUD nawyków, check-in dzienny, kalendarz readonly, pojedynczy wykres postępów (rolling success rate 7/30 dni), podstawowe powiadomienia motywacyjne generowane przez AI wyświetlane w aplikacji.

1.2. Grupa docelowa
- B2C, osoby chcące wdrażać/monitorować nawyki (sport, nauka, sen itp.), w szczególności początkujący użytkownicy trackerów nawyków.
- Urządzenia: przeglądarka desktop/mobile (web app). Brak natywnych aplikacji w MVP.

1.3. Wyróżniki
- Ujednolicony model nawyków dla dwóch typów: „zacząć robić” oraz „przestać robić”.
- Prosta, czytelna wizualizacja postępów (rolling success rate 7/30).
- Motywacyjne powiadomienia in-app, generowane przez AI, wyzwalane przy „miss due”.

1.4. Zakres MVP (wysoki poziom)
- MUST: Auth (rejestracja, logowanie, weryfikacja e-mail, reset hasła), CRUD nawyków, check-in, kalendarz readonly, uproszczone powiadomienia AI, wykres rolling 7/30.
- COULD: Rate-limiting dla powiadomień, drobne ulepszenia UX, proste telemetryczne liczniki aktywacji.

1.5. Definicje kluczowe
- success_rate: (suma daily_score w oknie) / (liczba_dni_zaplanowanych_w_oknie). Gdy brak zaplanowanych dni, success_rate = 0.
- daily_score: dzienny wkład do sukcesu obliczany na podstawie ActualValue, TargetValue, HabitType i CompletionMode:
  - **Binary (0/1)**: daily_score = (ActualValue > 0 ? 1.0 : 0.0)
  - **Quantitative**: ratio = ActualValue / TargetValue (clamped 0-1)
    - Dla typu Start: daily_score = ratio (np. 7/10 stron = 0.7)
    - Dla typu Stop: daily_score = 1 - ratio (np. 1/3 naruszeń = 0.67)
- Próg sukcesu nawyku: 75% realizacji do dnia deadline'u (o ile zdefiniowany).
- Harmonogram: dni tygodnia + wartość docelowa (TargetValue, 1-1000) z opcjonalną jednostką (TargetUnit, np. "pages", "meals").
- CompletionMode: sposób rozliczania postępu nawyku (Binary, Quantitative).

1.6. Założenia operacyjne MVP
- Strefa czasu użytkownika: do obliczeń przyjmujemy dobowy zakres 00:00–23:59 czasu lokalnego użytkownika.
- Check-in: jednorazowy wpis dzienny z wartością liczbową 0..powtórzenia (dla „zacząć”) albo 0..powtórzenia naruszeń (dla „przestać”). Brak edycji po zapisie. Uzupełnianie do 7 dni wstecz.
- Deadline nawyku: pole opcjonalne. Jeżeli brak deadline’u, 75% sukcesu interpretujemy na podstawie rolling success rate (okna 7/30 dni).
- Powiadomienia AI: w aplikacji (sekcja Notifications), trigger „miss due”; gdy dzień wykonany zgodnie z planem – brak notyfikacji tego dnia.

1.7. Ramy wykonawcze
- Organizacja: 1 osoba, 2 sprinty po 2 tygodnie.
- DoD: CRUD, auth, jedna funkcja z logiką biznesową, działający sensowny test (unit lub E2E), CI/CD (GitHub Actions).
- E2E: ścieżka „Rejestracja → utworzenie nawyku → check-in → kalendarz i wykres → notyfikacja (miss)”.

## 2. Problem użytkownika

2.1. Kontekst
- Użytkownicy mają trudność w utrzymaniu konsekwencji przy wdrażaniu i odrzucaniu nawyków, brakuje im prostego narzędzia do planowania i monitoringu z jasną informacją zwrotną oraz motywacją.

2.2. Kluczowe potrzeby
- Szybkie utworzenie nawyku z prostym harmonogramem.
- Czytelne odhaczanie codziennych kroków i natychmiastowa informacja o postępie.
- Zrozumiała metryka skuteczności (success_rate) i prosta wizualizacja trendu.
- Delikatna, kontekstowa motywacja, zwłaszcza po „miss due”.

2.3. Obecne bariery
- Złożone aplikacje z nadmiarem funkcji, brak spójnej metryki, brak jasnych podpowiedzi „co dzisiaj zrobić”, brak motywacyjnych przypomnień w odpowiednim momencie.

## 3. Wymagania funkcjonalne

Nota: Priorytet oznaczony jako MUST/COULD zgodnie z zakresem MVP.

F-001 Autoryzacja i uwierzytelnianie (MUST)
- Rejestracja konta z weryfikacją e-mail (jednorazowy link).
- Logowanie (hasło, minimalnie 8 znaków).
- Reset hasła via e-mail.
- Pojedyncza rola: user.
- Sesja użytkownika zabezpiecza dostęp do zasobów własnych.

F-002 Zarządzanie profilem – strefa czasu (MUST)
- Doba liczona 00:00–23:59 czasu lokalnego użytkownika.

F-003 Model nawyku i CRUD (MUST)
- Typ: start (zacząć) lub stop (przestać).
- Pola: tytuł (<= 80 znaków), opis (<= 280 znaków), harmonogram (dni tygodnia), powtórzenia/dzień (1..100, opcjonalne), deadline, data utworzenia.
- Operacje: tworzenie, odczyt, edycja, usuwanie (hard delete).

F-004 Ekran „Dziś” (MUST)
- Lista dzisiejszych kroków wg harmonogramu i powtórzeń.
- Szybkie przejście do check-in dla każdego nawyku.

F-005 Check-in dzienny (MUST)
- Jednorazowy wpis per nawyk i dzień:
  - start: liczba wykonanych w zakresie 0..powtórzenia.
  - stop: liczba naruszeń w zakresie 0..powtórzenia (obcinane do maks.).
- Uzupełnianie historii do 7 dni wstecz. Po zapisie brak edycji.

F-006 Kalendarz readonly per nawyk (MUST)
- Przyszłość: plan (neutralny).
- Przeszłość: wykonane (zielony), niewykonane (czerwony), częściowo wykonane wg reguł sukcesu dnia.
- Zmiany tylko poprzez CRUD i check-in.

F-007 Obliczanie success_rate i próg 75% (MUST)
- success_rate wg definicji: wykonane/zaplanowane (0, gdy brak zaplanowanych).
- Dla typu stop: dzienny wkład 1 - naruszenia/zaplanowane, z naruszenia <= zaplanowane.
- Próg sukcesu nawyku: 75% do deadline’u (jeśli ustawiony). Bez deadline’u – interpretacja w oknach rolling (patrz F-008).

F-008 Wykres postępu – rolling 7/30 (MUST)
- Jedna linia prezentująca rolling success rate dla 7 lub 30 dni (przełącznik).
- Tooltip: wykonane/zaplanowane w oknie.

F-009 Powiadomienia AI w aplikacji (MUST)
- Zakładka Notifications z listą wiadomości.
- Trigger: „miss due” (dzień zaplanowany niewykonany). Miss generuje pojedynczą wiadomość na dany dzień i nawyk.
- Treści generowane kontekstowo względem nawyku; w przypadku braku dostępności AI – fallback na stałe szablony (patrz F-011).
- Brak e-mail/SMS/push poza aplikacją.

F-010 Usuwanie danych (MUST)
- Hard delete nawyków i konta.

F-011 Obsługa niedostępności AI (MUST)
- Timeout i obsługa błędów generatora; użycie stałych tekstów i log błędu.

F-012 Limity i walidacje (MUST)
- Max liczba nawyków na użytkownika: 20.
- Zakres powtórzeń: 1..100/dzień.
- Tytuł <= 80 znaków; opis <= 280 znaków.
- Jedna próba check-in na dzień per nawyk; kolejne próby blokowane.

F-013 Rate-limit powiadomień (COULD)
- W S2 planowane, lecz poza twardym MVP: ograniczenie spamu dla często wykonujących (np. min. odstęp 3 dni między pozytywnymi komunikatami).
- Miss due zawsze generuje komunikat jednorazowy danego dnia.

F-014 Telemetria podstawowa (COULD)
- Liczniki aktywacji: ≥1 nawyk i ≥1 check-in w 24–48h.
- Zdarzenia: utworzenie nawyku, check-in, miss due.

Wymagania niefunkcjonalne (skrót)
- Dostępność i UX: prosta i szybka interakcja (<= 2–3 kliknięcia do check-in z ekranu „Dziś”).
- Wydajność: czas generowania widoku „Dziś” ≤ 500 ms przy 20 nawykach.
- Bezpieczeństwo: sesje zabezpieczone, weryfikacja e-mail, reset hasła; lockout po wielu nieudanych logowaniach – poza MVP (opcjonalne).

## 4. Granice produktu

4.1. W zakresie MVP (MUST)
- Auth: rejestracja, logowanie, weryfikacja e-mail, reset hasła.
- CRUD nawyków (start/stop), harmonogram (dni tygodnia) + powtórzenia/dzień.
- Check-in jednorazowy per dzień, uzupełnianie do 7 dni wstecz, brak edycji po zapisie.
- Kalendarz readonly; przyszłość=plan, przeszłość: zielony/czerwony (wg dziennego wyniku).
- Obliczanie success_rate, próg 75% do deadline’u.
- Wykres rolling success rate 7/30 z tooltipem.
- Powiadomienia AI w aplikacji (trigger: miss due), fallback tekstowy.
- Hard delete konta i nawyków.
- Ekran „Dziś”.

4.2. Poza MVP
- Integracje zewnętrzne (Google Calendar, eksport CSV).
- Rozbudowane wizualizacje/wykresy.
- Powiadomienia poza aplikacją (e-mail/SMS/push).
- System osiągnięć, nagród, gamifikacja.
- Zaawansowany rate-limit i personalizacja powiadomień (częściowo plan w S2).
- Blokada konta po X błędnych logowaniach – poza MVP (może być rozważana).

4.3. Założenia doprecyzowujące (dla wykonalności MVP)
- Check-in jako jednorazowy wpis liczbowy (0..powtórzenia) na dzień.
- Strefa czasu i doba: 00:00–23:59 czasu lokalnego użytkownika.
- Deadline opcjonalny; brak deadline’u → sukces oceniany przez rolling 7/30.

4.4. Kwestie nierozstrzygnięte (do decyzji/rozszerzeń)
- Ostateczne reguły rate-limit powiadomień pozytywnych (S2 lub po MVP).
- Pauzowanie nawyków (nie liczone do mianownika) – poza MVP.
- Polityka haseł i ewentualny lockout (poza MVP).
- Minimalna telemetria (COULD) i ew. dashboard wewnętrzny – po MVP.
- Operacyjne backup/restore – poza MVP (wewnętrzne procesy zespołu).

## 5. Historyjki użytkowników

US-001 Rejestracja z weryfikacją e-mail (MUST)
- Opis: Jako nowy użytkownik chcę zarejestrować konto i potwierdzić e-mail, aby móc bezpiecznie korzystać z aplikacji.
- Kryteria akceptacji:
  - Po podaniu e-mail i hasła (≥8 znaków) wysyłany jest jednorazowy link weryfikacyjny.
  - Kliknięcie linku aktywuje konto i umożliwia logowanie.
  - Niezweryfikowany e-mail blokuje logowanie i tworzenie danych.
  - Komunikaty o błędach są czytelne (zajęty e-mail, niepoprawny format).

US-002 Logowanie (MUST)
- Opis: Jako użytkownik chcę się zalogować, aby uzyskać dostęp do swoich nawyków.
- Kryteria akceptacji:
  - Logowanie wymaga poprawnego e-maila i hasła oraz zweryfikowanego adresu e-mail.
  - Po zalogowaniu widzę ekran „Dziś”.
  - Błędne dane pokazują komunikat o błędzie bez ujawniania, który element jest niepoprawny.

US-003 Reset hasła (MUST)
- Opis: Jako użytkownik, który zapomniał hasła, chcę zresetować hasło poprzez e-mail.
- Kryteria akceptacji:
  - Formularz „Zapomniałem hasła” wysyła link resetu.
  - Link jednorazowy wygasa po określonym czasie (np. 60 min).
  - Po ustawieniu nowego hasła mogę się zalogować.

US-004 Ustawienie strefy czasu (MUST)
- Opis: Jako użytkownik chcę ustawić lub zmienić strefę czasową, aby harmonogram był liczony poprawnie.
- Kryteria akceptacji:
  - Domyślna strefa ustalana automatycznie i widoczna w profilu.
  - Zmiana strefy wpływa na obliczanie „dzisiaj” od następnej doby.
  - Wszystkie widoki używają czasu lokalnego użytkownika.

US-005 Utworzenie nawyku – typ „zacząć” (MUST)
- Opis: Jako użytkownik chcę stworzyć nawyk „zacząć” z harmonogramem i powtórzeniami/dzień.
- Kryteria akceptacji:
  - Pola obowiązkowe: tytuł, typ, dni tygodnia; powtórzenia opcjonalne (domyślnie 1).
  - Walidacje: tytuł <=80, opis <=280, powtórzenia 1..100.
  - Utworzony nawyk pojawia się na liście i na ekranie „Dziś” zgodnie z planem.

US-006 Utworzenie nawyku – typ „przestać” (MUST)
- Opis: Jako użytkownik chcę stworzyć nawyk „przestać” z harmonogramem i limitem „naruszeń”.
- Kryteria akceptacji:
  - Pola jak wyżej; powtórzenia/dzień oznacza maksymalną liczbę dozwolonych naruszeń (limit).
  - Logika dzienna: wkład do success_rate = 1 - naruszenia/zaplanowane.

US-007 Edycja nawyku (MUST)
- Opis: Jako użytkownik chcę edytować szczegóły nawyku.
- Kryteria akceptacji:
  - Mogę zmienić tytuł, opis, dni tygodnia, powtórzenia, opcjonalny deadline.
  - Zmiana dotyczy przyszłych dni; zapisane historyczne check-iny pozostają bez zmian.

US-008 Usunięcie nawyku (MUST)
- Opis: Jako użytkownik chcę trwale usunąć nawyk.
- Kryteria akceptacji:
  - Operacja wymaga potwierdzenia (np. modal).
  - Po usunięciu nawyk znika z listy, kalendarza i statystyk (hard delete).

US-009 Lista nawyków (MUST)
- Opis: Jako użytkownik chcę przeglądać wszystkie swoje nawyki.
- Kryteria akceptacji:
  - Widoczne: tytuł, typ, status (np. skrót postępu), podstawowe akcje (edytuj, usuń).
  - Dla >20 nawyków wyświetlany komunikat o osiągnięciu limitu.

US-010 Ekran „Dziś” (MUST)
- Opis: Jako użytkownik chcę zobaczyć dzisiejsze kroki do wykonania.
- Kryteria akceptacji:
  - Lista kroków wg zaplanowanych na dziś nawyków.
  - Szybkie przejście do check-in; elementy nie na dziś są ukryte lub odfiltrowane.

US-011 Check-in – typ „zacząć” (MUST)
- Opis: Jako użytkownik chcę odnotować dzienny postęp liczbowy dla nawyku „zacząć”.
- Kryteria akceptacji:
  - Wpis 0..powtórzenia; wartości >powtórzenia są obcięte do maks.
  - Tylko jeden zapis na dzień; kolejne próby odrzucone z komunikatem.
  - Po zapisie dzień w kalendarzu zmienia kolor zgodnie z wynikiem.

US-012 Check-in – typ „przestać” (MUST)
- Opis: Jako użytkownik chcę odnotować dzienne naruszenia.
- Kryteria akceptacji:
  - Wpis 0..powtórzenia; wartości >powtórzenia są obcięte.
  - Wkład dzienny do success_rate = 1 - naruszenia/zaplanowane.
  - Jeden zapis na dzień; po zapisie brak edycji.

US-013 Uzupełnienie wstecz (MUST)
- Opis: Jako użytkownik chcę uzupełnić brakujący wpis wstecznie (max 7 dni).
- Kryteria akceptacji:
  - System pozwala na wybór daty z ostatnich 7 dni.
  - Po zapisie wpisu historycznego brak możliwości edycji.
  - Próba starszej daty jest odrzucona z komunikatem.

US-014 Brak edycji po zapisie (MUST)
- Opis: Jako użytkownik nie mogę edytować zapisanego check-inu.
- Kryteria akceptacji:
  - Dla daty z istniejącym check-inem przycisk edycji jest niedostępny.
  - API odrzuca próby nadpisania istniejącego wpisu.

US-015 Kalendarz readonly (MUST)
- Opis: Jako użytkownik chcę zobaczyć kalendarz statusów.
- Kryteria akceptacji:
  - Przyszłe dni jako plan (neutralny), przeszłe: zielony/czerwony wg wyniku.
  - Kliknięcie dnia otwiera szczegóły (read-only: wynik, zaplanowane, notatka jeśli dostępna).

US-016 Wykres rolling success rate 7/30 (MUST)
- Opis: Jako użytkownik chcę przełączyć okno 7/30 i zobaczyć trend.
- Kryteria akceptacji:
  - Przełącznik 7/30 przeładowuje serię danych.
  - Tooltip pokazuje wykonane/zaplanowane dla okna.

US-017 Powiadomienie AI – miss due (MUST)
- Opis: Jako użytkownik chcę otrzymać motywacyjny komunikat przy niewykonaniu zaplanowanego dnia.
- Kryteria akceptacji:
  - Dla każdego nawyku i dnia z miss due generowany jest jeden komunikat.
  - Gdy dzień jest wykonany, powiadomienie nie jest generowane.
  - Treść pojawia się w zakładce Notifications.

US-018 Przegląd powiadomień (MUST)
- Opis: Jako użytkownik chcę przeglądać historię powiadomień.
- Kryteria akceptacji:
  - Lista powiadomień uporządkowana chronologicznie.
  - Widoczne: tytuł nawyku, data, treść.

US-019 Usunięcie konta (MUST)
- Opis: Jako użytkownik chcę trwale usunąć konto i wszystkie dane.
- Kryteria akceptacji:
  - Operacja wymaga potwierdzenia (np. wpisanie „DELETE”).
  - Po potwierdzeniu następuje hard delete i wylogowanie.

US-020 Wylogowanie (MUST)
- Opis: Jako użytkownik chcę się wylogować.
- Kryteria akceptacji:
  - Sesja zostaje zakończona; dostęp do zasobów wymaga ponownego logowania.

US-021 Walidacje i limity (MUST)
- Opis: Jako użytkownik otrzymuję jasne komunikaty przy przekroczeniu limitów lub błędach walidacji.
- Kryteria akceptacji:
  - Limity: 20 nawyków, tytuł <=80, opis <=280, powtórzenia 1..100.
  - Komunikaty błędów są jednoznaczne i wskazują pole/problem.

US-022 Fallback powiadomień AI (MUST)
- Opis: Jako użytkownik nadal otrzymuję przydatny komunikat, gdy AI jest niedostępne.
- Kryteria akceptacji:
  - Gdy generator AI zwraca błąd/timeout, stosowany jest stały szablon z kontekstem nawyku.
  - Zdarzenie jest logowane technicznie.

US-023 Bezpieczny dostęp do zasobów (MUST)
- Opis: Jako użytkownik chcę, aby tylko ja miał dostęp do moich nawyków i wpisów.
- Kryteria akceptacji:
  - Każde wywołanie API weryfikuje tożsamość i własność zasobów.
  - Próby dostępu do cudzych zasobów zwracają błąd autoryzacji (403/404).

US-024 Interpretacja bez deadline’u (MUST)
- Opis: Jako użytkownik bez zdefiniowanego deadline’u chcę widzieć sensowny postęp.
- Kryteria akceptacji:
  - Wykres i wskaźniki pokazują rolling 7/30.
  - Próg 75% sygnalizowany w kontekście rolling, nie wymaga osiągnięcia na „dany dzień”.

US-025 Dwuklikowy przepływ check-in z „Dziś” (COULD)
- Opis: Jako użytkownik chcę wykonać check-in w maks. 2–3 akcjach z ekranu „Dziś”.
- Kryteria akceptacji:
  - Klik „Check-in” → wprowadzenie wartości → „Zapisz”.
  - Łączny czas interakcji krótszy niż 5 sekund przy stabilnym łączu.

US-026 E2E ścieżka krytyczna (MUST)
- Opis: Jako zespół chcemy mieć test E2E „Rejestracja → nawyk → check-in → kalendarz/wykres → notyfikacja (miss)”.
- Kryteria akceptacji:
  - Test przechodzi na CI/CD.
  - Przypadek „miss due” generuje powiadomienie w zakładce Notifications.

Uwagi:
- Każda historia jest testowalna przez UI/API i posiada jednoznaczne kryteria.
- Historie COULD nie są wymagane do releasu MVP, ale mile widziane w S2.

## 6. Metryki sukcesu

6.1. Produktowe
- Na poziomie nawyku: osiągnięcie 75% success_rate do deadline’u (o ile zdefiniowany).
- Na poziomie użytkownika: odsetek użytkowników z ≥1 nawykiem i ≥1 check-in w ciągu 24–48 godzin od rejestracji (aktywacja).

6.2. Adopcja i aktywność
- 90% aktywnych użytkowników posiada zdefiniowane nawyki i śledzi swoje postępy (min. jeden check-in tygodniowo).
- Retencja 7-dniowa: odsetek użytkowników, którzy wykonali min. 2 check-iny w pierwszym tygodniu.

6.3. Jakość i niezawodność
- Skuteczność weryfikacji e-mail (≥98% dostarczonych linków).
- Czas renderu ekranu „Dziś” ≤ 500 ms przy ≤20 nawykach.
- Odsetek błędów krytycznych w check-in < 0,1%.

6.4. Powiadomienia
- Pokrycie „miss due”: ≥95% przypadków generuje powiadomienie w aplikacji w ciągu ≤5 min od spełnienia warunku (lub przy następnym wejściu do aplikacji).
- Fallback AI użyty w <10% komunikatów (docelowo).

6.5. Testy i proces
- Test E2E ścieżki krytycznej przechodzi stabilnie na CI/CD.
- Pokrycie co najmniej jednej funkcji biznesowej testami jednostkowymi.

Lista kontrolna PRD:
- Każda historia użytkownika jest testowalna (tak).
- Kryteria akceptacji są jasne i konkretne (tak).
- Zestaw historii wystarcza do zbudowania pełnego MVP (tak).
- Uwzględniono wymagania dotyczące uwierzytelniania i autoryzacji (tak: US-001, US-002, US-003, US-023).
