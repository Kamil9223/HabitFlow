# SQL Server 2022 Developer Edition
FROM mcr.microsoft.com/mssql/server:2022-latest

# Ustawienia środowiska
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=HabitFlow2024!
ENV MSSQL_PID=Developer
ENV MSSQL_TCP_PORT=1433

# Eksponuj port MSSQL
EXPOSE 1433

# Domyślne polecenie uruchomienia SQL Server
CMD ["/opt/mssql/bin/sqlservr"]
