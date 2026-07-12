# ERP — Toy Dekoru & Tədbir Avadanlığı İcarəsi

Enterprise ERP sistemi. Prinsip: **lokal başla → serverə kodsuz keç → 100+ istifadəçi → 10 il yaşasın.**

Tam texniki əsas: [`docs/architecture/technical-design.md`](docs/architecture/technical-design.md).

## Texnologiya
C# / .NET 10 · Avalonia (desktop) · ASP.NET Core (API) · EF Core + SQLite→PostgreSQL · MediatR · FluentValidation · Mapster.

## Solution strukturu (Clean Architecture)
```
src/
├── Core/
│   ├── ERP.Domain          Entity, ValueObject, biznes invariantları (sıfır asılılıq)
│   └── ERP.Application      Use-case (CQRS/MediatR), DTO, validasiya, interfeyslər
├── Infrastructure/
│   └── ERP.Infrastructure   EF Core (AppDbContext), repository, UoW
├── Api/
│   └── ERP.Api             ASP.NET Core Web API host (DI wiring, endpoints)
└── Clients/
    ├── ERP.Desktop         Avalonia MVVM masaüstü client
    └── ERP.Shared.Contracts DTO müqavilələri (client + server bölüşür)
```
Asılılıq həmişə **içəriyə** yönəlir. Domain heç kimi tanımır.

## Qurma və işə salma
```powershell
dotnet build ERP.slnx
dotnet run --project src/Api/ERP.Api      # API → https://localhost:xxxx/health
```

## Vəziyyət
Faza 1–2 (arxitektura + texniki dizayn) tamam. Solution skeleti quruldu. Növbəti: **Modul 2 — Müştərilər (domain dizaynı)**.
