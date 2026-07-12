namespace ERP.Domain.Modules.Customers;

/// <summary>
/// Müştəri növü. Fərdi müştəri (toy, şəxsi tədbir) və ya korporativ (şirkət, agentlik).
/// Korporativ müştəri üçün VÖEN tələb oluna bilər.
/// </summary>
public enum CustomerType
{
    Fərdi = 1,
    Korporativ = 2
}
