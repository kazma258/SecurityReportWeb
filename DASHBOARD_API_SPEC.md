# 儀表板 API 規格文件

本文檔定義儀表板功能所需的 API 端點，包含資料視覺化與修復狀況更新功能。

## 基礎資訊

- **Base URL**: `https://localhost:7225/api/Data` (開發環境)
- **Content-Type**: `application/json`
- **CORS**: 已啟用，允許來自 `http://localhost:3333` 的請求

---

## 一、儀表板統計 API

### 1. 儀表板總覽統計 ⭐新增

**端點**: `GET /api/Data/dashboard/overview`

**說明**: 取得儀表板頂部統計卡片所需的資料（尚未解決漏洞、已修復漏洞、整體修復率、高風險漏洞）

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| fromDate | string | 否 | 起始日期 (YYYY-MM-DD) |
| toDate | string | 否 | 結束日期 (YYYY-MM-DD) |
| unitName | string | 否 | 依單位名稱過濾 |

**回應範例**:

```json
{
  "unresolvedCount": 99,
  "resolvedCount": 87,
  "overallFixRate": 70.0,
  "highRiskCount": 13
}
```

**資料計算規則**:
- `unresolvedCount`: `Status != "Closed"` 的警報數量
- `resolvedCount`: `Status == "Closed"` 的警報數量
- `overallFixRate`: `(resolvedCount / totalCount) × 100`
- `highRiskCount`: `Level == "High"` 且 `Status != "Closed"` 的警報數量

---

### 2. 風險等級分布 ⭐新增

**端點**: `GET /api/Data/dashboard/risk-level-distribution`

**說明**: 取得風險等級分布資料，用於圓餅圖顯示

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| fromDate | string | 否 | 起始日期 (YYYY-MM-DD) |
| toDate | string | 否 | 結束日期 (YYYY-MM-DD) |
| unitName | string | 否 | 依單位名稱過濾 |

**回應範例**:

```json
{
  "high": 13,
  "medium": 45,
  "low": 128,
  "informational": 0
}
```

---

### 3. 歷次掃描結果比較 ⭐新增

**端點**: `GET /api/Data/dashboard/scan-comparison`

**說明**: 取得歷次掃描的新增與修復數量比較，用於長條圖顯示

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| fromDate | string | 是 | 起始日期 (YYYY-MM-DD) |
| toDate | string | 是 | 結束日期 (YYYY-MM-DD) |
| groupBy | string | 否 | 分組方式：`day` \| `week` \| `month` (預設: `day`) |
| unitName | string | 否 | 依單位名稱過濾 |

**回應範例**:

```json
[
  {
    "date": "2024-01-15",
    "newCount": 25,
    "resolvedCount": 18
  },
  {
    "date": "2024-01-16",
    "newCount": 30,
    "resolvedCount": 22
  }
]
```

**資料計算規則**:
- `newCount`: 該日期當天新增的警報數量（依 `ReportDay` 判斷）
- `resolvedCount`: 該日期當天狀態變更為 `Closed` 的警報數量（需查詢狀態歷史表）

---

### 4. 部門資安績效 ⭐新增

**端點**: `GET /api/Data/dashboard/department-performance`

**說明**: 取得各部門的資安績效統計，用於表格顯示

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| fromDate | string | 否 | 起始日期 (YYYY-MM-DD) |
| toDate | string | 否 | 結束日期 (YYYY-MM-DD) |
| sortBy | string | 否 | 排序欄位：`totalCount` \| `fixRate` (預設: `totalCount`) |
| sortOrder | string | 否 | 排序方向：`asc` \| `desc` (預設: `desc`) |

**回應範例**:

```json
[
  {
    "unitName": "電算中心",
    "totalCount": 50,
    "resolvedCount": 35,
    "fixRate": 70.0,
    "manager": "張三"
  },
  {
    "unitName": "教務處",
    "totalCount": 30,
    "resolvedCount": 20,
    "fixRate": 66.7,
    "manager": "李四"
  }
]
```

**資料計算規則**:
- `totalCount`: 該單位的總警報數量
- `resolvedCount`: 該單位狀態為 `Closed` 的警報數量
- `fixRate`: `(resolvedCount / totalCount) × 100`

---

## 二、修復狀況更新 API

### 5. 更新警報狀態 ⭐新增

**端點**: `PATCH /api/Data/zap-alerts/{alertId}/status`

**說明**: 更新指定警報的修復狀態（僅負責人可更新自己管理的網站警報）

