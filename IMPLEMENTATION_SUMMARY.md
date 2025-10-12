# 專案實作總結

## ✅ 已完成功能

### 1. 資料庫架構
- ✅ 4 個主要資料表：`UrlLists`, `RiskDescription`, `ZAPReport`, `ZAPAlertDetail`
- ✅ 確定性 GUID 計算（相同內容產生相同 ID）
- ✅ 唯一鍵與索引設計

### 2. 匯入系統核心
- ✅ `ImportService`：智能 upsert、交易控制、錯誤回報
- ✅ `SignatureHelper`：自動計算 GUID 與內容指紋
- ✅ 自動關聯邏輯（透過 WebName）

### 3. API 端點

#### 主要端點：檔案上傳
```
POST /api/import/upload-html
Content-Type: multipart/form-data
參數: file (HTML 檔案)
```

- ✅ 接收 HTML 檔案上傳
- ✅ 自動呼叫解析服務
- ✅ 自動匯入資料庫
- ✅ 回傳詳細結果

#### 備用端點：JSON 匯入（測試用）
```
POST /api/import/json
Content-Type: application/json
```

### 4. HTML 解析架構
- ✅ `IHtmlParserService` 介面定義
- ✅ `HtmlParserServiceStub` 佔位實作（拋出 NotImplementedException）
- ✅ DI 註冊完成
- ⏳ **待團隊成員實作**：實際 HTML 解析邏輯

### 5. 文件與測試工具
- ✅ `Import/README_HtmlParser.md`：完整實作指南
- ✅ `test-upload.html`：測試用 HTML 範例
- ✅ `test-upload.ps1`：Windows PowerShell 測試腳本
- ✅ `test-upload.sh`：Linux/Mac Bash 測試腳本

## 📋 團隊成員待辦事項

### 實作 HTML 解析服務

1. **建立新類別**：`Import/Services/HtmlParserService.cs`

2. **實作 `IHtmlParserService` 介面**：
   ```csharp
   public class HtmlParserService : IHtmlParserService
   {
       public async Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null)
       {
           // TODO: 實作解析邏輯
       }
   }
   ```

3. **解析目標資料**：
   - 站點資訊 → `UrlListImportDto`
   - 風險類型 → `RiskDescriptionImportDto`
   - 報告元資訊 → `ZapReportImportDto`
   - 警告清單 → `ZapAlertDetailImportDto`

4. **註冊新實作**（`Program.cs`）：
   ```csharp
   // 從
   builder.Services.AddScoped<IHtmlParserService, HtmlParserServiceStub>();
   
   // 改為
   builder.Services.AddScoped<IHtmlParserService, HtmlParserService>();
   ```

5. **建議套件**：
   ```bash
   dotnet add package HtmlAgilityPack
   ```

### 關鍵注意事項

#### ⚠️ WebName 必須一致
所有關聯都透過 `WebName` 字串建立：
- `UrlList.webName`
- `ZapReport.siteWebName`
- `ZapAlert.rootWebName`

必須使用**完全相同**的值（大小寫敏感）。

#### 🔑 ID 自動計算
以下 ID 系統會自動產生，**不需手動設定**：
- `UrlId`：由 `Url` 內容計算
- `RiskId`：由 `Name + Signature` 計算
- `ReportId`：由 `SiteUrlId + GeneratedDay` 計算
- `Signature`：由風險描述內容計算

#### 📝 JSON 轉義
HTML 中的特殊字元需正確處理：
- 換行符號 → `\n`
- 雙引號 → `\"`
- 反斜線 → `\\`

建議使用標準 JSON 序列化器自動處理。

## 🧪 測試方式

### 1. 啟動專案
```bash
dotnet run
```

### 2. 測試檔案上傳（目前會回傳 HTTP 501）
```powershell
# Windows
.\test-upload.ps1

# 或使用 curl
curl -X POST http://localhost:8080/api/import/upload-html -F "file=@test-upload.html"
```

### 3. 測試 JSON 匯入（可用）
```bash
curl -X POST http://localhost:8080/api/import/json \
  -H "Content-Type: application/json" \
  -d @example-data.json
```

### 4. 使用 Swagger UI
瀏覽 `http://localhost:8080/swagger`

## 📊 預期回傳格式

### 成功（實作 HTML 解析後）
```json
{
  "fileName": "report.html",
  "fileSize": 123456,
  "result": {
    "urlListsInserted": 1,
    "riskDescriptionsInserted": 5,
    "zapReportsInserted": 1,
    "zapAlertsInserted": 23,
    "skippedReasons": []
  }
}
```

### 目前（尚未實作）
```json
{
  "error": "HTML 解析功能尚未實作",
  "message": "HTML 解析邏輯尚未實作。請在 HtmlParserService 中實作..."
}
```

## 🗂️ 專案結構

```
SecurityReportWeb/
├── Controllers/
│   ├── ImportController.cs       ✅ 檔案上傳與 JSON 匯入
│   └── DbTestController.cs
├── Database/
│   └── Models/
│       ├── ReportDbContext.cs
│       ├── UrlList.cs
│       ├── RiskDescription.cs
│       ├── Zapreport.cs
│       └── ZapalertDetail.cs
├── Import/
│   ├── Dtos/
│   │   └── ImportDtos.cs         ✅ 所有 DTO 定義
│   ├── Services/
│   │   ├── IImportService.cs     ✅ 匯入服務介面
│   │   ├── ImportService.cs      ✅ 匯入邏輯實作
│   │   ├── IHtmlParserService.cs ✅ 解析服務介面
│   │   └── HtmlParserServiceStub.cs ⏳ 佔位實作（待替換）
│   ├── Helpers/
│   │   └── SignatureHelper.cs    ✅ GUID 與指紋計算
│   └── README_HtmlParser.md      📘 實作指南
├── Migrations/
│   └── ...                       ✅ 資料庫遷移
├── Program.cs                    ✅ 啟動設定與 DI 註冊
├── test-upload.html              🧪 測試範例
├── test-upload.ps1               🧪 Windows 測試腳本
└── test-upload.sh                🧪 Linux/Mac 測試腳本
```

## 📞 問題排查

### 編譯警告
```
warning CS8618: 不可為 Null 的 屬性 'Signature' 必須包含非 Null 值
```
這是既有的警告，不影響功能。

### HTTP 501 Not Implemented
這是預期行為，表示 HTML 解析邏輯尚未實作。

### 資料未插入（ZapReports/ZapAlerts = 0）
檢查 `WebName` 是否一致：
- 回傳結果中會有 `skippedReasons` 列出具體原因

## ✨ 下一步

1. ⏳ 團隊成員實作 `HtmlParserService`
2. ⏳ 使用實際 ZAP HTML 報告測試
3. ⏳ 根據需求調整解析邏輯
4. ✅ 系統自動處理匯入與關聯

---

**建立時間**: 2025-10-12  
**狀態**: 架構完成，等待 HTML 解析實作

