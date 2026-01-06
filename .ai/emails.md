# Emails - rejestracja i potwierdzenie

Ten dokument opisuje, jak dziala wysylka maili podczas rejestracji oraz jak konfigurowac to lokalnie i na produkcji.

## Pojecia i pola konfiguracji

- FromEmail: adres nadawcy widoczny w polu "From" (np. no-reply@habitflow.app). To jest "adres aplikacji", z ktorego uzytkownik widzi wiadomosc.
- FromName: nazwa nadawcy wyswietlana obok FromEmail (np. HabitFlow).
- Email:Smtp:Username: login do serwera SMTP (konto techniczne), uzywany do uwierzytelnienia przy wysylce.
- Email:Smtp:Password: haslo lub token do SMTP.
- Email:Smtp:Host / Port: adres i port serwera SMTP.
- Email:LinkBaseUrl: adres bazowy UI (Blazor), z ktorego budowany jest link aktywacyjny/resetu.
- App:BaseUrl: adres bazowy aplikacji (fallback, gdy LinkBaseUrl nie jest ustawiony).

### FromEmail vs Username

- FromEmail to adres, ktory uzytkownik widzi jako nadawce wiadomosci.
- Username to konto, ktorym logujemy sie do SMTP (autoryzacja). Czasem moze byc taki sam jak FromEmail, ale nie musi.
- Przyklad: korzystamy z SendGrid SMTP.
  - Username: "apikey" (staly login)
  - Password: rzeczywisty klucz API
  - FromEmail: "no-reply@habitflow.app"

## Proces rejestracji - przeplyw

1) Uzytkownik wysyla POST /api/v1/auth/register (email, haslo).
2) Backend tworzy konto w Identity i ustawia EmailConfirmed = false.
3) Backend generuje token potwierdzajacy (jednorazowy, z TTL).
4) Backend buduje link: Email:LinkBaseUrl + /auth/confirm-email?userId=...&token=...
5) IEmailSender wysyla mail z linkiem aktywacyjnym.
6) Uzytkownik klika link, frontend wysyla POST /api/v1/auth/confirm-email.
7) Backend weryfikuje token i ustawia EmailConfirmed = true.

## Konfiguracja developerska (lokalnie)

Cel: miec realne maile, ale bez wysylki do Internetu.

Rekomendacja:
- Uzyc lokalnego serwera SMTP (np. MailHog, smtp4dev, Papercut).
- Ustawic w appsettings.Development.json tylko niesekretne dane (Host/Port/FromEmail/FromName/App:BaseUrl).
- Dane uwierzytelniajace (Username/Password) trzymac w user-secrets lub env vars.

### Krok po kroku (Docker + smtp4dev)

1) Uruchom kontenery:
```bash
cd .docker
docker-compose up -d
```

2) Otworz UI smtp4dev:
- http://localhost:3000

3) Ustaw konfiguracje SMTP w `HabitFlow.Api/appsettings.Development.json`:
- Email:Smtp:Host = localhost
- Email:Smtp:Port = 2525
- Email:LinkBaseUrl = https://localhost:7231
- Email:FromEmail = no-reply@habitflow.local
- Email:FromName = HabitFlow
- App:BaseUrl = http://localhost:5000 (lub port Blazor)

4) Jesli SMTP wymaga auth (tu nie wymaga), ustaw user-secrets:
```bash
cd HabitFlow.Api
dotnet user-secrets init
dotnet user-secrets set "Email:Smtp:Username" "dev"
dotnet user-secrets set "Email:Smtp:Password" "dev"
```

5) Uruchom API i Blazor, zarejestruj konto i sprawdz skrzynke w smtp4dev.

Typowe ustawienia lokalne:
- Email:Smtp:Host = localhost
- Email:Smtp:Port = 1025
- Email:Smtp:Username = (puste lub dowolne)
- Email:Smtp:Password = (puste lub dowolne)
- Email:FromEmail = no-reply@habitflow.local
- Email:FromName = HabitFlow
- App:BaseUrl = http://localhost:5000

Uwagi:
- Lokalny SMTP zwykle nie wymaga TLS (EnableSsl = false) i nie sprawdza auth.
- W razie potrzeby mozna dodac tryb "DevEmailSender", ktory tylko loguje linki zamiast wysylac SMTP.

## Konfiguracja produkcyjna

Cel: niezawodna wysylka realnych maili, z pelnym audytem i bezpiecznymi sekretami.

Rekomendacja:
- Provider: SendGrid/Mailgun/AWS SES (SMTP lub API).
- Sekrety tylko w zmiennych srodowiskowych lub secret managerze (nie w repo).
- Domena i SPF/DKIM ustawione u providera dla poprawnej dostarczalnosci.

Typowy zestaw:
- Email:Smtp:Host = smtp.provider.com
- Email:Smtp:Port = 587
- Email:Smtp:Username = konto SMTP / "apikey"
- Email:Smtp:Password = sekret
- Email:FromEmail = no-reply@habitflow.app
- Email:FromName = HabitFlow
- App:BaseUrl = https://app.habitflow.app

## Checklista

- FromEmail to adres widoczny dla uzytkownika.
- Username to konto do logowania na SMTP.
- Sekrety tylko w user-secrets/env/secret store.
- App:BaseUrl ustawione per srodowisko.
- Linki potwierdzenia i resetu generowane na backendzie.
