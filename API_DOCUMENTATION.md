# 資料查詢 API 文檔

本文檔說明後端提供的資料查詢 API 端點，供前端開發使用。

## 基礎資訊

- **Base URL**: `https://localhost:7225/api/Data` (開發環境)
- **Content-Type**: `application/json`
- **CORS**: 已啟用，允許來自 `http://localhost:3333` 的請求

## 通用功能

### 分頁參數

所有列表端點都支援分頁：

- `pageNumber` (int, 選填): 頁碼，從 1 開始，預設為 1
- `pageSize` (int, 選填): 每頁筆數，預設為 20，最大為 100

### 排序參數

- `sortBy` (string, 選填): 排序欄位名稱
- `sortOrder` (string, 選填): 排序方向，`asc` 或 `desc`，預設為 `asc`

### 分頁回應格式

```typescript
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

## API 端點

### 1. URL 清單

#### 1.1 取得 URL 清單列表

**端點**: `GET /api/Data/url-lists`

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| pageNumber | int | 否 | 頁碼（預設: 1） |
| pageSize | int | 否 | 每頁筆數（預設: 20，最大: 100） |
| search | string | 否 | 搜尋關鍵字（搜尋 WebName、Url、UnitName、Manager） |
| unitName | string | 否 | 依單位名稱過濾 |
| manager | string | 否 | 依管理者過濾 |
| sortBy | string | 否 | 排序欄位：`webName`, `url`, `unitName`, `uploadDate`（預設: `webName`） |
| sortOrder | string | 否 | 排序方向：`asc`, `desc`（預設: `asc`） |
| includeStats | boolean | 否 | 是否包含統計資訊（報告數、警報數），預設: `false` |

**回應範例**:

```json
{
  "items": [
    {
      "urlId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "url": "https://example.ncut.edu.tw",
      "ip": "192.168.1.1",
      "webName": "範例網站",
      "unitName": "電算中心",
      "remark": "備註說明",
      "manager": "張三",
      "managerMail": "zhang@ncut.edu.tw",
      "outsourcedVendor": "委外廠商",
      "riskReportLink": "https://report.example.com",
      "uploadDate": "2024-01-15",
      "reportCount": 5,
      "alertCount": 12
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

**使用範例**:

```typescript
// 取得第一頁 URL 清單
const response = await fetch('https://localhost:7225/api/Data/url-lists?pageNumber=1&pageSize=20');
const data = await response.json();

// 搜尋包含 "ncut" 的 URL
const searchResponse = await fetch('https://localhost:7225/api/Data/url-lists?search=ncut');

// 取得特定單位的 URL，依上傳日期降序排序
const unitResponse = await fetch('https://localhost:7225/api/Data/url-lists?unitName=電算中心&sortBy=uploadDate&sortOrder=desc');

// 包含統計資訊
const statsResponse = await fetch('https://localhost:7225/api/Data/url-lists?includeStats=true');
```

#### 1.2 取得單一 URL 清單詳情

**端點**: `GET /api/Data/url-lists/{id}`

**路徑參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| id | Guid | 是 | URL 清單的 ID |

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| includeStats | boolean | 否 | 是否包含統計資訊，預設: `true` |

**回應範例**:

```json
{
  "urlId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "url": "https://example.ncut.edu.tw",
  "ip": "192.168.1.1",
  "webName": "範例網站",
  "unitName": "電算中心",
  "remark": "備註說明",
  "manager": "張三",
  "managerMail": "zhang@ncut.edu.tw",
  "outsourcedVendor": "委外廠商",
  "riskReportLink": "https://report.example.com",
  "uploadDate": "2024-01-15",
  "reportCount": 5,
  "alertCount": 12
}
```

**使用範例**:

```typescript
const urlId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const response = await fetch(`https://localhost:7225/api/Data/url-lists/${urlId}`);
const urlList = await response.json();
```

---

### 2. ZAP 報告

#### 2.1 取得 ZAP 報告列表

**端點**: `GET /api/Data/zap-reports`

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| pageNumber | int | 否 | 頁碼（預設: 1） |
| pageSize | int | 否 | 每頁筆數（預設: 20，最大: 100） |
| siteUrlId | Guid | 否 | 依網站 URL ID 過濾 |
| siteWebName | string | 否 | 依網站名稱過濾 |
| fromDate | DateOnly | 否 | 報告日期起始（格式: YYYY-MM-DD） |
| toDate | DateOnly | 否 | 報告日期結束（格式: YYYY-MM-DD） |
| isDeleted | boolean | 否 | 是否包含已刪除的報告 |
| sortBy | string | 否 | 排序欄位：`generatedDate`, `generatedDay`, `zapversion`（預設: `generatedDate`） |
| sortOrder | string | 否 | 排序方向：`asc`, `desc`（預設: `desc`） |
| includeStats | boolean | 否 | 是否包含統計資訊（警報數），預設: `false` |

**回應範例**:

```json
{
  "items": [
    {
      "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "siteUrlId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "siteWebName": "範例網站",
      "siteUrl": "https://example.ncut.edu.tw",
      "generatedDate": "2024-01-15T10:30:00Z",
      "generatedDay": "2024-01-15",
      "zapversion": "2.14.0",
      "zapsupporter": "ZAP Team",
      "isDeleted": false,
      "alertCount": 12
    }
  ],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**使用範例**:

```typescript
// 取得所有報告
const response = await fetch('https://localhost:7225/api/Data/zap-reports');

// 取得特定網站的報告
const siteResponse = await fetch('https://localhost:7225/api/Data/zap-reports?siteWebName=範例網站');

// 取得指定日期範圍的報告
const dateRangeResponse = await fetch('https://localhost:7225/api/Data/zap-reports?fromDate=2024-01-01&toDate=2024-01-31');

// 只取得未刪除的報告
const activeResponse = await fetch('https://localhost:7225/api/Data/zap-reports?isDeleted=false');
```

---

### 3. ZAP 警報詳情

#### 3.1 取得 ZAP 警報列表

**端點**: `GET /api/Data/zap-alerts`

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| pageNumber | int | 否 | 頁碼（預設: 1） |
| pageSize | int | 否 | 每頁筆數（預設: 20，最大: 100） |
| rootUrlId | Guid | 否 | 依根 URL ID 過濾 |
| rootWebName | string | 否 | 依根網站名稱過濾 |
| riskName | string | 否 | 依風險名稱過濾 |
| level | string | 否 | 依風險等級過濾（如: `High`, `Medium`, `Low`, `Informational`） |
| status | string | 否 | 依狀態過濾（如: `Open`, `Closed`, `False Positive`） |
| fromDate | DateOnly | 否 | 報告日期起始（格式: YYYY-MM-DD） |
| toDate | DateOnly | 否 | 報告日期結束（格式: YYYY-MM-DD） |
| sortBy | string | 否 | 排序欄位：`reportDate`, `reportDay`, `riskName`, `level`, `status`（預設: `reportDate`） |
| sortOrder | string | 否 | 排序方向：`asc`, `desc`（預設: `desc`） |

**回應範例**:

```json
{
  "items": [
    {
      "alertId": 1,
      "rootUrlId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "rootWebName": "範例網站",
      "rootUrl": "https://example.ncut.edu.tw",
      "url": "https://example.ncut.edu.tw/login",
      "reportDate": "2024-01-15T10:30:00Z",
      "reportDay": "2024-01-15",
      "riskName": "SQL Injection",
      "level": "High",
      "method": "POST",
      "parameter": "username",
      "attack": "admin' OR '1'='1",
      "evidence": "Database error message",
      "status": "Open",
      "otherInfo": "其他資訊"
    }
  ],
  "totalCount": 200,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**使用範例**:

```typescript
// 取得所有高風險警報
const highRiskResponse = await fetch('https://localhost:7225/api/Data/zap-alerts?level=High&pageSize=50');

// 取得特定網站的未處理警報
const openAlertsResponse = await fetch('https://localhost:7225/api/Data/zap-alerts?rootWebName=範例網站&status=Open');

// 取得指定日期範圍的警報
const dateAlertsResponse = await fetch('https://localhost:7225/api/Data/zap-alerts?fromDate=2024-01-01&toDate=2024-01-31');

// 取得特定風險類型的警報
const riskResponse = await fetch('https://localhost:7225/api/Data/zap-alerts?riskName=SQL Injection');
```

---

### 4. 風險描述

#### 4.1 取得風險描述列表

**端點**: `GET /api/Data/risk-descriptions`

**查詢參數**:

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| search | string | 否 | 搜尋關鍵字（搜尋 Name、Description） |
| name | string | 否 | 依風險名稱精確過濾 |

**回應範例**:

```json
[
  {
    "riskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "SQL Injection",
    "description": "SQL injection is a code injection technique...",
    "solution": "Use parameterized queries...",
    "reference": "https://owasp.org/www-community/attacks/SQL_Injection",
    "cweid": 89,
    "wascid": 19,
    "pluginId": 40018,
    "signature": "sql_injection_signature"
  }
]
```

**使用範例**:

```typescript
// 取得所有風險描述
const response = await fetch('https://localhost:7225/api/Data/risk-descriptions');
const risks = await response.json();

// 搜尋風險描述
const searchResponse = await fetch('https://localhost:7225/api/Data/risk-descriptions?search=SQL');

// 取得特定風險名稱
const nameResponse = await fetch('https://localhost:7225/api/Data/risk-descriptions?name=SQL Injection');
```

---

### 5. 統計資訊

#### 5.1 取得統計資訊

**端點**: `GET /api/Data/statistics`

**說明**: 取得整體統計資訊，適合用於儀表板顯示。

**回應範例**:

```json
{
  "totalUrlLists": 100,
  "totalZapReports": 500,
  "totalZapAlerts": 2000,
  "totalRiskDescriptions": 50,
  "alertsByLevel": [
    { "level": "High", "count": 100 },
    { "level": "Medium", "count": 500 },
    { "level": "Low", "count": 800 },
    { "level": "Informational", "count": 600 }
  ],
  "alertsByStatus": [
    { "status": "Open", "count": 1500 },
    { "status": "Closed", "count": 400 },
    { "status": "False Positive", "count": 100 }
  ],
  "topRiskNames": [
    { "riskName": "SQL Injection", "count": 200 },
    { "riskName": "Cross-Site Scripting (XSS)", "count": 150 },
    { "riskName": "Missing Security Headers", "count": 120 }
  ]
}
```

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/statistics');
const stats = await response.json();

console.log(`總共有 ${stats.totalUrlLists} 個網站`);
console.log(`高風險警報: ${stats.alertsByLevel.find(l => l.level === 'High')?.count}`);
```

---

## 儀表板 API

### 6. 儀表板總覽統計 ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/dashboard/overview?fromDate=2024-01-01&toDate=2024-01-31');
const overview = await response.json();
```

---

### 7. 風險等級分布 ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/dashboard/risk-level-distribution');
const distribution = await response.json();
```

---

### 8. 歷次掃描結果比較 ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/dashboard/scan-comparison?fromDate=2024-01-01&toDate=2024-01-31&groupBy=day');
const comparison = await response.json();
```

---

### 9. 部門資安績效 ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/dashboard/department-performance?sortBy=fixRate&sortOrder=desc');
const performance = await response.json();
```

---

## 修復狀況更新 API

### 10. 更新警報狀態 ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/zap-alerts/123/status', {
  method: 'PATCH',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    status: 'Closed',
    remark: '已修復SQL注入漏洞'
  })
});
const result = await response.json();
```

---

### 11. 取得修復紀錄（單一警報） ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/zap-alerts/123/status-history');
const history = await response.json();
```

