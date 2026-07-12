# 📘 TEXNİKİ LAYİHƏ SƏNƏDİ (Technical Design Document)
## Toy Dekoru & Tədbir Avadanlığı İcarəsi — Enterprise ERP

**Faza 2: Texniki Təməl · Versiya 1.1 · Status: Təsdiqləndi (2026-07-12)**

> Bütün qərarlar bir prinsipə tabedir:
> **Lokal başla → serverə kodsuz keç → 100+ istifadəçi → 10 il yaşasın.**

> **Qeyd (v1.1):** Sistemdə **.NET 10 (10.0.101, LTS)** quraşdırılıb. İlkin sənəddə .NET 9 yazılmışdı; .NET 10 daha yeni LTS olduğu və artıq mövcud olduğu üçün platform kimi **.NET 10** seçilir. Bütün digər qərarlar dəyişməz qalır.

---

## 1. Proqramlaşdırma Dili — **C# / .NET 10**
**Səbəb:** Enterprise-də sınaqdan çıxmış, LTS dəstəkli. Ən böyük üstünlük: **eyni Domain + Application kodu desktop, backend və gələcək mobil arasında bölüşülür** — tək dil, tək komanda, tək məntiq bazası. Yüksək performans (native AOT imkanı), zəngin ekosistem, güclü async model.

## 2. Windows Desktop — **Avalonia UI (MVVM)**
**Səbəb:** GPU-akselerasiyalı (SkiaSharp render), dark/light tema, multi-window + dock, premium UI tələblərini qarşılayır. WPF-dən fərqli olaraq **cross-platform** və gələcək **.NET MAUI mobil** ilə Domain/Application kodunu paylaşır. MVVM UI-nin test edilə bilən və genişlənən qalmasını təmin edir.

## 3. Backend — **ASP.NET Core Web API**
**Səbəb:** ERP backend üçün sənaye standartı. **Linux VPS-də native işləyir** (Kestrel), tam async I/O ilə yüksək throughput, daxili DI, middleware pipeline. Eyni API həm lokalda (localhost proses), həm serverdə, həm mobil üçün işləyir.

## 4. Database — **Lokal: SQLite → Server: PostgreSQL**
**Səbəb:** SQLite = "sadə yaddaş": tək fayl, **quraşdırma yoxdur**, servis yoxdur, amma real SQL + ACID + transaction. PostgreSQL = server üçün: **MVCC ilə 100+ eyni anda istifadəçi**, ikiqat-bron kilidləri, güclü indeks. EF Core hər ikisini eyni kodla dəstəklədiyi üçün keçid **kodsuzdur** (yalnız provider + connection string).
> **Qayda:** provider-a xas SQL yox — yalnız EF Core abstraksiyası. Bu, SQLite→PostgreSQL keçidini ağrısız edir.

## 5. ORM — **EF Core (əsas) + Dapper (ağır oxumalar)**
**Səbəb:** EF Core = məhsuldarlıq, migration, provider abstraksiyası (SQLite/PG). Dapper = 100k+ sətirlik hesabat və siyahılarda maksimum sürət. Hibrid yanaşma "yazıda rahatlıq, oxuda sürət" verir.

## 6. Authentication — **JWT (Access + Refresh Token) + ASP.NET Core Identity**
**Səbəb:** JWT **stateless**-dir → server yaddaşda sessiya saxlamır → üfüqi genişlənmə (bölmə 40) kodsuz olur. Identity parol hash/salt, kilidləmə, təhlükəsizlik idarəsini hazır verir. Access token qısa ömürlü, Refresh token uzun — həm təhlükəsiz, həm rahat. Lokalda da, serverdə də, mobildə də eyni.

## 7. Authorization — **Permission-based RBAC + Policy-based Authorization**
**Səbəb:** Rollar (Admin, Anbardar, Menecer, Kassir) → atomik icazələr (`orders.approve`, `products.edit`). Kod `if role=="Admin"` **yazmır**, `RequirePermission("orders.approve")` yazır. Rollar dəyişəndə kod qırılmır — gələcək-təhlükəsiz və çevik. ASP.NET Core policy sistemi bunu endpoint səviyyəsində tətbiq edir.

