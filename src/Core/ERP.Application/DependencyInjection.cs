using System.Reflection;
using ERP.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
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

        // MediatR — yüngül CQRS (TDD §17). Command/Query handler-ləri assembly-dən tapılır.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation — validator-lar assembly-dən tapılır (TDD §22).
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behavior — hər request avtomatik validasiyadan keçir (TDD §22).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // TODO: logging & transaction behavior-ları sonra əlavə olunacaq.

        return services;
    }
}