---

### 12. 取得部門修復紀錄（主管用） ⭐新增

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

**使用範例**:

```typescript
const response = await fetch('https://localhost:7225/api/Data/dashboard/fix-history?unitName=電算中心&pageNumber=1&pageSize=20');
const fixHistory = await response.json();
```

---

## 錯誤處理

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

**錯誤處理範例**:

```typescript
try {
  const response = await fetch('https://localhost:7225/api/Data/url-lists');
  
  if (!response.ok) {
    if (response.status === 404) {
      console.error('資源不存在');
    } else if (response.status === 500) {
      const error = await response.json();
      console.error('伺服器錯誤:', error.message);
    }
    return;
  }
  
  const data = await response.json();
  // 處理資料
} catch (error) {
  console.error('網路錯誤:', error);
}
```

---

## TypeScript 類型定義

建議在前端專案中定義以下類型：

```typescript
// 分頁結果
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// URL 清單
interface UrlListDto {
  urlId: string;
  url: string;
  ip?: string;
  webName: string;
  unitName: string;
  remark?: string;
  manager?: string;
  managerMail?: string;
  outsourcedVendor?: string;
  riskReportLink: string;
  uploadDate: string; // DateOnly 格式: YYYY-MM-DD
  reportCount?: number;
  alertCount?: number;
}

// ZAP 報告
interface ZapReportDto {
  reportId: string;
  siteUrlId: string;
  siteWebName?: string;
  siteUrl?: string;
  generatedDate: string; // ISO 8601 格式
  generatedDay: string; // DateOnly 格式: YYYY-MM-DD
  zapversion: string;
  zapsupporter: string;
  isDeleted: boolean;
  alertCount?: number;
}

// ZAP 警報詳情
interface ZapAlertDetailDto {
  alertId: number;
  rootUrlId: string;
  rootWebName?: string;
  rootUrl?: string;
  url: string;
  reportDate: string; // ISO 8601 格式
  reportDay: string; // DateOnly 格式: YYYY-MM-DD
  riskName: string;
  level: string;
  method: string;
  parameter?: string;
  attack?: string;
  evidence?: string;
  status: string;
  otherInfo?: string;
}

// 風險描述
interface RiskDescriptionDto {
  riskId: string;
  name: string;
  description?: string;
  solution?: string;
  reference?: string;
  cweid?: number;
  wascid?: number;
  pluginId?: number;
  signature: string;
}

// 統計資訊
interface StatisticsDto {
  totalUrlLists: number;
  totalZapReports: number;
  totalZapAlerts: number;
  totalRiskDescriptions: number;
  alertsByLevel: Array<{ level: string; count: number }>;
  alertsByStatus: Array<{ status: string; count: number }>;
  topRiskNames: Array<{ riskName: string; count: number }>;
}

// 儀表板總覽統計
interface DashboardOverviewDto {
  unresolvedCount: number;
  resolvedCount: number;
  overallFixRate: number;
  highRiskCount: number;
}

// 風險等級分布
interface RiskLevelDistributionDto {
  high: number;
  medium: number;
  low: number;
  informational: number;
}

// 掃描結果比較
interface ScanComparisonDto {
  date: string; // YYYY-MM-DD
  newCount: number;
  resolvedCount: number;
}

// 部門資安績效
interface DepartmentPerformanceDto {
  unitName: string;
  totalCount: number;
  resolvedCount: number;
  fixRate: number;
  manager: string;
}

// 警報狀態更新請求
interface UpdateAlertStatusRequest {
  status: 'Open' | 'In Progress' | 'Closed' | 'False Positive';
  remark?: string;
}

// 警報狀態更新回應
interface UpdateAlertStatusResponse {
  alertId: number;
  status: string;
  updatedAt: string; // ISO 8601 格式
  updatedBy: string;
}

// 警報狀態歷史記錄
interface AlertStatusHistoryDto {
  historyId: number;
  alertId: number;
  oldStatus: string | null;
  newStatus: string;
  remark: string | null;
  updatedAt: string; // ISO 8601 格式
  updatedBy: string;
  updatedByRole: string;
}

// 修復紀錄（主管用）
interface FixHistoryItemDto {
  alertId: number;
  webName: string;
  unitName: string;
  riskName: string;
  level: string;
  status: string;
  updatedAt: string; // ISO 8601 格式
  updatedBy: string;
  remark: string | null;
}
```

