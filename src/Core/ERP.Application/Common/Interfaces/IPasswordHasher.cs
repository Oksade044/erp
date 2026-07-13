namespace ERP.Application.Common.Interfaces;

/// <summary>Parol hash-ləmə (TDD §39). Parol heç vaxt açıq saxlanmır.</summary>
public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
}
