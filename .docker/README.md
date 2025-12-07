# HabitFlow Docker Configuration

## MSSQL Database

### Uruchomienie bazy danych lokalnie

#### Opcja 1: Docker Compose (zalecane)
```bash
# Z katalogu głównego projektu
cd .docker
docker-compose up -d

# Sprawdzenie statusu
docker-compose ps

# Logi
docker-compose logs -f mssql

# Zatrzymanie
docker-compose down
```

#### Opcja 2: Docker Build i Run
```bash
# Z katalogu głównego projektu
cd .docker/mssql
docker build -t habitflow-mssql .
docker run -d -p 1433:1433 --name habitflow-mssql habitflow-mssql
```

### Parametry połączenia

- **Server**: `localhost,1433`
- **Database**: `HabitFlowDb` (utworzona automatycznie przez EF Core migrations)
- **User**: `sa`
- **Password**: Zobacz `SA_PASSWORD` w `docker-compose.yml`

### Połączenie z bazą

#### SQL Server Management Studio (SSMS)
- Server name: `localhost,1433`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: Użyj wartości `SA_PASSWORD` z `docker-compose.yml`

#### Azure Data Studio
- Connection type: Microsoft SQL Server
- Server: `localhost,1433`
- Authentication type: SQL Login
- User name: `sa`
- Password: Użyj wartości `SA_PASSWORD` z `docker-compose.yml`

#### sqlcmd (z poziomu kontenera)
```bash
docker exec -it habitflow-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<SA_PASSWORD>' -C
```

### Zarządzanie wolumenem danych

```bash
# Lista wolumenów
docker volume ls

# Inspekcja wolumenu
docker volume inspect docker_mssql-data

# Usunięcie wolumenu (UWAGA: usuwa wszystkie dane!)
docker-compose down -v
```

### Healthcheck

Kontener sprawdza swój stan co 30 sekund. Możesz sprawdzić status:
```bash
docker inspect habitflow-mssql --format='{{.State.Health.Status}}'
```

### Troubleshooting

#### Kontener się nie uruchamia
```bash
# Sprawdź logi
docker logs habitflow-mssql

# Sprawdź czy port 1433 nie jest zajęty
netstat -ano | findstr :1433
```

#### Nie można połączyć się z bazą
1. Sprawdź czy kontener działa: `docker ps`
2. Sprawdź healthcheck: `docker inspect habitflow-mssql`
3. Sprawdź czy firewall nie blokuje portu 1433
4. Upewnij się, że używasz wartości `SA_PASSWORD` z `docker-compose.yml`

#### Reset bazy danych
```bash
# Zatrzymaj i usuń kontener wraz z danymi
docker-compose down -v

# Uruchom ponownie
docker-compose up -d
```

### Produkcja

⚠️ **UWAGA**: Ten setup jest przeznaczony tylko dla środowiska deweloperskiego!

Dla produkcji:
- Użyj silniejszego hasła i przechowuj je w secrets
- Rozważ użycie managed database service (Azure SQL Database)
- Skonfiguruj backupy
- Włącz SSL/TLS
- Ogranicz dostęp sieciowy