---

## 前端實作建議

### 1. API 客戶端封裝

建議建立一個 API 客戶端類別來統一處理請求：

```typescript
class DataApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = 'https://localhost:7225/api/Data') {
    this.baseUrl = baseUrl;
  }

  async getUrlLists(params?: {
    pageNumber?: number;
    pageSize?: number;
    search?: string;
    unitName?: string;
    manager?: string;
    sortBy?: string;
    sortOrder?: string;
    includeStats?: boolean;
  }): Promise<PagedResult<UrlListDto>> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/url-lists?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getUrlList(id: string, includeStats: boolean = true): Promise<UrlListDto> {
    const response = await fetch(`${this.baseUrl}/url-lists/${id}?includeStats=${includeStats}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getZapReports(params?: {
    pageNumber?: number;
    pageSize?: number;
    siteUrlId?: string;
    siteWebName?: string;
    fromDate?: string;
    toDate?: string;
    isDeleted?: boolean;
    sortBy?: string;
    sortOrder?: string;
    includeStats?: boolean;
  }): Promise<PagedResult<ZapReportDto>> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/zap-reports?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getZapAlerts(params?: {
    pageNumber?: number;
    pageSize?: number;
    rootUrlId?: string;
    rootWebName?: string;
    riskName?: string;
    level?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    sortBy?: string;
    sortOrder?: string;
  }): Promise<PagedResult<ZapAlertDetailDto>> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/zap-alerts?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getRiskDescriptions(params?: {
    search?: string;
    name?: string;
  }): Promise<RiskDescriptionDto[]> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/risk-descriptions?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getStatistics(): Promise<StatisticsDto> {
    const response = await fetch(`${this.baseUrl}/statistics`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  // 儀表板 API
  async getDashboardOverview(params?: {
    fromDate?: string;
    toDate?: string;
    unitName?: string;
  }): Promise<DashboardOverviewDto> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/dashboard/overview?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getRiskLevelDistribution(params?: {
    fromDate?: string;
    toDate?: string;
    unitName?: string;
  }): Promise<RiskLevelDistributionDto> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/dashboard/risk-level-distribution?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getScanComparison(params: {
    fromDate: string;
    toDate: string;
    groupBy?: string;
    unitName?: string;
  }): Promise<ScanComparisonDto[]> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });
    
    const response = await fetch(`${this.baseUrl}/dashboard/scan-comparison?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getDepartmentPerformance(params?: {
    fromDate?: string;
    toDate?: string;
    sortBy?: string;
    sortOrder?: string;
  }): Promise<DepartmentPerformanceDto[]> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/dashboard/department-performance?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  // 修復狀況更新 API
  async updateAlertStatus(
    alertId: number,
    request: UpdateAlertStatusRequest
  ): Promise<UpdateAlertStatusResponse> {
    const response = await fetch(`${this.baseUrl}/zap-alerts/${alertId}/status`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getAlertStatusHistory(alertId: number): Promise<AlertStatusHistoryDto[]> {
    const response = await fetch(`${this.baseUrl}/zap-alerts/${alertId}/status-history`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }

  async getFixHistory(params?: {
    fromDate?: string;
    toDate?: string;
    unitName?: string;
    manager?: string;
    status?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PagedResult<FixHistoryItemDto>> {
    const queryParams = new URLSearchParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
    }
    
    const response = await fetch(`${this.baseUrl}/dashboard/fix-history?${queryParams}`);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return response.json();
  }
}

// 使用範例
const apiClient = new DataApiClient();

// 取得 URL 清單
const urlLists = await apiClient.getUrlLists({ pageNumber: 1, pageSize: 20 });

// 取得統計資訊
const stats = await apiClient.getStatistics();
```

### 2. React/Vue 組合式函數範例

```typescript
// useUrlLists.ts (React)
import { useState, useEffect } from 'react';

export function useUrlLists(params?: {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  // ... 其他參數
}) {
  const [data, setData] = useState<PagedResult<UrlListDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const result = await apiClient.getUrlLists(params);
        setData(result);
        setError(null);
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [JSON.stringify(params)]);

  return { data, loading, error };
}
```

---

## 權限與角色定義

### 角色定義

| 角色 | 說明 | 權限 |
|------|------|------|
| Manager | 負責人 | 可更新自己管理的網站警報狀態，可查看自己的修復紀錄 |
| Supervisor | 主管 | 僅可查看所有修復紀錄，不可更新狀態 |
| Admin | 系統管理員 | 完整權限 |

### 權限檢查邏輯

**更新警報狀態時**:
1. 取得警報對應的 `UrlList.Manager`
2. 檢查當前使用者是否為該 Manager
3. 檢查使用者角色是否為 Manager 或 Admin
4. 若不符合，返回 403 Forbidden

**查看修復紀錄時**:
1. 若為 Supervisor 或 Admin，可查看所有紀錄
2. 若為 Manager，僅可查看自己管理的警報紀錄

---

## 注意事項

1. **日期格式**: 
   - `DateOnly` 類型使用 `YYYY-MM-DD` 格式（例如: `2024-01-15`）
   - `DateTime` 類型使用 ISO 8601 格式（例如: `2024-01-15T10:30:00Z`）

2. **分頁限制**: 
   - 每頁最大筆數為 100，超過會自動調整為 100

3. **效能考量**: 
   - 使用 `includeStats=true` 時會執行額外的查詢，可能影響效能
   - 建議在列表頁面使用 `includeStats=false`，只在詳情頁面使用 `includeStats=true`
   - 儀表板 API 可能需要處理大量資料，建議加入適當的索引
   - 可考慮實作快取機制（Redis）來提升查詢效能

4. **CORS**: 
   - 目前僅允許來自 `http://localhost:3333` 的請求
   - 如需支援其他來源，請聯繫後端開發人員

5. **錯誤處理**: 
   - 建議在所有 API 呼叫中加入錯誤處理邏輯
   - 網路錯誤和 HTTP 錯誤應分別處理
   - 401 Unauthorized 表示需要登入，應導向登入頁面
   - 403 Forbidden 表示權限不足，應顯示適當的錯誤訊息

6. **狀態值**: 
   - 狀態值必須為預定義的值：`Open`, `In Progress`, `Closed`, `False Positive`
   - 更新時會驗證狀態值的有效性

7. **權限檢查**: 
   - 所有更新操作都需要進行權限檢查
   - 建議實作統一的權限檢查 Middleware

8. **資料一致性**: 
   - 更新狀態時需同時更新 `ZAPAlertDetail.Status` 和 `AlertStatusHistory` 表
   - 建議使用資料庫交易確保一致性

---

## 更新日誌

- **2025-10-29**: 初始版本，提供完整的資料查詢 API
- **2025-11-26**: 新增儀表板 API 與修復狀況更新功能

