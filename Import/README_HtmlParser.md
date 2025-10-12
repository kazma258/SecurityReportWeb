# HTML 解析服務實作指南

## 概述

此專案需要將上傳的 ZAP HTML 報告解析為結構化資料並匯入資料庫。

## 架構流程

```
使用者上傳 HTML → ImportController.UploadHtml()
                        ↓
                   IHtmlParserService.ParseZapReportAsync()
                        ↓
                   ImportRequestDto (結構化資料)
                        ↓
                   IImportService.ImportAsync()
                        ↓
                   寫入資料庫 → 回傳結果
```

## 團隊成員任務

### 實作 `IHtmlParserService`

目前使用 `HtmlParserServiceStub`（佔位實作），會拋出 `NotImplementedException`。

請建立新的類別實作實際的 HTML 解析邏輯：

```csharp
public class HtmlParserService : IHtmlParserService
{
    public async Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null)
    {
        var result = new ImportRequestDto
        {
            UrlLists = new List<UrlListImportDto>(),
            RiskDescriptions = new List<RiskDescriptionImportDto>(),
            ZapReports = new List<ZapReportImportDto>(),
            ZapAlerts = new List<ZapAlertDetailImportDto>(),
            ReplaceAlertsForSubmittedDays = true
        };

        // TODO: 解析 HTML，提取以下資訊
        
        // 1. 站點基本資訊 → UrlLists
        // 2. 風險類型描述 → RiskDescriptions
        // 3. 報告元資訊 → ZapReports
        // 4. 每個警告細節 → ZapAlerts

        return result;
    }
}
```

## 需要解析的資料結構

### 1. UrlListImportDto（站點清單）

```csharp
new UrlListImportDto
{
    Url = "https://example.com",              // 必填：站點 URL
    WebName = "範例網站主機",                   // 必填：唯一識別名稱
    Ip = "1.2.3.4",                           // 選填
    UnitName = "IT部門",                       // 必填
    Manager = "張三",                          // 選填
    ManagerMail = "user@example.com",         // 選填
    OutsourcedVendor = "XX公司",               // 選填
    Remark = "備註",                           // 選填
    RiskReportLink = "report.html",           // 必填
    UploadDate = DateOnly.FromDateTime(DateTime.Now) // 必填
}
```

### 2. RiskDescriptionImportDto（風險描述）

```csharp
new RiskDescriptionImportDto
{
    Name = "SQL Injection",                   // 必填：風險名稱
    Description = "詳細描述...",               // 選填
    Solution = "解決方案...",                  // 選填
    Reference = "https://...",                // 選填
    Cweid = 89,                               // 選填：CWE ID
    Wascid = 19,                              // 選填：WASC ID
    PluginId = 40018                          // 選填：ZAP Plugin ID
}
// 注意：Signature 由系統自動計算，不需提供
```

### 3. ZapReportImportDto（報告資訊）

```csharp
new ZapReportImportDto
{
    SiteWebName = "範例網站主機",              // 必填：對應 UrlList.WebName
    GeneratedDate = DateTime.Parse("2025-10-12T10:00:00Z"), // 必填
    GeneratedDay = DateOnly.Parse("2025-10-12"),            // 必填
    Zapversion = "2.16.1",                    // 必填
    Zapsupporter = "OWASP",                   // 必填
    IsDeleted = false                         // 必填
}
```

### 4. ZapAlertDetailImportDto（警告細節）

```csharp
new ZapAlertDetailImportDto
{
    RootWebName = "範例網站主機",              // 必填：對應 UrlList.WebName
    Url = "https://example.com/page1",        // 必填：具體 URL
    ReportDate = DateTime.Parse("2025-10-12T10:00:00Z"), // 必填
    ReportDay = DateOnly.Parse("2025-10-12"), // 必填
    RiskName = "SQL Injection",               // 必填：對應風險名稱
    Level = "High",                           // 必填：Low/Medium/High/Informational
    Method = "GET",                           // 必填：HTTP 方法
    Parameter = "id",                         // 選填：參數名稱
    Attack = "' OR '1'='1",                   // 選填：攻擊內容
    Evidence = "<script>alert(1)</script>",   // 選填：證據片段
    Status = "Open",                          // 選填（預設 "Open"）
    OtherInfo = "額外資訊..."                  // 選填
}
```

