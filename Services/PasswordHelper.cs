using System.Security.Cryptography;

namespace SecurityReportWeb.Services;

/// <summary>
/// 密碼雜湊輔助類別
/// </summary>
public static class PasswordHelper
{
    /// <summary>
    /// 雜湊密碼（使用 PBKDF2 with SHA256）
    /// </summary>
    /// <param name="password">明文密碼</param>
    /// <returns>Base64 編碼的雜湊值（包含 Salt）</returns>
    public static string HashPassword(string password)
    {
        // 生成隨機 Salt
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 使用 PBKDF2 雜湊密碼
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        // 組合 Salt 和 Hash（Salt + Hash）
        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 驗證密碼
    /// </summary>
    /// <param name="password">明文密碼</param>
    /// <param name="passwordHash">雜湊後的密碼</param>
    /// <returns>是否驗證成功</returns>
    public static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var hash = new byte[32];
            Array.Copy(hashBytes, 16, hash, 0, 32);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(32);

            return computedHash.SequenceEqual(hash);
        }
        catch
        {
            return false;
        }
    }
}