## 8. Clean Architecture
```
Presentation (Avalonia) → API → Application → Domain ← Infrastructure
Asılılıq həmişə İÇƏRİYƏ. Domain heç kimi tanımır.
```
**Səbəb:** Biznes məntiqini UI/DB/framework-dən ayırır. 10 il ərzində texnologiya (UI, DB) dəyişsə belə, qəlb (Domain) toxunulmaz qalır.

## 9. Layer-lər
| Layer | Vəzifə |
|---|---|
| **Domain** | Entity, Value Object, biznes invariantları, interfeyslər — sıfır asılılıq |
| **Application** | Use-case-lər (CQRS handler), servis orkestrasiyası, DTO, validasiya |
| **Infrastructure** | EF Core, Repository, UoW, cache, fayl, audit, xarici servislər |
| **API** | HTTP sərhəd: auth, versiyalama, exception, rate-limit |
| **Presentation** | Yalnız göstərmə (Avalonia) — biznes qaydası yoxdur |

## 10. Folder Structure
```
src/
├── Core/ERP.Domain/         (Entities, ValueObjects, Enums, Events, Interfaces)
├── Core/ERP.Application/     (Features/CQRS, DTOs, Validators, Behaviors, Interfaces)
├── Infrastructure/ERP.Infrastructure/ (Persistence, Repositories, Audit, Cache, Files, Identity, Jobs)
├── Api/ERP.Api/             (Endpoints, Middleware, Auth, appsettings.*)
└── Clients/
    ├── ERP.Desktop/         (Avalonia: Views, ViewModels, Themes, ApiClient)
    ├── ERP.Shared.Contracts/(DTO müqavilələri — client+server bölüşür)
    └── ERP.Mobile/          (gələcək skeleton)
tests/ · deploy/ · docs/
```

## 11. API Strukturu — **RESTful + Versiyalı**
**Səbəb:** `/api/v1/...` resurs-yönümlü, versiyalama gələcək dəyişiklikləri köhnə clientləri qırmadan buraxır. Standart cavab zərfi (`Result<T>`, `PagedResult<T>`), xətalar **RFC 7807 ProblemDetails** formatında, avtomatik **OpenAPI/Swagger** sənədi. Server-side pagination/filter/sort məcburidir.

## 12. DTO Strukturu
**Səbəb:** Entity heç vaxt şəbəkəyə çıxmır. Request/Response DTO-lar **ERP.Shared.Contracts**-da — client və server eyni müqaviləni paylaşır (tip təhlükəsizliyi). Mapping üçün **Mapster** (performanslı, minimal reflection). Bu; təhlükəsizlik (daxili sahələr gizli), versiyalama və performans (yalnız lazımi data) verir.

## 13. Entity Strukturu — **Rich Domain Model**
**Səbəb:** Entity-lər "anemic" (yalnız property) deyil — biznes qaydaları öz içindədir (məs. `Product.ChangeTrackingMode()` invariant yoxlayır). Hamısı **BaseEntity**-dən miras alır: `Id (Guid)`, audit sahələri, `IsDeleted` (soft delete), `RowVersion` (optimistic concurrency). Value Object-lər (Money, Sku, Email) primitivləri əvəz edir → yanlış data qeyri-mümkün olur.

## 14. Repository Pattern — **Generic + Specific + Specification**
**Səbəb:** `IRepository<T>` ümumi əməliyyatlar, xüsusi repolar (məs. `IProductRepository`) domenə xas sorğular. Interfeyslər Application-da, implementasiya Infrastructure-da → Application SQL bilmir. **Specification pattern** ilə mürəkkəb filtrlər təkrar-istifadə olunur, test edilir.

## 15. Unit of Work
**Səbəb:** Bir biznes əməliyyatındakı bütün dəyişiklikləri **tək transaction**-da commit edir ("sifariş yarat + stok rezerv et + audit yaz" — ya hamısı, ya heç biri). `IUnitOfWork` DbContext-i sarır, data bütövlüyünün təminatçısıdır.

