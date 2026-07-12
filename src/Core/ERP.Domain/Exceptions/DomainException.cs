namespace ERP.Domain.Exceptions;

/// <summary>
/// Domain invariantı pozulanda atılır (TDD §21). Mənalı, istifadəçiyə göstərilə bilən
/// biznes mesajı daşıyır. Global middleware bunu 400/422-yə çevirir.
/// </summary>
public class DomainException(string message) : Exception(message);