## 關鍵注意事項

### 1. **WebName 必須一致**
`WebName` 是關聯的樞紐，以下三處必須使用**相同的值**：
- `UrlListImportDto.WebName`
- `ZapReportImportDto.SiteWebName`
- `ZapAlertDetailImportDto.RootWebName`

### 2. **UrlId 自動計算**
不需手動產生 `UrlId`，系統會根據 `Url` 內容自動計算確定性 GUID。

### 3. **Signature 自動計算**
`RiskDescription` 的 `Signature` 會根據內容欄位自動計算，不需提供。

### 4. **JSON 轉義**
- 換行符號：`\n`
- 雙引號：`\"`
- 反斜線：`\\`

建議使用 `System.Text.Json` 或 `Newtonsoft.Json` 自動處理轉義。

## 建議的解析策略

### 使用 HtmlAgilityPack

```bash
dotnet add package HtmlAgilityPack
```

```csharp
using HtmlAgilityPack;

public class HtmlParserService : IHtmlParserService
{
    public async Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var result = new ImportRequestDto();

        // 提取站點 URL（範例）
        var siteUrlNode = doc.DocumentNode.SelectSingleNode("//div[@id='about']//a");
        if (siteUrlNode != null)
        {
            result.UrlLists.Add(new UrlListImportDto
            {
                Url = siteUrlNode.GetAttributeValue("href", ""),
                WebName = ExtractWebName(siteUrlNode.InnerText),
                // ...其他欄位
            });
        }

        // 提取警告清單（範例）
        var alertNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'alert-item')]");
        if (alertNodes != null)
        {
            foreach (var node in alertNodes)
            {
                result.ZapAlerts.Add(new ZapAlertDetailImportDto
                {
                    RiskName = node.SelectSingleNode(".//h3")?.InnerText,
                    Url = node.SelectSingleNode(".//a")?.GetAttributeValue("href", ""),
                    // ...其他欄位
                });
            }
        }

        return await Task.FromResult(result);
    }
}
```

## 測試方式

### 1. 替換實作（Program.cs）

```csharp
// 從
builder.Services.AddScoped<IHtmlParserService, HtmlParserServiceStub>();

// 改為
builder.Services.AddScoped<IHtmlParserService, HtmlParserService>();
```

### 2. 使用 curl 測試

```bash
curl -X POST http://localhost:8080/api/import/upload-html \
  -F "file=@report.html"
```

### 3. 使用 Swagger UI

1. 啟動專案
2. 瀏覽 `http://localhost:8080/swagger`
3. 找到 `POST /api/import/upload-html`
4. 上傳 HTML 檔案

## API 端點

### 主要端點：上傳 HTML
- **URL**: `POST /api/import/upload-html`
- **Content-Type**: `multipart/form-data`
- **參數**: `file` (HTML 檔案)

### 備用端點：直接提交 JSON（測試用）
- **URL**: `POST /api/import/json`
- **Content-Type**: `application/json`
- **Body**: `ImportRequestDto` 物件

## 回傳格式

```json
{
  "fileName": "report.html",
  "fileSize": 123456,
  "result": {
    "urlListsInserted": 1,
    "urlListsUpdated": 0,
    "riskDescriptionsInserted": 5,
    "riskDescriptionsUpdated": 0,
    "zapReportsInserted": 1,
    "zapReportsUpdated": 0,
    "zapReportsSkipped": 0,
    "zapAlertsInserted": 23,
    "zapAlertsSkipped": 0,
    "warnings": [],
    "skippedReasons": []
  }
}
```

## 錯誤處理

如果解析失敗，應拋出有意義的例外：

```csharp
throw new InvalidOperationException($"無法在 HTML 中找到站點 URL（檔案：{fileName}）");
```

系統會自動捕捉並回傳 HTTP 500 錯誤與訊息。

## 聯絡資訊

如有問題請聯繫團隊負責人。

