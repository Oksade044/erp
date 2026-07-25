using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;
using ERP.Shared.Contracts.Products;
using FluentValidation;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Yeni kateqoriya yaradır (təkrar ad → mövcud kateqoriya qaytarılır, xəta yox).</summary>
public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator() =>
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
}

public sealed class CreateCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var name = request.Request.Name.Trim();

        // Artıq varsa, onu qaytar (idempotent — yazarkən təkrar yaranmasın).
        var existing = await categories.GetByNameAsync(name, ct);
        if (existing is not null)
            return Result.Success(existing.Id);

        var category = Category.Create(name);
        await categories.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(category.Id);
    }
}
