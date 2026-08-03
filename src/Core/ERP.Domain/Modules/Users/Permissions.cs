namespace ERP.Domain.Modules.Users;

/// <summary>
/// Atomik icazələr (TDD §7). Kod `if role=="Admin"` yazmır — `RequirePermission("orders.approve")`
/// yazır. Rollar dəyişəndə kod qırılmır.
/// </summary>
public static class Permissions
{
    public const string CustomersView = "customers.view";
    public const string CustomersEdit = "customers.edit";
    public const string ProductsView = "products.view";
    public const string ProductsEdit = "products.edit";
    /// <summary>Həssas qiymətləri (alış/maya, satış) görmək icazəsi — yalnız Admin/Menecer.</summary>
    public const string ProductsViewCost = "products.viewcost";
    public const string OrdersView = "orders.view";
    public const string OrdersEdit = "orders.edit";
    public const string OrdersApprove = "orders.approve";
    public const string InvoicesView = "invoices.view";
    public const string InvoicesEdit = "invoices.edit";
    public const string SuppliersView = "suppliers.view";
    public const string SuppliersEdit = "suppliers.edit";
    public const string PurchasesView = "purchases.view";
    public const string PurchasesEdit = "purchases.edit";
    public const string FinanceView = "finance.view";
    public const string FinanceEdit = "finance.edit";
    public const string HrView = "hr.view";
    public const string HrEdit = "hr.edit";
    public const string WarehousesView = "warehouses.view";
    public const string WarehousesEdit = "warehouses.edit";
    public const string ReportsView = "reports.view";
    /// <summary>Təmsilçi borcları / hesabatı (satış-kassa sahəsi, HR deyil).</summary>
    public const string RepresentativesView = "representatives.view";
    public const string RepresentativesEdit = "representatives.edit";
    public const string UsersManage = "users.manage";
    /// <summary>Audit jurnalını görmək — yalnız səlahiyyətli (Admin).</summary>
    public const string AuditView = "audit.view";

    /// <summary>Bütün icazələr + istifadəçi üçün aydın adları (#16 — rol matrisi UI).</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> Catalog =
    [
        (CustomersView, "Müştərilər (+ Borclar) — bax"), (CustomersEdit, "Müştərilər — dəyiş"),
        (ProductsView, "Məhsullar (+ Kateqoriyalar) — bax"), (ProductsEdit, "Məhsullar/Kateqoriyalar — dəyiş"),
        (ProductsViewCost, "Məhsul alış/satış qiyməti — bax"),
        (OrdersView, "Sifarişlər (+ Bron/İcarə) — bax"), (OrdersEdit, "Sifarişlər — dəyiş"),
        (OrdersApprove, "Sifariş — təsdiqlə"),
        (InvoicesView, "Fakturalar — bax"), (InvoicesEdit, "Fakturalar — dəyiş/ödəniş"),
        (SuppliersView, "Təchizatçılar — bax"), (SuppliersEdit, "Təchizatçılar — dəyiş"),
        (PurchasesView, "Alışlar — bax"), (PurchasesEdit, "Alışlar — dəyiş"),
        (FinanceView, "Maliyyə (+ Kassa) — bax"), (FinanceEdit, "Maliyyə/Kassa — dəyiş"),
        (RepresentativesView, "Təmsilçi borcları — bax"), (RepresentativesEdit, "Təmsilçi borcları — dəyiş"),
        (HrView, "HR: İşçilər/Davamiyyət/Əməkhaqqı — bax"), (HrEdit, "HR — dəyiş"),
        (WarehousesView, "Anbarlar/Stok — bax"), (WarehousesEdit, "Anbarlar/Stok — dəyiş"),
        (ReportsView, "Hesabatlar (+ Təqvim) — bax"),
        (UsersManage, "İstifadəçilər & Rollar & Sahə icazələri — idarə et"),
        (AuditView, "Audit jurnalı — bax"),
    ];

    /// <summary>Rol → icazələr xəritəsi. Admin hər şeyi bacarır.</summary>
    public static IReadOnlyCollection<string> ForRole(Role role) => role switch
    {
        Role.Admin =>
        [
            CustomersView, CustomersEdit, ProductsView, ProductsEdit, ProductsViewCost,
            OrdersView, OrdersEdit, OrdersApprove, InvoicesView, InvoicesEdit,
            SuppliersView, SuppliersEdit, PurchasesView, PurchasesEdit,
            FinanceView, FinanceEdit, RepresentativesView, RepresentativesEdit, HrView, HrEdit,
            WarehousesView, WarehousesEdit, ReportsView, UsersManage, AuditView
        ],
        Role.Menecer =>
        [
            CustomersView, CustomersEdit, ProductsView, ProductsEdit, ProductsViewCost,
            OrdersView, OrdersEdit, OrdersApprove, InvoicesView, InvoicesEdit,
            SuppliersView, SuppliersEdit, PurchasesView, PurchasesEdit,
            FinanceView, FinanceEdit, RepresentativesView, RepresentativesEdit, HrView, HrEdit,
            WarehousesView, WarehousesEdit, ReportsView
        ],
        Role.Anbardar =>
        [
            ProductsView, ProductsEdit, OrdersView, OrdersEdit,
            SuppliersView, PurchasesView, PurchasesEdit,
            WarehousesView, WarehousesEdit
        ],
        Role.Kassir =>
        [
            CustomersView, OrdersView, InvoicesView, InvoicesEdit,
            FinanceView, FinanceEdit, RepresentativesView, RepresentativesEdit
        ],
        _ => []
    };
}