**權限**: 
- 僅該警報對應網站的負責人（`UrlList.Manager`）可更新
- 主管僅可查看，不可更新

**路徑參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| alertId | int | 是 | 警報 ID |

**請求體**:

```json
{
  "status": "Closed" | "In Progress" | "False Positive",
  "remark": "修復備註（選填）"
}
```

**回應範例**:

```json
{
  "alertId": 123,
  "status": "Closed",
  "updatedAt": "2024-01-15T10:30:00Z",
  "updatedBy": "張三"
}
```

**狀態值定義**:
- `Open`: 未處理（預設值）
- `In Progress`: 處理中
- `Closed`: 已修復
- `False Positive`: 誤報

**注意事項**:
- 狀態變更時會自動記錄到 `AlertStatusHistory` 表
- 同時會記錄到 `AuditLog` 表（透過現有的審計機制）

---

### 6. 取得修復紀錄（單一警報） ⭐新增

**端點**: `GET /api/Data/zap-alerts/{alertId}/status-history`

**說明**: 取得指定警報的狀態變更歷史記錄

**權限**: 
- 負責人可查看自己管理的警報歷史
- 主管可查看所有警報歷史

**路徑參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| alertId | int | 是 | 警報 ID |

**回應範例**:

```json
[
  {
    "historyId": 1,
    "alertId": 123,
    "oldStatus": "Open",
    "newStatus": "In Progress",
    "remark": "開始處理",
    "updatedAt": "2024-01-14T09:00:00Z",
    "updatedBy": "張三",
    "updatedByRole": "Manager"
  },
  {
    "historyId": 2,
    "alertId": 123,
    "oldStatus": "In Progress",
    "newStatus": "Closed",
    "remark": "已修復SQL注入漏洞",
    "updatedAt": "2024-01-15T10:30:00Z",
    "updatedBy": "張三",
    "updatedByRole": "Manager"
  }
]
```

---

### 7. 取得部門修復紀錄（主管用） ⭐新增

**端點**: `GET /api/Data/dashboard/fix-history`

**說明**: 取得部門的修復紀錄列表（僅主管可存取）

**權限**: 僅主管可存取

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| fromDate | string | 否 | 起始日期 (YYYY-MM-DD) |
| toDate | string | 否 | 結束日期 (YYYY-MM-DD) |
| unitName | string | 否 | 依單位名稱過濾 |
| manager | string | 否 | 依負責人過濾 |
| status | string | 否 | 依狀態過濾 |
| pageNumber | int | 否 | 頁碼（預設: 1） |
| pageSize | int | 否 | 每頁筆數（預設: 20，最大: 100） |

**回應範例**:

```json
{
  "items": [
    {
      "alertId": 123,
      "webName": "範例網站",
      "unitName": "電算中心",
      "riskName": "SQL Injection",
      "level": "High",
      "status": "Closed",
      "updatedAt": "2024-01-15T10:30:00Z",
      "updatedBy": "張三",
      "remark": "已修復"
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 三、現有 API 端點（無變更）

以下端點已存在，無需修改：

- `GET /api/Data/url-lists` - 取得 URL 清單列表
- `GET /api/Data/url-lists/{id}` - 取得單一 URL 清單詳情
- `GET /api/Data/zap-reports` - 取得 ZAP 報告列表
- `GET /api/Data/zap-alerts` - 取得 ZAP 警報列表
- `GET /api/Data/risk-descriptions` - 取得風險描述列表
- `GET /api/Data/statistics` - 取得統計資訊

---

## 四、資料庫變更需求

### 4.1 新增資料表：AlertStatusHistory

**目的**: 記錄警報狀態變更歷史，方便查詢修復紀錄與趨勢分析

**資料表結構**:

```sql
CREATE TABLE AlertStatusHistory (
    -- 主鍵：狀態變更歷史記錄的唯一識別碼，自動遞增
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    
    -- 外鍵：關聯到 ZAPAlertDetail 表的警報 ID
    -- 用途：識別此狀態變更屬於哪個警報
    AlertId INT NOT NULL,
    
    -- 舊狀態：狀態變更前的狀態值
    -- 用途：記錄狀態變更前的狀態，用於追蹤狀態變化歷程
    -- 可為 NULL：首次建立警報時沒有舊狀態
    -- 可能值：'Open', 'In Progress', 'Closed', 'False Positive'
    OldStatus NVARCHAR(255) NULL,
    
    -- 新狀態：狀態變更後的狀態值
    -- 用途：記錄狀態變更後的目標狀態
    -- 必填：每次狀態變更都必須有明確的新狀態
    -- 可能值：'Open', 'In Progress', 'Closed', 'False Positive'
    NewStatus NVARCHAR(255) NOT NULL,
    
    -- 備註：狀態變更時的說明或備註
    -- 用途：記錄負責人更新狀態時填寫的修復說明、處理方式等資訊
    -- 可為 NULL：允許不填寫備註
    Remark NVARCHAR(MAX) NULL,
    
    -- 更新時間：狀態變更的時間戳記（UTC 時間）
    -- 用途：記錄狀態變更的準確時間，用於時間序列分析和審計追蹤
    -- 預設值：自動設定為當前 UTC 時間
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- 更新者：執行狀態變更的使用者名稱
    -- 用途：記錄是誰執行了狀態變更，用於責任追蹤和權限審計
    -- 通常為負責人（Manager）的姓名
    UpdatedBy NVARCHAR(255) NOT NULL,
    
    -- 更新者角色：執行狀態變更的使用者角色
    -- 用途：記錄執行者的角色類型，用於權限分析和審計
    -- 可能值：'Manager'（負責人）、'Supervisor'（主管）、'Admin'（系統管理員）
    UpdatedByRole NVARCHAR(50) NOT NULL,
    
    -- 外鍵約束：確保 AlertId 必須存在於 ZAPAlertDetail 表中
    -- 級聯刪除：當警報被刪除時，相關的狀態歷史記錄也會自動刪除
    CONSTRAINT FK_AlertStatusHistory_ZAPAlertDetail 
        FOREIGN KEY (AlertId) 
        REFERENCES ZAPAlertDetail(AlertID) 
        ON DELETE CASCADE
);

