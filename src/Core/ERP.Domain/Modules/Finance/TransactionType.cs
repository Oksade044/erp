namespace ERP.Domain.Modules.Finance;

/// <summary>Maliyyə əməliyyatının növü — kassaya giriş və ya çıxış.</summary>
public enum TransactionType
{
    Mədaxil = 1,   // Income — kassaya daxil olan pul (gəlir)
    Məxaric = 2    // Expense — kassadan çıxan pul (xərc)
}
