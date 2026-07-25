using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Products;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Bütün kateqoriyaları əlifba sırası ilə qaytarır (məhsul formasında seçim üçün).</summary>
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesHandler(ICategoryRepository categories)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var list = await categories.ListOrderedAsync(ct);
        return list.Select(c => new CategoryDto(c.Id, c.Name, c.IsActive)).ToList();
    }
}