-- 索引：優化依警報 ID 查詢狀態歷史的效能
-- 用途：快速查詢特定警報的所有狀態變更記錄
CREATE INDEX IX_AlertStatusHistory_AlertId 
    ON AlertStatusHistory(AlertId);
    
-- 索引：優化依時間範圍查詢的效能
-- 用途：快速查詢特定時間範圍內的狀態變更記錄，用於趨勢分析
CREATE INDEX IX_AlertStatusHistory_UpdatedAt 
    ON AlertStatusHistory(UpdatedAt);
    
-- 索引：優化依狀態查詢的效能
-- 用途：快速查詢特定狀態的變更記錄，用於統計分析
CREATE INDEX IX_AlertStatusHistory_NewStatus 
    ON AlertStatusHistory(NewStatus);
```

**C# 模型類別**:

```csharp
/// <summary>
/// 警報狀態變更歷史記錄
/// 用途：記錄每次警報狀態變更的詳細資訊，用於審計追蹤和趨勢分析
/// </summary>
public partial class AlertStatusHistory
{
    /// <summary>
    /// 狀態變更歷史記錄的唯一識別碼（主鍵）
    /// </summary>
    public int HistoryId { get; set; }
    
    /// <summary>
    /// 關聯的警報 ID（外鍵）
    /// 用途：識別此狀態變更屬於哪個警報
    /// </summary>
    public int AlertId { get; set; }
    
    /// <summary>
    /// 狀態變更前的狀態值
    /// 用途：記錄狀態變更前的狀態，用於追蹤狀態變化歷程
    /// 可為 null：首次建立警報時沒有舊狀態
    /// </summary>
    public string? OldStatus { get; set; }
    
    /// <summary>
    /// 狀態變更後的狀態值
    /// 用途：記錄狀態變更後的目標狀態
    /// 必填：每次狀態變更都必須有明確的新狀態
    /// </summary>
    public string NewStatus { get; set; } = null!;
    
    /// <summary>
    /// 狀態變更時的說明或備註
    /// 用途：記錄負責人更新狀態時填寫的修復說明、處理方式等資訊
    /// 可為 null：允許不填寫備註
    /// </summary>
    public string? Remark { get; set; }
    
    /// <summary>
    /// 狀態變更的時間戳記（UTC 時間）
    /// 用途：記錄狀態變更的準確時間，用於時間序列分析和審計追蹤
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 執行狀態變更的使用者名稱
    /// 用途：記錄是誰執行了狀態變更，用於責任追蹤和權限審計
    /// 通常為負責人（Manager）的姓名
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
    
    /// <summary>
    /// 執行狀態變更的使用者角色
    /// 用途：記錄執行者的角色類型，用於權限分析和審計
    /// 可能值：'Manager'（負責人）、'Supervisor'（主管）、'Admin'（系統管理員）
    /// </summary>
    public string UpdatedByRole { get; set; } = null!;
    
