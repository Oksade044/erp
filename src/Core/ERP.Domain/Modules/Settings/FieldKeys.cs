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

    /// <summary>Müştərinin borc məbləği (Müştərilər siyahısındakı "Borc" sütunu).</summary>
    public const string CustomerDebt = "customer.debt";

    /// <summary>Açar → istifadəçi üçün aydın ad.</summary>
    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>
        {
            [ProductCost] = "Məhsul alış/satış qiyməti",
            [OrderCreator] = "Sifarişi kim yaradıb",
            [EmployeeSalary] = "İşçi maaşı",
            [CustomerDebt] = "Müştəri borcu",
        };

    public static IReadOnlyList<string> All => [ProductCost, OrderCreator, EmployeeSalary, CustomerDebt];

    public static string DisplayName(string key) =>
        DisplayNames.TryGetValue(key, out var name) ? name : key;
}
