namespace ERP.Application.Common.Messaging;

/// <summary>
/// Yüngül daxili mediator müqavilələri (MediatR-ın Task-əsaslı API-si ilə uyğun).
/// Xarici asılılıq/lisenziya yoxdur — 10 illik layihə üçün tam nəzarət (TDD §17).
/// </summary>

/// <summary>Cavab qaytaran mesaj (command və ya query).</summary>
public interface IRequest<out TResponse>;

/// <summary>Bir request tipini emal edən handler.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

/// <summary>Request-ləri handler-ə göndərən mediator.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}

/// <summary>Pipeline-də növbəti addımı (handler və ya sonrakı behavior) təmsil edir.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct = default);

/// <summary>
/// Cross-cutting pipeline behavior (validasiya, logging, transaction — TDD §17, §22).
/// Hər request handler-dən əvvəl/sonra icra olunur.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
