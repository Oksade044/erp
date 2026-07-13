using System.Reflection;
using ERP.Application.Common.Behaviors;
using ERP.Application.Common.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Application;

/// <summary>
/// Application layer-in DI qeydiyyatı (TDD §18). Hər layer öz servislərini modul
/// şəkildə qeyd edir — host yalnız AddApplication() çağırır.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Yüngül daxili mediator (TDD §17) — xarici asılılıq yoxdur.
        services.AddScoped<ISender, Mediator>();

        // Bütün IRequestHandler<,> implementasiyalarını assembly-dən qeyd et.
        var handlerInterface = typeof(IRequestHandler<,>);
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var service in type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface))
            {
                services.AddScoped(service, type);
            }
        }

        // FluentValidation — validator-lar assembly-dən tapılır (TDD §22).
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behavior — hər request avtomatik validasiyadan keçir (TDD §22).
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // TODO: logging & transaction behavior-ları sonra əlavə olunacaq.

        return services;
    }
}
