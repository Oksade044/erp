namespace ERP.Shared.Contracts.Reports;

/// <summary>Mənfəət/Zərər (P&L) hesabatı — dövr üzrə gəlir, xərc, xalis mənfəət + kateqoriya bölgüsü.</summary>
public sealed record ProfitLossDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    string Currency,
    IReadOnlyList<CategoryAmountDto> IncomeByCategory,
    IReadOnlyList<CategoryAmountDto> ExpenseByCategory);

/// <summary>Kateqoriya + məbləğ (P&L bölgüsü / qrafik üçün).</summary>
public sealed record CategoryAmountDto(string Category, decimal Amount);

/// <summary>Aylıq gəlir/xərc analitikası (qrafik üçün 12 nöqtə).</summary>
public sealed record MonthlyRevenueDto(
    int Year,
    string Currency,
    IReadOnlyList<MonthlyPointDto> Points);

public sealed record MonthlyPointDto(int Month, decimal Income, decimal Expense, decimal Net);
