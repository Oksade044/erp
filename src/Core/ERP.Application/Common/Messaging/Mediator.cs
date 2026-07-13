using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Application.Common.Messaging;

/// <summary>
/// Yüngül mediator implementasiyası. Request tipinə uyğun handler-i DI-dən tapır və
/// bütün pipeline behavior-larını (validasiya və s.) əhatə edərək icra edir.
/// Wrapper-lər tip üzrə keşlənir (reflection yalnız bir dəfə).
/// </summary>
public sealed class Mediator(IServiceProvider provider) : ISender
{
    private static readonly ConcurrentDictionary<Type, object> _wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapperBase<TResponse>)_wrappers.GetOrAdd(
            request.GetType(),
            static requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, provider, ct);
    }

    private abstract class RequestHandlerWrapperBase<TResponse>
    {
        public abstract Task<TResponse> Handle(object request, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(object request, IServiceProvider sp, CancellationToken ct)
        {
            var typed = (TRequest)request;
            var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline = c => handler.Handle(typed, c);

            // Behavior-lar tərs sırada əhatə olunur ki, qeydiyyat sırası ilə icra olunsunlar.
            foreach (var behavior in sp.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var next = pipeline;
                var current = behavior;
                pipeline = c => current.Handle(typed, next, c);
            }

            return pipeline(ct);
        }
    }
}
