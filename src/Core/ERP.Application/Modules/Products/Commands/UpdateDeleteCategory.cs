using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using FluentValidation;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Kateqoriyanın adını dəyişir.</summary>
public sealed record UpdateCategoryCommand(Guid Id, string Name) : IRequest<Result>;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateCategoryHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct);
        if (category is null) return Result.Failure($"Kateqoriya tapılmadı: {request.Id}");
        category.Rename(request.Name);
        categories.Update(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>Kateqoriyanı silir (soft delete).</summary>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCategoryHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(request.Id, ct);
        if (category is null) return Result.Failure($"Kateqoriya tapılmadı: {request.Id}");
        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
