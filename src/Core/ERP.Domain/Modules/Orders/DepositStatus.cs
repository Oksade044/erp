namespace ERP.Domain.Modules.Orders;

/// <summary>
/// Depozitin vəziyyəti (#37). Hesablaşmaya (Settle) və tutulmalara görə hesablanır:
/// Yoxdur — depozit qoyulmayıb; Alındı — depozit alınıb, hələ hesablaşılmayıb;
/// Qaytarıldı — tam geri qaytarıldı (tutulma yox); Qismən — bir hissəsi tutuldu;
/// Saxlanıldı — tam saxlanıldı (tutulmalar depoziti udub).
/// </summary>
public enum DepositStatus
{
    Yoxdur = 0,
    Alındı = 1,
    Qaytarıldı = 2,
    Qismən = 3,
    Saxlanıldı = 4
}
