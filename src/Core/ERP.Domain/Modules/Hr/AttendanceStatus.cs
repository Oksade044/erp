namespace ERP.Domain.Modules.Hr;

/// <summary>İşçinin bir gündəki davamiyyət statusu.</summary>
public enum AttendanceStatus
{
    Gəlib = 1,       // Present
    Gəlməyib = 2,    // Absent
    Məzuniyyət = 3,  // On leave
    Xəstə = 4,       // Sick
    Yarımgün = 5     // Half day
}