## 16. Service Layer
**Səbəb:** Application servisləri use-case-ləri orkestr edir; biznes məntiqi buradadır. API controller-ləri **nazikdir** — yalnız çağırır. Bu, eyni məntiqin gələcəkdə mobil API tərəfindən də istifadəsini təmin edir.

## 17. CQRS — **Bəli, yüngül CQRS (MediatR ilə)**
**Səbəb:** Command (yazma) və Query (oxuma) ayrılır → **MediatR pipeline** ilə validasiya, logging, transaction, caching avtomatik hər sorğuya tətbiq olunur (cross-cutting). **Amma** başlanğıcda tam event-sourcing/ayrı oxuma DB YOX — həddindən artıq mürəkkəblik. Read model gələcəkdə yük artanda ayrıla bilər. Bu, "sadə başla, genişlənə bilən qal" prinsipidir.

## 18. Dependency Injection
**Səbəb:** Daxili **Microsoft.Extensions.DependencyInjection**. Hər layer öz servislərini modul şəkildə qeyd edir (`AddApplication()`, `AddInfrastructure()`). Bütün asılılıqlar interfeys üzərindən → test, dəyişdirmə, genişlənmə asan (SOLID "D").

## 19. Logging — **Serilog (struktur log)**
**Səbəb:** Struktur (JSON) loglar → axtarıla və filtrlənə bilən. Lokalda **fayl sink**, serverdə **Seq/console** (Docker log). Hər sorğuya **Correlation ID** → problem izləmə. Log səviyyələri konfiqurasiya ilə idarə olunur.

## 20. Audit Log — **EF Core Interceptor (SaveChanges)**
**Səbəb:** Audit əl ilə yazılmır — **interceptor** hər Insert/Update/Delete-də avtomatik: entity adı, ID, köhnə→yeni dəyər (JSON), istifadəçi, vaxt, IP. Ayrı `AuditLog` cədvəli. Bu; təhlükəsizlik, hesabatlıq və mübahisə həlli üçün ("kim bu sifarişi dəyişdi") kritikdir.

## 21. Exception Handling — **Global Middleware + Result Pattern**
**Səbəb:** İki qat: (1) **Gözlənilən xətalar** (validasiya, tapılmadı) `Result<T>` ilə qaytarılır — exception atmır, performanslı. (2) **Gözlənilməz xətalar** global middleware tutur → **ProblemDetails** qaytarır, daxili detalları **heç vaxt sızdırmır**, Serilog-a yazır. Domenə xas exception-lar (`InsufficientStockException`) mənalı mesaj verir.

## 22. Validation — **FluentValidation + MediatR Behavior**
**Səbəb:** Deklarativ, test edilə bilən qaydalar. **Pipeline behavior** ilə hər command avtomatik validasiyadan keçir — controller-də əl ilə yoxlama yoxdur. İki səviyyə: DTO validasiyası (format) + Domain invariantları (biznes qaydası).

## 23. File Storage — **IFileStorage abstraksiyası**
**Səbəb:** Lokalda = **disk qovluğu**, serverdə = disk və ya **S3-uyğun (MinIO)**. Kod `IFileStorage` interfeysi ilə işləyir — keçid **konfiqurasiya** ilə. Fayllar **heç vaxt DB-də saxlanmır**, yalnız yol/metadata DB-də.

## 24. Image Storage
**Səbəb:** Məhsul şəkilləri fayl storage-də (yuxarıdakı abstraksiya). DB yalnız yol + metadata saxlayır. **Thumbnail-lar əvvəlcədən generasiya** olunur (siyahılarda sürət). BLOB DB-də saxlamaq performansı öldürər — buna görə qadağandır.

## 25. PDF Generation — **QuestPDF**
**Səbəb:** Kod-əsaslı, sürətli, HTML-engine tələb etmir (Chromium yükü yoxdur), Linux-da problemsiz. İcarə **müqavilələri, qaimələr, təhvil-təslim aktları** üçün ideal. Şablon sistemi ilə peşəkar sənədlər.

