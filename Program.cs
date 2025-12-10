using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecurityReportWeb.Database.Models;
using SecurityReportWeb.Import.Services;
using SecurityReportWeb.Services;
using Microsoft.Extensions.Logging;
using System;

var builder = WebApplication.CreateBuilder(args);

// 獲取 Logger 用於記錄啟動資訊
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Program");

// 加入服務到容器
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//讀取連線字串並替換 ${SA_PASSWORD}
var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var saPassword = builder.Configuration["SA_PASSWORD"] ?? Environment.GetEnvironmentVariable("SA_PASSWORD");
if (!string.IsNullOrWhiteSpace(saPassword))
{
    conn = conn.Replace("${SA_PASSWORD}", saPassword);
}

// 🔍 輸出連接字串資訊（用於除錯）
logger.LogInformation("=== 資料庫連線配置除錯資訊 ===");
logger.LogInformation("環境: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("原始連接字串（從配置讀取）: {ConnectionString}", 
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "未設定");
logger.LogInformation("SA_PASSWORD 環境變數: {Status}", 
    string.IsNullOrEmpty(saPassword) ? "❌ 未設定" : "✅ 已設定");
if (!string.IsNullOrEmpty(conn))
{
    // 隱藏密碼部分
    var maskedConn = conn;
    if (!string.IsNullOrEmpty(saPassword))
    {
        maskedConn = conn.Replace(saPassword, "***");
    }
    logger.LogInformation("實際使用的連接字串: {MaskedConnectionString}", maskedConn);
    
    // 提取伺服器資訊
    var serverMatch = System.Text.RegularExpressions.Regex.Match(conn, @"Server=([^;]+)");
    if (serverMatch.Success)
    {
        logger.LogInformation("資料庫伺服器: {Server}", serverMatch.Groups[1].Value);
    }
}
else
{
    logger.LogWarning("❌ 警告：連接字串為空！");
}
logger.LogInformation("====================================");

builder.Services.AddDbContext<ReportDbContext>(options =>
 options.UseSqlServer(conn));

// 匯入服務
builder.Services.AddScoped<IImportService, ImportService>();

// HTML 解析服務
builder.Services.AddScoped<IHtmlParserService, HtmlParserService>();

// XLSX 解析服務
builder.Services.AddScoped<IXlsxParserService, XlsxParserService>();

// 認證服務
builder.Services.AddScoped<IAuthService, AuthService>();

// CORS 設定
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3333")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials();
    });
});

// ✅ 驗證必要的環境變數
ValidateRequiredEnvironmentVariables(logger);

var app = builder.Build();

// ✅ 自動建立資料庫（直接以最新結構建立，不執行 Migration）
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    var appLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // 等待資料庫連線就緒（容器啟動需要時間）
        await EnsureDatabaseReady(context, appLogger);

        //直接以目前的模型建立資料庫結構（跳過 Migration 歷史）
        var created = await context.Database.EnsureCreatedAsync();

        if (created)
        {
            appLogger.LogInformation("✅ 資料庫建立成功（全新建立）");
        }
        else
        {
            appLogger.LogInformation("ℹ️ 資料庫已存在，跳過建立");
        }
    }
    catch (Exception ex)
    {
        appLogger.LogError(ex, "❌ 資料庫建立失敗: {Message}", ex.Message);
        // 在 Docker 環境中，我們希望應用程式繼續運行，而非崩潰
        // throw; // 可取消註解讓應用程式在資料庫建立失敗時終止
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 啟用 CORS（必須在 UseAuthorization 之前）
app.UseCors("AllowLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();

// 驗證必要的環境變數
static void ValidateRequiredEnvironmentVariables(ILogger logger)
{
    logger.LogInformation("=== 驗證必要的環境變數 ===");
    
    // 驗證 JWT_SECRET
    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
    if (string.IsNullOrEmpty(jwtSecret))
    {
        logger.LogError("❌ JWT_SECRET 環境變數未設定");
        throw new InvalidOperationException(
            "JWT_SECRET 環境變數未設定。請在 .env 檔案或環境變數中設定 JWT_SECRET，且長度至少需要 16 個字元（128 位元）。");
    }
    
    if (jwtSecret.Contains("${"))
    {
        logger.LogError("❌ JWT_SECRET 包含佔位符，請設定實際的金鑰值");
        throw new InvalidOperationException(
            "JWT_SECRET 包含佔位符。請在 .env 檔案中設定實際的 JWT_SECRET 值，且長度至少需要 16 個字元（128 位元）。");
    }
    
    if (jwtSecret.Length < 16)
    {
        logger.LogError("❌ JWT_SECRET 長度不足：{Length} 個字元，至少需要 16 個字元", jwtSecret.Length);
        throw new InvalidOperationException(
            $"JWT_SECRET 長度不足：目前為 {jwtSecret.Length} 個字元，至少需要 16 個字元（128 位元）。請設定足夠長的 JWT_SECRET。");
    }
    
    logger.LogInformation("✅ JWT_SECRET 驗證通過（長度：{Length} 個字元）", jwtSecret.Length);
    
    // 驗證 SA_PASSWORD（可選，但建議設定）
    var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
    if (string.IsNullOrEmpty(saPassword))
    {
        logger.LogWarning("⚠️ SA_PASSWORD 環境變數未設定，將使用配置檔案中的值");
    }
    else
    {
        logger.LogInformation("✅ SA_PASSWORD 已設定");
    }
    
    logger.LogInformation("====================================");
}

// 確保資料庫連線就緒的輔助方法
static async Task EnsureDatabaseReady(ReportDbContext context, ILogger logger, int maxRetries = 30, int delaySeconds = 2)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            // 使用 CanConnectAsync 檢查連接
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                // 進一步驗證：嘗試執行一個簡單的查詢
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                logger.LogInformation("✅ 資料庫連線就緒");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⏳ 等待資料庫啟動... (嘗試 {Attempt}/{MaxRetries}): {Message}", 
                i + 1, maxRetries, ex.Message);

            if (i == maxRetries - 1)
            {
                logger.LogError(ex, "❌ 資料庫連接失敗，最後一次錯誤: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    logger.LogError("內部異常: {InnerMessage}", ex.InnerException.Message);
                }
                throw new TimeoutException($"資料庫在 {maxRetries * delaySeconds} 秒內未能就緒: {ex.Message}", ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}
