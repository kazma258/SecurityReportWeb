# 認證與開發測試端點 API 規格

- Base URL：`/api`
- Content-Type：`application/json`（除非特別註明）
- 認證：登入後取得的 JWT，透過 `Authorization: Bearer <token>` 傳遞。

## 1. 使用者登入
- Method / Path：`POST /api/auth/login`
- Request Body：
  ```json
  {
    "username": "string",
    "password": "string"
  }
  ```
- Response 200：
  ```json
  {
    "success": true,
    "token": "jwt-token-string",
    "expiresAt": "2025-01-01T00:00:00Z",
    "user": {
      "userId": 1,
      "username": "admin",
      "fullName": "管理員",
      "email": "admin@example.com",
      "roles": ["Admin", "Manager"]
    }
  }
  ```
- Response 400（參數驗證失敗）：
  ```json
  {
    "success": false,
    "errorMessage": "請求參數驗證失敗"
  }
  ```
- Response 401（帳號或密碼錯誤）：
  ```json
  {
    "success": false,
    "errorMessage": "帳號或密碼錯誤"
  }
  ```

## 2. 驗證 Token 並取得使用者資訊
- Method / Path：`POST /api/auth/validate`
- Headers：`Authorization: Bearer <token>`
- Request Body：無
- Response 200：
  ```json
  {
    "userId": 1,
    "username": "admin",
    "fullName": "管理員",
    "email": "admin@example.com",
    "roles": ["Admin", "Manager"]
  }
  ```
- Response 401：
  ```json
  { "message": "Token 無效或已過期" }
  ```

## 3. 建立測試使用者（僅開發環境）
- Method / Path：`POST /api/user/create-test-user`
- Query 參數：
  - `username` (必填)
  - `password` (必填)
  - `fullName` (選填)
  - `email` (選填)
- Response 200：
  ```json
  {
    "message": "使用者建立成功",
    "userId": 2,
    "username": "tester"
  }
  ```
- Response 400：`{ "message": "使用者已存在" }` 或 `{ "message": "帳號和密碼為必填項目" }`
- Response 404：無
- 說明：僅在 `ASPNETCORE_ENVIRONMENT=Development` 時可用，生產環境請勿呼叫。

## 4. 指派角色（僅開發環境）
- Method / Path：`POST /api/user/assign-role`
- Query 參數：
  - `username` (必填)
  - `roleName` (必填，若角色不存在會自動建立)
- Response 200：
  ```json
  {
    "message": "角色指派成功",
    "username": "tester",
    "roleName": "Admin"
  }
  ```
- Response 400：`{ "message": "此端點僅在開發環境可用" }` 或 `{ "message": "使用者已擁有此角色" }`
- Response 404：`{ "message": "使用者不存在" }`
- 說明：僅在開發環境使用，供前端測試登入流程。

## 串接建議
- 登入後將 `token` 緩存於前端（例如 localStorage），後續 API 以 `Authorization` header 帶入。
- Token 過期時（Validate 回 401），提示重新登入。
- 角色資訊在 `roles` 陣列，可用於前端路由/功能顯示控管。

