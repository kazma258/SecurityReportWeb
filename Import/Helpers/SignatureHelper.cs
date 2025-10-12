using System;
using System.Security.Cryptography;
using System.Text;

namespace SecurityReportWeb.Import.Helpers;

public static class SignatureHelper
{
    /// <summary>
    /// 從字串計算確定性 GUID（使用 SHA256 前 16 bytes）
    /// </summary>
    public static Guid ComputeGuidFromString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        
        // 取前 16 bytes 作為 GUID
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        
        return new Guid(guidBytes);
    }

    /// <summary>
    /// 計算 UrlList 的 UrlId（基於 Url 內容）
    /// </summary>
    public static Guid ComputeUrlId(string url)
    {
        return ComputeGuidFromString(url);
    }

    /// <summary>
    /// 計算 ZapReport 的 ReportId（基於 SiteUrlId + GeneratedDay）
    /// </summary>
    public static Guid ComputeReportId(Guid siteUrlId, DateOnly generatedDay)
    {
        var combined = $"{siteUrlId:N}|{generatedDay:yyyy-MM-dd}";
        return ComputeGuidFromString(combined);
    }

    /// <summary>
    /// 計算 RiskDescription 的 RiskId（基於 Name + Signature）
    /// </summary>
    public static Guid ComputeRiskId(string name, string signature)
    {
        var combined = $"{name}|{signature}";
        return ComputeGuidFromString(combined);
    }

    /// <summary>
    /// 計算 RiskDescription 的內容指紋（基於 Name/Description/Solution/Reference/CWEId/WASCId/PluginID）
    /// </summary>
    public static string ComputeRiskSignature(
        string name,
        string? description,
        string? solution,
        string? reference,
        int? cweId,
        int? wascId,
        int? pluginId)
    {
        var content = new StringBuilder();
        content.Append(name ?? string.Empty);
        content.Append('|');
        content.Append(description ?? string.Empty);
        content.Append('|');
        content.Append(solution ?? string.Empty);
        content.Append('|');
        content.Append(reference ?? string.Empty);
        content.Append('|');
        content.Append(cweId?.ToString() ?? string.Empty);
        content.Append('|');
        content.Append(wascId?.ToString() ?? string.Empty);
        content.Append('|');
        content.Append(pluginId?.ToString() ?? string.Empty);

        var bytes = Encoding.UTF8.GetBytes(content.ToString());
        var hash = SHA256.HashData(bytes);
        
        // 回傳 Base64（較短）或 Hex（較易讀）
        return Convert.ToBase64String(hash);
    }
}

