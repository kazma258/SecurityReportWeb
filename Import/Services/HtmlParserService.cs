using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SecurityReportWeb.Import.Dtos;

namespace SecurityReportWeb.Import.Services;

/// <summary>
/// HTML 解析服務實作
/// 負責解析 ZAP HTML 報告並轉換為 ImportRequestDto
/// </summary>
public class HtmlParserService : IHtmlParserService
{
    /// <summary>
    /// 解析 ZAP HTML 報告
    /// </summary>
    public Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException("HTML 內容不能為空", nameof(htmlContent));
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var result = new ImportRequestDto
        {
            ReplaceAlertsForSubmittedDays = true
        };

        // 解析基本信息
        var (siteUrl, generatedDate, zapVersion, zapSupporter) = ParseBasicInfo(doc);

        // 從 Site URL 提取 WebName（使用 hostname）
        var webName = ExtractWebName(siteUrl);
        var generatedDay = DateOnly.FromDateTime(generatedDate);

        // 建立 ZapReportImportDto
        result.ZapReports.Add(new ZapReportImportDto
        {
            SiteWebName = webName,
            GeneratedDate = generatedDate,
            GeneratedDay = generatedDay,
            Zapversion = zapVersion,
            Zapsupporter = zapSupporter,
            IsDeleted = false
        });

        // 解析所有警告
        ParseAlerts(doc, result, webName, generatedDate, generatedDay);

