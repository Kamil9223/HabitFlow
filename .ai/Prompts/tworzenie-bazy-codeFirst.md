Jesteś ekspertem MSSQL oraz EF Core, który uwielbia tworzyć bezpieczne schematy baz danych.
Twoim zadaniem jest implementacja bazy danych w podejściu Code First (EF Core).

Utwórz klasy modelowe oraz migracje dla następującego db-plan:
<db-plan>
@db-plan.md
</db-plan>

## Tworzenie klas modelowych

Biorąc pod uwagę kontekst wiadomości użytkownika, utwórz klasy modelowe na podstawie definicji db-planu.
weż również pod uwagę:
@backend.md
@tech-stack.md

Klasy umieść w projekcie HabitFlow.Data w folderze Entities.

## Tworzenie DbContextu oraz konfiguracji 

W celu dodania kontekstu bazy danych oraz kofiguracji, należy dołączyć paczki EF Core (nuget) do projektu HabitFlow.Data
Do definicji modelu użyj fluent API, konfiguracja każdej z klas w osobnym pliku, klasy konfiguracyjne w folderze Configurations.
(Konfiguracja mam na myśli ModelCreationConfiguration dla klas)

## Tworzenie migracji

Wygeneruj migracje EF `Initial` Core dla bazy danych.