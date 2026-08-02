namespace ERP.Domain.Modules.Hr;

/// <summary>Əməkhaqqı hesablamasının statusu.</summary>
public enum PayrollStatus
{
    Hesablanmış = 1,       // Calculated (draft) — heç nə ödənilməyib
    Ödənilmiş = 2,         // Paid — tam ödənilib
    QismənÖdənilmiş = 3    // Partially paid — hissə-hissə ödənilir
}