## 26. Excel Import/Export — **ClosedXML**
**Səbəb:** Lisenziya problemi yoxdur (EPPlus-un kommersiya lisenziyasından fərqli). Məhsul kataloqunun toplu **importu**, hesabatların **exportu**. Böyük fayllar **background job**-da işlənir ki, UI donmasın.

## 27. Barcode & QR — **QRCoder / ZXing.Net**
**Səbəb:** Generasiya: hər məhsul/nüsxə üçün barkod/QR yaradılır və çap olunur. Barkod dəyəri DB-də (`Barcode` entity). Skanlama: **əl skaneri (keyboard-wedge)** — heç bir xüsusi inteqrasiya lazım deyil, klaviatura kimi işləyir. Gələcəkdə mobil kamera ilə skanlama. Anbar giriş/çıxış, inventarizasiya sürətlənir.

## 28. Printer Dəstəyi
**Səbəb:** İki tip: (1) **Sənəd çapı** (A4 qaimə/müqavilə) → PDF → sistem printeri. (2) **Etiket/barkod çapı** → etiket printeri (ESC/POS və ya Windows print). Şablon-əsaslı, desktop native çap API.

## 29. Backup Strategiyası
**Səbəb:** Lokal (SQLite): planlaşdırılmış **fayl kopyası + WAL checkpoint** (background job), gündəlik. Server (PostgreSQL): planlaşdırılmış **`pg_dump`** + **WAL arxivləmə (PITR)** — istənilən ana bərpa. **Offsite kopya** (xarici disk/bulud) məlumat itkisinə qarşı. Backup avtomatik + monitorinqlə.

## 30. Restore Strategiyası
**Səbəb:** Sənədləşmiş bərpa: SQLite = fayl əvəzləmə; PG = **`pg_restore`** və ya PITR. **Test-restore** mütəmadi (bərpa olunmayan backup = backup deyil). Migration-lar versiyalı → sxem uyğunsuzluğu olmur.

## 31. Offline İşləmə Prinsipi
**Səbəb:** Lokalda API + SQLite **eyni kompüterdə** proses kimi. UI → localhost API → SQLite. İnternet lazım deyil. **Kritik:** UI birbaşa DB-yə qoşulmur, həmişə API ilə (API-first) — bu, serverə keçidi kodsuz saxlayan yeganə şeydir.