        return Task.FromResult(result);
    }

    /// <summary>
    /// 解析報告基本信息
    /// </summary>
    private (string siteUrl, DateTime generatedDate, string zapVersion, string zapSupporter) ParseBasicInfo(HtmlDocument doc)
    {
        // 解析 Site URL (在 h2 標籤中)
        var h2Nodes = doc.DocumentNode.SelectNodes("//h2");
        var siteUrl = string.Empty;
        if (h2Nodes != null)
        {
            foreach (var h2 in h2Nodes)
            {
                var text = h2.InnerText.Trim();
                if (text.StartsWith("Site:", StringComparison.OrdinalIgnoreCase))
                {
                    siteUrl = text.Replace("Site:", "", StringComparison.OrdinalIgnoreCase).Trim();
                    break;
                }
            }
        }

        // 解析 Generated Date 和 ZAP Version (在 h3 標籤中)
        var h3Nodes = doc.DocumentNode.SelectNodes("//h3");
        var generatedDate = DateTime.Now;
        var zapVersion = string.Empty;

        if (h3Nodes != null)
        {
            foreach (var h3 in h3Nodes)
            {
                var text = h3.InnerText.Trim();

                if (text.StartsWith("Generated on", StringComparison.OrdinalIgnoreCase))
                {
                    var dateStr = text.Replace("Generated on", "", StringComparison.OrdinalIgnoreCase).Trim();
                    generatedDate = ParseGeneratedDate(dateStr);
                }
                else if (text.StartsWith("ZAP Version:", StringComparison.OrdinalIgnoreCase))
                {
                    zapVersion = text.Replace("ZAP Version:", "", StringComparison.OrdinalIgnoreCase).Trim();
                }
            }
        }

        // 解析 ZAP Supporter (在 h4 標籤中)
        var h4Nodes = doc.DocumentNode.SelectNodes("//h4");
        var zapSupporter = "Checkmarx"; // 預設值
        if (h4Nodes != null)
        {
            foreach (var h4 in h4Nodes)
            {
                var text = h4.InnerText.Trim();
                if (text.StartsWith("ZAP by", StringComparison.OrdinalIgnoreCase))
                {
                    zapSupporter = text.Replace("ZAP by", "", StringComparison.OrdinalIgnoreCase).Trim();
                    break;
                }
            }
        }

        return (siteUrl, generatedDate, zapVersion, zapSupporter);
    }

    /// <summary>
    /// 解析生成日期字串
    /// 支援格式：週五, 27 6月 2025 16:14:56
    /// </summary>
    private DateTime ParseGeneratedDate(string dateStr)
    {
        try
        {
            // 移除開頭的星期文字 (例如：週五, 或 Friday,)
            var match = Regex.Match(dateStr, @"[\u4e00-\u9fa5]+,\s*(.+)");
            if (match.Success)
            {
                dateStr = match.Groups[1].Value;
            }
            else
            {
                match = Regex.Match(dateStr, @"[A-Za-z]+,\s*(.+)");
                if (match.Success)
                {
                    dateStr = match.Groups[1].Value;
                }
            }

            // 嘗試解析繁體中文格式：27 6月 2025 16:14:56
            match = Regex.Match(dateStr, @"(\d+)\s+(\d+)月\s+(\d+)\s+([\d:]+)");
            if (match.Success)
            {
                var day = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var year = int.Parse(match.Groups[3].Value);
                var time = match.Groups[4].Value;
                var timeParts = time.Split(':');
                var hour = int.Parse(timeParts[0]);
                var minute = int.Parse(timeParts[1]);
                var second = timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0;

                return new DateTime(year, month, day, hour, minute, second);
            }

            // 嘗試標準日期格式解析
            if (DateTime.TryParse(dateStr, out var result))
            {
                return result;
            }

            // 如果都失敗，返回當前時間
            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }

    /// <summary>
    /// 從 URL 提取 WebName（使用 hostname）
    /// </summary>
    private string ExtractWebName(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }
            return url;
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// 解析所有警告
    /// </summary>
    private void ParseAlerts(HtmlDocument doc, ImportRequestDto result, string webName, DateTime generatedDate, DateOnly generatedDay)
    {
        // 選擇所有警告表格 (table.results)
        var alertTables = doc.DocumentNode.SelectNodes("//table[@class='results']");
        if (alertTables == null) return;

        foreach (var table in alertTables)
        {
            try
            {
                ParseSingleAlert(table, result, webName, generatedDate, generatedDay);
            }
            catch (Exception)
            {
                // 忽略單個警告解析失敗，繼續處理下一個
                continue;
            }
        }
    }

    /// <summary>
    /// 解析單個警告
    /// </summary>
    private void ParseSingleAlert(HtmlNode table, ImportRequestDto result, string webName, DateTime generatedDate, DateOnly generatedDay)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null || rows.Count == 0) return;

        // 第一行包含風險等級和警告名稱
        var headerRow = rows[0];
        var thNodes = headerRow.SelectNodes(".//th");
        if (thNodes == null || thNodes.Count < 2) return;

        var riskLevel = thNodes[0].InnerText.Trim();
        var alertName = thNodes[1].InnerText.Trim();

        // 解析警告的詳細信息
        var riskDescription = ParseRiskDescription(rows);
        if (riskDescription != null)
        {
            riskDescription.Name = alertName;

            // 檢查是否已存在相同的風險描述（避免重複）
            if (!result.RiskDescriptions.Any(r => r.Name == riskDescription.Name))
            {
                result.RiskDescriptions.Add(riskDescription);
            }
        }

        // 解析警告的所有實例
        ParseAlertInstances(rows, result, webName, generatedDate, generatedDay, alertName, riskLevel);
    }

    /// <summary>
    /// 解析風險描述信息
    /// </summary>
    private RiskDescriptionImportDto? ParseRiskDescription(IList<HtmlNode> rows)
    {
        var riskDesc = new RiskDescriptionImportDto();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 2) continue;

            var label = cells[0].InnerText.Trim();
            var value = cells[1];

            switch (label)
            {
                case "Description":
                    riskDesc.Description = ExtractTextFromMultipleDivs(value);
                    break;
                case "Solution":
                    riskDesc.Solution = ExtractTextFromMultipleDivs(value);
                    break;
                case "Reference":
                    riskDesc.Reference = ExtractTextFromMultipleDivs(value);
                    break;
                case "CWE Id":
                    var cweText = value.InnerText.Trim();
                    if (int.TryParse(cweText, out var cweId))
                    {
                        riskDesc.Cweid = cweId;
                    }
                    break;
                case "WASC Id":
                    var wascText = value.InnerText.Trim();
                    if (int.TryParse(wascText, out var wascId))
                    {
                        riskDesc.Wascid = wascId;
                    }
                    break;
                case "Plugin Id":
                    var pluginText = value.InnerText.Trim();
                    if (int.TryParse(pluginText, out var pluginId))
                    {
                        riskDesc.PluginId = pluginId;
                    }
                    break;
            }
        }

        // 如果有任何有效的描述信息，就返回
        if (!string.IsNullOrWhiteSpace(riskDesc.Description) ||
            !string.IsNullOrWhiteSpace(riskDesc.Solution))
        {
            return riskDesc;
        }

        return null;
    }

    /// <summary>
    /// 從包含多個 div 的節點中提取文字
    /// </summary>
    private string ExtractTextFromMultipleDivs(HtmlNode node)
    {
        var divs = node.SelectNodes(".//div");
        if (divs != null && divs.Count > 0)
        {
            var texts = divs.Select(d => HtmlEntity.DeEntitize(d.InnerText.Trim()))
                           .Where(t => !string.IsNullOrWhiteSpace(t));
            return string.Join("\n", texts);
        }

        // 如果沒有 div，直接取 a 標籤或文字
        var aNode = node.SelectSingleNode(".//a");
        if (aNode != null)
        {
            return HtmlEntity.DeEntitize(aNode.InnerText.Trim());
        }

        return HtmlEntity.DeEntitize(node.InnerText.Trim());
    }

    /// <summary>
    /// 解析警告的所有實例
    /// </summary>
    private void ParseAlertInstances(IList<HtmlNode> rows, ImportRequestDto result, string webName,
        DateTime generatedDate, DateOnly generatedDay, string alertName, string riskLevel)
    {
        ZapAlertDetailImportDto? currentAlert = null;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 2) continue;

            var label = cells[0].InnerText.Trim();
            var valueCell = cells[1];

            // 檢查是否是 URL (新實例的開始)
            if (label == "URL" || label.Contains("indent1"))
            {
                // 如果有之前的警告，先保存
                if (currentAlert != null && !string.IsNullOrWhiteSpace(currentAlert.Url))
                {
                    result.ZapAlerts.Add(currentAlert);
                }

                // 開始新的警告實例
                var aNode = valueCell.SelectSingleNode(".//a");
                var url = aNode != null ? aNode.GetAttributeValue("href", "") : valueCell.InnerText.Trim();

                currentAlert = new ZapAlertDetailImportDto
                {
                    RootWebName = webName,
                    Url = HtmlEntity.DeEntitize(url),
                    ReportDate = generatedDate,
                    ReportDay = generatedDay,
                    RiskName = alertName,
                    Level = riskLevel,
                    Method = string.Empty,
                    Parameter = null,
                    Attack = null,
                    Evidence = null,
                    Status = null,
                    OtherInfo = null
                };
            }
            else if (currentAlert != null)
            {
                // 填充當前警告的詳細信息
                var value = HtmlEntity.DeEntitize(valueCell.InnerText.Trim());

                if (label == "方法" || label == "Method")
                {
                    currentAlert.Method = value;
                }
                else if (label == "Parameter")
                {
                    currentAlert.Parameter = string.IsNullOrWhiteSpace(value) ? null : value;
                }
                else if (label == "攻擊" || label == "Attack")
                {
                    currentAlert.Attack = string.IsNullOrWhiteSpace(value) ? null : value;
                }
                else if (label == "Evidence")
                {
                    currentAlert.Evidence = string.IsNullOrWhiteSpace(value) ? null : value;
                }
                else if (label == "Other Info")
                {
                    currentAlert.OtherInfo = string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
        }

        // 保存最後一個警告實例
        if (currentAlert != null && !string.IsNullOrWhiteSpace(currentAlert.Url))
        {
            result.ZapAlerts.Add(currentAlert);
        }
    }
}

