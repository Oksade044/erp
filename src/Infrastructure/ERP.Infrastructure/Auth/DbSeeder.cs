using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Users;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Infrastructure.Auth;

/// <summary>
/// İlkin data seeding. İstifadəçi yoxdursa, standart admin yaradır ki, sistemə ilk giriş mümkün olsun.
/// Prod-da bu parol dərhal dəyişdirilməlidir.
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Sxemi hazırla. EF migration-ları provider-xasdır — mövcud migration seti SQLite
        // tipləri (TEXT/BLOB/INTEGER) ilə yaradılıb və Postgres-ə birbaşa tətbiqi sxemi sındırır
        // (məs. Guid→uuid ↔ text uyğunsuzluğu). Ona görə:
        //   • Postgres → sxem modeldən birbaşa (provider-düzgün) yaradılır (EnsureCreated).
        //   • SQLite/digər → migration tətbiq olunur (lokal-first, tarixçəli).
        // Qeyd: Postgres-də incremental migration lazım olanda ayrıca Postgres migration seti/
        // assembly-yə keçilməlidir (EnsureCreated __EFMigrationsHistory yaratmır).
        var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isPostgres)
        {
            await db.Database.EnsureCreatedAsync();

            // Soft-delete olunmuş sətirlər unikal indeksdə yeri tutmasın — indeksləri "partial"
            // et (WHERE "IsDeleted" = false). Belə ki, silinmiş adı/telefonu/kodu təzədən yaratmaq
            // mümkün olsun (əvvəl "duplicate key" → 500 verirdi, bütün bölmələrdə). Dinamik +
            // idempotent: yalnız IsDeleted sütunu olan cədvəllərin filtri olmayan unikal indeksləri.
            await db.Database.ExecuteSqlRawAsync("""
                DO $$
                DECLARE r RECORD;
                BEGIN
                  FOR r IN
                    SELECT i.indexname, i.indexdef
                    FROM pg_indexes i
                    WHERE i.schemaname = 'public'
                      AND i.indexdef ILIKE '%UNIQUE INDEX%'
                      AND i.indexdef NOT ILIKE '%WHERE%'
                      AND EXISTS (
                        SELECT 1 FROM information_schema.columns c
                        WHERE c.table_schema = 'public' AND c.table_name = i.tablename
                          AND c.column_name = 'IsDeleted')
                      -- Konstraint (PK / UNIQUE constraint) arxasında olan indeksləri ÇIXAR:
                      -- onlar DROP INDEX ilə silinə bilməz (yalnız ALTER TABLE ilə).
                      AND NOT EXISTS (
                        SELECT 1 FROM pg_constraint con
                        JOIN pg_class ic ON ic.oid = con.conindid
                        WHERE ic.relname = i.indexname)
                  LOOP
                    EXECUTE 'DROP INDEX ' || quote_ident(r.indexname);
                    EXECUTE r.indexdef || ' WHERE "IsDeleted" = false';
                  END LOOP;
                END $$;
                """);
        }
        else
            await db.Database.MigrateAsync();

        // #16 — daxili rolları seed et (icazələr enum xəritəsindən; sonradan Admin dəyişə bilər).
        if (!await roles.AnyAsync())
        {
            foreach (var role in Enum.GetValues<Role>())
                await roles.AddAsync(AppRole.Create(role.ToString(), Permissions.ForRole(role), isSystem: true));
            await db.SaveChangesAsync();
        }
        else
        {
            // Sistem rollarına YENİ modul icazələrini (məs. representatives) əlavə et — idempotent,
            // mövcud icazələri SİLMƏDƏN (Admin-in əl dəyişiklikləri qorunur). Belə ki, köhnə DB-lərdə
            // də yeni bölmələr rol matrisində görünsün və işləsin.
            var changed = false;
            foreach (var role in Enum.GetValues<Role>())
            {
                var appRole = await roles.GetByNameAsync(role.ToString());
                if (appRole is null || !appRole.IsSystem) continue;

                var merged = appRole.Permissions.Union(Permissions.ForRole(role)).ToList();
                if (merged.Count != appRole.Permissions.Count)
                {
                    appRole.SetPermissions(merged);
                    changed = true;
                }
            }
            if (changed) await db.SaveChangesAsync();
        }

        if (await users.AnyAsync())
            return;

        var (hash, salt) = hasher.Hash(DefaultAdminPassword);
        var admin = User.Create(DefaultAdminUsername, hash, salt, "Sistem Administratoru", Role.Admin.ToString());

        await users.AddAsync(admin);
        await db.SaveChangesAsync();
    }
}
