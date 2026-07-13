using FluentValidation;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior — hər request avtomatik FluentValidation-dan keçir (TDD §22).
/// Controller/handler-də əl ilə yoxlama yoxdur. Xəta olarsa ValidationException atılır
/// (global middleware onu 400/422-yə çevirəcək).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next(ct);
    }
}
