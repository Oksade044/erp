namespace ERP.Domain.Modules.Hr;

/// <summary>Əməkhaqqı hesablamasının statusu.</summary>
public enum PayrollStatus
{
    Hesablanmış = 1,  // Calculated (draft)
    Ödənilmiş = 2     // Paid
}
