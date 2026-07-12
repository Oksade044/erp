using ERP.Domain.Exceptions;
using FluentValidation;

namespace ERP.Api.Middleware;

/// <summary>
/// Global exception middleware (TDD §21). Gözlənilməz xətaları tutur, RFC 7807 ProblemDetails
/// qaytarır, daxili detalları sızdırmır. Domain/validation xətaları mənalı 400 verir.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            logger.LogWarning("Validasiya xətası: {Errors}", string.Join("; ", errors));
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                "Validasiya xətası", string.Join(" ", errors));
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain xətası: {Message}", ex.Message);
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Biznes qaydası pozuldu", ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            // Yanlış/oxuna bilməyən request gövdəsi client xətasıdır (400), server xətası deyil.
            logger.LogWarning("Yanlış sorğu: {Message}", ex.Message);
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                "Yanlış sorğu", "Sorğu gövdəsi düzgün deyil (JSON/UTF-8 yoxlayın).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gözlənilməz xəta");
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Server xətası", "Gözlənilməz xəta baş verdi.");
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail
        });
    }
}
