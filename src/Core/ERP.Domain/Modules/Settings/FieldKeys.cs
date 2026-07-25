namespace ERP.Domain.Modules.Settings;

/// <summary>
/// Görünürlüyü Admin/Menecer tərəfindən tənzimlənə bilən həssas sahələrin kataloqu.
/// Yeni həssas sahə əlavə etmək üçün buraya açar + izah yazılır.
/// </summary>
public static class FieldKeys
{
    /// <summary>Məhsulun alış (maya) və satış qiyməti.</summary>
    public const string ProductCost = "product.cost";

    /// <summary>Sifarişi kim yaradıb (yaradan ad + rol).</summary>
    public const string OrderCreator = "order.creator";

    /// <summary>İşçinin maaşı (əməkhaqqı məbləğləri).</summary>
    public const string EmployeeSalary = "employee.salary";

    /// <summary>Açar → istifadəçi üçün aydın ad.</summary>
    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>
        {
            [ProductCost] = "Məhsul alış/satış qiyməti",
            [OrderCreator] = "Sifarişi kim yaradıb",
            [EmployeeSalary] = "İşçi maaşı",
        };

    public static IReadOnlyList<string> All => [ProductCost, OrderCreator, EmployeeSalary];

    public static string DisplayName(string key) =>
        DisplayNames.TryGetValue(key, out var name) ? name : key;
}
