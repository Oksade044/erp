namespace ERP.Shared.Contracts.Products;

/// <summary>Toplu idxal nəticəsi — neçə yaradıldı, neçə ötürüldü, xətalar.</summary>
public sealed record ImportResultDto(
    int Created,
    int Skipped,
    IReadOnlyList<string> Errors);
