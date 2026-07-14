# Server Deployment (SQLite → PostgreSQL)

Prinsip: **app kodu dəyişmir** — yalnız provider config + connection string + Postgres migration-ları (TDD §4, §32).

## Nə dəyişir (kodsuz keçidin bütün fərqi)
1. `Database:Provider` = `Postgres` (appsettings.Production.json / env `Database__Provider`)
2. `ConnectionStrings:Default` → Postgres connection string
3. Postgres üçün migration seti (aşağıda)

Domain, Application, repository, endpoint kodu — **heç biri dəyişmir**. Provider seçimi
yalnız `AddInfrastructure`-dədır (`UseSqlite` ↔ `UseNpgsql`).

## 1. Postgres migration-ları yaratmaq
EF Core migration-ları provider-a xasdır (SQLite tipi ≠ Postgres tipi). Serverə keçəndə
Postgres üçün ayrıca miqrasiya seti yaradılır:

```powershell
# Postgres provider aktiv ikən (env ilə) migration yarat
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=erp;Username=erp;Password=..."
dotnet ef migrations add InitialPg `
  --project src/Infrastructure/ERP.Infrastructure `
  --startup-project src/Api/ERP.Api `
  --output-dir Persistence/Migrations/Postgres
```

> Qeyd: lokal SQLite migration-ları ilə eyni contextdə saxlanılırsa, ayrı migration
> assembly və ya provider-ayrılmış qovluq strategiyası tələb olunur. Sadə yol:
> serverdə təmiz Postgres bazasına ilk dəfə `InitialPg` tətbiq etmək.

## 2. Docker Compose ilə qaldırmaq
```powershell
$env:POSTGRES_PASSWORD = "güclü-parol"
$env:JWT_KEY = "ən-azı-32-baytlıq-gizli-açar"
docker compose -f deploy/docker-compose.yml up -d --build
```
Servislər: **api** (:8080), **postgres** (:5432), **redis** (:6379).
API açılışda migration-ları avtomatik tətbiq edir (`DbSeeder.MigrateAsync`) və admin seed edir.

## 3. Data köçürmə (mövcud SQLite → Postgres)
Kiçik həcm üçün: API vasitəsilə yenidən daxiletmə, və ya `pgloader` ilə birbaşa köçürmə.

## 4. Client
Desktop-da `ApiBaseUrl`-i server ünvanına dəyiş (hazırda `http://localhost:5080`).

## Qeydlər
- **RowVersion concurrency**: SQLite-də sadə sütun, Postgres-də `IsRowVersion` (server-side).
  Postgres üçün `xmin` konkurentlik tokeni daha idiomatikdir — server miqrasiyasında nəzərdən keçirin.
- **Redis**: cache + SignalR backplane üçün hazırdır (TDD §37, §38) — kod inteqrasiyası gələcək iş.
- **Nginx/TLS**: reverse proxy + Let's Encrypt production üçün əlavə olunmalıdır (TDD §32).