## 32. VPS Keçid Strategiyası
**Səbəb:** **Docker Compose** ilə: API + PostgreSQL + Redis + Nginx (TLS reverse proxy). Addımlar: (1) `appsettings.Production.json` konfiqurasiya, (2) `docker compose up`, (3) data köçürmə (`pg_dump`→`pg_restore`), (4) TLS (Let's Encrypt), (5) client-də `ApiBaseUrl` dəyişikliyi. **Kod dəyişmir** — "build once, deploy anywhere".

## 33. Performans Optimallaşdırma
**Səbəb:** Async hər yerdə; server-side pagination/filter; **projection** (yalnız lazımi sahələr); strateji **indeksləmə** (axtarış + tarix aralığı); ağır oxumalarda Dapper; caching; UI virtualization (100k sətir); connection pooling; N+1 query qadağan. Performans **ölçülür** (SLA + struktur log), "hiss" deyil.

## 34. Memory Management
**Səbəb:** Böyük data **stream** olunur (`IAsyncEnumerable`), heç vaxt tam cədvəl yaddaşa yüklənmir. **DbContext pooling** (obyekt təkrar-istifadə). Pagination + UI virtualization RAM istifadəsini sabit saxlayır. Düzgün `Dispose`/`using`. 100k məhsulda belə yaddaş partlamır.

## 35. Multi-threading Strategiyası
**Səbəb:** **Async/await** I/O üçün (thread bloklanmır). **UI thread heç vaxt gözləmir** — hər API çağırışı/hesablama background thread-də; UI yalnız çəkir. Serverdə thread-pool sorğuları paralel idarə edir. Kilid əvəzinə **optimistic concurrency** (RowVersion) — deadlock riski minimal.

## 36. Background Jobs — **Hangfire**
**Səbəb:** Lokalda in-process, serverdə PostgreSQL storage ilə davamlı. PDF generasiya, e-poçt/SMS, Excel import, hesabat, backup, gecikməli tapşırıqlar — hamısı növbədə, API cavabını gözlətmədən. **Dashboard** ilə monitorinq. Server-də ayrıca worker node-a çıxarıla bilər.

## 37. Cache Strategiyası — **ICacheService abstraksiyası**
**Səbəb:** Lokalda **IMemoryCache**, serverdə **Redis** (paylaşılan, node-lar arası). Kod eyni interfeyslə işləyir — keçid konfiqurasiya. Dəyişməyən data (kateqoriya, status, settings) cache-lənir; dəyişəndə **invalidation**. Redis eyni zamanda SignalR backplane və distributed lock verir.

## 38. Real-time Notification — **SignalR**
**Səbəb:** WebSocket-əsaslı canlı bildiriş: Notification Center, **canlı stok/mövcudluq** yeniləməsi (bir istifadəçi rezerv edəndə digərləri dərhal görür — ikiqat-bronun qarşısı). Lokalda da işləyir (localhost). Serverdə **Redis backplane** ilə çoxlu node arasında miqyaslanır.

## 39. Security Strategiyası
**Səbəb (çoxqatlı müdafiə):** TLS/HTTPS (transport); JWT + parol hash (autentifikasiya); Permission RBAC (avtorizasiya); FluentValidation (giriş yoxlaması); parametrli sorğular/ORM (SQL injection qorunması); **rate limiting** (sui-istifadə); audit log (izlənəbilirlik); **least privilege** (minimal icazə); secrets management (parollar kodda deyil, env/secret store); istəyə görə data-at-rest şifrələmə. Təhlükəsizlik **default**-dur, sonradan yamaq deyil.

## 40. Future Mobile API Strategiyası
**Səbəb:** **Eyni ASP.NET Core API** mobil üçün də xidmət edir (REST/JSON, JWT). Mobil client **ERP.Shared.Contracts** DTO müqavilələrini paylaşır (tip təhlükəsizliyi). **.NET MAUI** ilə mobil app Domain + Application kodunu birbaşa təkrar-istifadə edir — sıfırdan yazma yoxdur. API versiyalı olduğu üçün mobil və desktop müstəqil inkişaf edə bilər.

---

## 🧩 Texnologiya Xülasəsi (bir baxışda)
| Sahə | Seçim |
|---|---|
| Dil / Platform | C# / .NET 10 |
| Desktop | Avalonia UI (MVVM) |
| Backend | ASP.NET Core Web API |
| DB (lokal→server) | SQLite → PostgreSQL |
| ORM | EF Core + Dapper |
| Auth | JWT + Identity |
| Authz | Permission-based RBAC |
| CQRS/Mediator | MediatR (yüngül CQRS) |
| Mapping | Mapster |
| Validation | FluentValidation |
| Logging | Serilog |
| PDF | QuestPDF |
| Excel | ClosedXML |
| Barcode/QR | QRCoder / ZXing.Net |
| Background | Hangfire |
| Cache | IMemoryCache → Redis |
| Real-time | SignalR |
| File/Image | IFileStorage (disk → MinIO) |
| Deploy | Docker Compose + Nginx |
| Mobile (gələcək) | .NET MAUI (kod paylaşımı) |

---

## 📌 Layihə Prinsipləri (dəyişməz qaydalar)
1. **API-first** — UI heç vaxt birbaşa DB-yə qoşulmur.
2. **Provider-agnostik** — provider-a xas SQL yox, yalnız EF Core abstraksiyası.
3. **Domain toxunulmazdır** — asılılıq həmişə içəriyə.
4. **Təhlükəsizlik default-dur** — sonradan yamaq deyil.
5. **Performans ölçülür** — "hiss" deyil.
6. **Dil: yalnız Azərbaycanca** (UI mətnləri).
7. **İcarə qiyməti** məhsula və sifarişə görə dinamik dəyişir.