    /// <summary>
    /// 導航屬性：關聯的警報詳情
    /// 用途：EF Core 關聯查詢，可透過此屬性存取完整的警報資訊
    /// </summary>
    public virtual ZapalertDetail Alert { get; set; } = null!;
}
```

### 4.2 現有資料表評估

**ZAPAlertDetail 表**:
- ✅ 已有 `Status` 欄位，預設值為 "Open"
- ✅ 已有必要的索引
- ⚠️ 建議新增索引：`(Status, Level)` 複合索引，用於儀表板查詢優化

**UrlList 表**:
- ✅ 已有 `Manager` 欄位，可用於權限檢查
- ✅ 已有 `UnitName` 欄位，可用於部門統計

**AuditLog 表**:
- ✅ 已存在，可用於記錄所有變更
- ⚠️ `AlertStatusHistory` 表專門用於狀態變更，查詢效能較佳

### 4.3 建議新增的索引

```sql
-- 優化儀表板查詢效能
CREATE INDEX IX_ZAPAlertDetail_Status_Level 
    ON ZAPAlertDetail(Status, Level);
    
CREATE INDEX IX_ZAPAlertDetail_ReportDay_Status 
    ON ZAPAlertDetail(ReportDay, Status);
```

---

## 五、權限與角色定義

### 5.1 角色定義

| 角色 | 說明 | 權限 |
|------|------|------|
| Manager | 負責人 | 可更新自己管理的網站警報狀態，可查看自己的修復紀錄 |
| Supervisor | 主管 | 僅可查看所有修復紀錄，不可更新狀態 |
| Admin | 系統管理員 | 完整權限 |

### 5.2 權限檢查邏輯

**更新警報狀態時**:
1. 取得警報對應的 `UrlList.Manager`
2. 檢查當前使用者是否為該 Manager
3. 檢查使用者角色是否為 Manager 或 Admin
4. 若不符合，返回 403 Forbidden

**查看修復紀錄時**:
1. 若為 Supervisor 或 Admin，可查看所有紀錄
2. 若為 Manager，僅可查看自己管理的警報紀錄

---

## 六、錯誤處理

所有 API 端點在發生錯誤時會返回以下格式：

```json
{
  "error": "錯誤訊息",
  "message": "詳細錯誤訊息"
}
```

**HTTP 狀態碼**:

- `200 OK`: 請求成功
- `400 Bad Request`: 請求參數錯誤
- `401 Unauthorized`: 未授權（需要登入）
- `403 Forbidden`: 無權限（角色不符）
- `404 Not Found`: 資源不存在
- `500 Internal Server Error`: 伺服器內部錯誤
- `503 Service Unavailable`: 服務不可用（通常是資料庫連線問題）

---

## 七、實作優先順序

### 高優先級（MVP）
1. ✅ 儀表板總覽統計 API
2. ✅ 風險等級分布 API
3. ✅ 更新警報狀態 API
4. ✅ 權限檢查機制
5. ✅ 新增 `AlertStatusHistory` 資料表

### 中優先級
6. ✅ 歷次掃描結果比較 API
7. ✅ 部門資安績效 API
8. ✅ 修復紀錄查詢 API

### 低優先級（進階功能）
9. ⚠️ 資料快取優化
10. ⚠️ 通知機制（Email/站內通知）

---

## 八、注意事項

1. **日期格式**: 
   - `DateOnly` 類型使用 `YYYY-MM-DD` 格式（例如: `2024-01-15`）
   - `DateTime` 類型使用 ISO 8601 格式（例如: `2024-01-15T10:30:00Z`）

2. **狀態值**: 
   - 狀態值必須為預定義的值：`Open`, `In Progress`, `Closed`, `False Positive`
   - 更新時會驗證狀態值的有效性

3. **權限檢查**: 
   - 所有更新操作都需要進行權限檢查
   - 建議實作統一的權限檢查 Middleware

4. **效能考量**: 
   - 儀表板 API 可能需要處理大量資料，建議加入適當的索引
   - 可考慮實作快取機制（Redis）來提升查詢效能

5. **資料一致性**: 
   - 更新狀態時需同時更新 `ZAPAlertDetail.Status` 和 `AlertStatusHistory` 表
   - 建議使用資料庫交易確保一致性

---

## 更新日誌

- **2024-01-XX**: 初始版本，定義儀表板 API 規格與修復狀況更新機制

