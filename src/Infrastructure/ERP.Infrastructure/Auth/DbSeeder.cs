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

        // Gözləyən migration-ları tətbiq et (lokal-first: proqram açılanda sxem hazır olur).
        await db.Database.MigrateAsync();

        // #16 — daxili rolları seed et (icazələr enum xəritəsindən; sonradan Admin dəyişə bilər).
        if (!await roles.AnyAsync())
        {
            foreach (var role in Enum.GetValues<Role>())
                await roles.AddAsync(AppRole.Create(role.ToString(), Permissions.ForRole(role), isSystem: true));
            await db.SaveChangesAsync();
        }

        if (await users.AnyAsync())
            return;

        var (hash, salt) = hasher.Hash(DefaultAdminPassword);
        var admin = User.Create(DefaultAdminUsername, hash, salt, "Sistem Administratoru", Role.Admin.ToString());

        await users.AddAsync(admin);
        await db.SaveChangesAsync();
    }
}
