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
    public const string OrdersView = "orders.view";
    public const string OrdersEdit = "orders.edit";
    public const string OrdersApprove = "orders.approve";
    public const string InvoicesView = "invoices.view";
    public const string InvoicesEdit = "invoices.edit";
    public const string ReportsView = "reports.view";
    public const string UsersManage = "users.manage";

    /// <summary>Rol → icazələr xəritəsi. Admin hər şeyi bacarır.</summary>
    public static IReadOnlyCollection<string> ForRole(Role role) => role switch
    {
        Role.Admin =>
        [
            CustomersView, CustomersEdit, ProductsView, ProductsEdit,
            OrdersView, OrdersEdit, OrdersApprove, InvoicesView, InvoicesEdit,
            ReportsView, UsersManage
        ],
        Role.Menecer =>
        [
            CustomersView, CustomersEdit, ProductsView, ProductsEdit,
            OrdersView, OrdersEdit, OrdersApprove, InvoicesView, InvoicesEdit, ReportsView
        ],
        Role.Anbardar =>
        [
            ProductsView, ProductsEdit, OrdersView, OrdersEdit
        ],
        Role.Kassir =>
        [
            CustomersView, OrdersView, InvoicesView, InvoicesEdit
        ],
        _ => []
    };
}
