using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecurityReportWeb.Database.Models;
using SecurityReportWeb.Import.Dtos;
using SecurityReportWeb.Import.Helpers;

namespace SecurityReportWeb.Import.Services;

public interface IImportService
{
    Task<ImportResultDto> ImportAsync(ImportRequestDto request);
}

public class ImportService : IImportService
{
    private readonly ReportDbContext _dbContext;

    public ImportService(ReportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ImportResultDto> ImportAsync(ImportRequestDto request)
    // 匯入功能主程式，依據傳入參數進行多表 Upsert、Alert 明細批量寫入
    // 步驟：
    // 1. 開啟資料庫交易 (transaction)
    // 2. 處理 UrlList：依 WebName 判斷新增或更新（upsert），並產生 UrlId
    // 3. 取得最新 UrlList、建構 Host->UrlId 對照
    // 4. 處理 RiskDescription：依 Name 判斷 upsert，計算 Signature
    // 5. 建構 Signature->RiskId 對照 (如後續需要)
    // 6. 處理 ZapReport：依 WebName+ReportDay upsert，標註刪除/同步報告
    // 7. 處理 ZapAlert 明細：
    //    a) 依 WebName 對應 UrlId，補齊 foreign key
    //    b) 分組：RootUrlId+ReportDay
    //    c) 若指定 ReplaceAlertsForSubmittedDays，先清除同組舊資料
    //    d) 將 alert 批量寫入明細資料表
    // 8. 儲存所有變更、提交交易、回傳結果
    {
        var result = new ImportResultDto();

        await using var trx = await _dbContext.Database.BeginTransactionAsync();

        // UrlList upsert by WebName (unique)
        // UrlId 由 Url 內容計算而得，確保相同 URL 總是得到相同 ID
        if (request.UrlLists?.Count > 0)
        {
            var existingByWebName = await _dbContext.UrlLists
                .AsTracking()
                .ToDictionaryAsync(x => x.WebName);

            foreach (var dto in request.UrlLists)
            {
                var computedUrlId = SignatureHelper.ComputeUrlId(dto.Url);

                if (existingByWebName.TryGetValue(dto.WebName, out var entity))
                {
                    // 更新時也要重新計算 UrlId（若 Url 改變）
                    entity.UrlId = computedUrlId;
                    entity.Url = dto.Url;
                    entity.Ip = dto.Ip;
                    entity.UnitName = dto.UnitName;
                    entity.Remark = dto.Remark;
                    entity.Manager = dto.Manager;
                    entity.ManagerMail = dto.ManagerMail;
                    entity.OutsourcedVendor = dto.OutsourcedVendor;
                    entity.RiskReportLink = dto.RiskReportLink;
                    entity.UploadDate = dto.UploadDate;
                    result.UrlListsUpdated++;
                }
                else
                {
                    var newEntity = new UrlList
                    {
                        UrlId = computedUrlId,
                        Url = dto.Url,
                        Ip = dto.Ip,
                        WebName = dto.WebName,
                        UnitName = dto.UnitName,
                        Remark = dto.Remark,
                        Manager = dto.Manager,
                        ManagerMail = dto.ManagerMail,
                        OutsourcedVendor = dto.OutsourcedVendor,
                        RiskReportLink = dto.RiskReportLink,
                        UploadDate = dto.UploadDate,
                    };
                    _dbContext.UrlLists.Add(newEntity);
                    existingByWebName[dto.WebName] = newEntity;
                    result.UrlListsInserted++;
                }
            }
        }

        // RiskDescription upsert by (Name, Signature)
        // Signature 由內容自動計算
        if (request.RiskDescriptions?.Count > 0)
        {
            // 預先計算所有 Signature
            var dtoWithSignatures = request.RiskDescriptions
                .Select(dto => new
                {
                    Dto = dto,
                    Signature = SignatureHelper.ComputeRiskSignature(
                        dto.Name,
                        dto.Description,
                        dto.Solution,
                        dto.Reference,
                        dto.Cweid,
                        dto.Wascid,
                        dto.PluginId)
                })
                .ToList();

            var signatures = dtoWithSignatures.Select(x => x.Signature).ToList();
            var names = dtoWithSignatures.Select(x => x.Dto.Name).ToList();

            var existed = await _dbContext.RiskDescriptions
                .Where(r => names.Contains(r.Name) && signatures.Contains(r.Signature))
                .ToListAsync();

            foreach (var item in dtoWithSignatures)
            {
                var dto = item.Dto;
                var signature = item.Signature;
                var riskId = SignatureHelper.ComputeRiskId(dto.Name, signature);
                var entity = existed.FirstOrDefault(x => x.Name == dto.Name && x.Signature == signature);

                if (entity != null)
                {
                    // 更新既有記錄（即使內容相同也更新，確保最新）
                    entity.RiskId = riskId;
                    entity.Description = dto.Description;
                    entity.Solution = dto.Solution;
                    entity.Reference = dto.Reference;
                    entity.Cweid = dto.Cweid;
                    entity.Wascid = dto.Wascid;
                    entity.PluginId = dto.PluginId;
                    result.RiskDescriptionsUpdated++;
                }
                else
                {
                    _dbContext.RiskDescriptions.Add(new RiskDescription
                    {
                        RiskId = riskId,
                        Name = dto.Name,
                        Description = dto.Description,
                        Solution = dto.Solution,
                        Reference = dto.Reference,
                        Cweid = dto.Cweid,
                        Wascid = dto.Wascid,
                        PluginId = dto.PluginId,
                        Signature = signature,
                    });
                    result.RiskDescriptionsInserted++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        // 重新載入 UrlList，建立以 Url host 為索引的對應表
        var urlListRecords = await _dbContext.UrlLists.AsNoTracking().ToListAsync();
        var urlByNormalizedHost = BuildUrlIdByHostLookup(urlListRecords);

        // ZapReport upsert by (SiteUrlId, GeneratedDay)
        // ReportId 由 (SiteUrlId, GeneratedDay) 計算而得
        if (request.ZapReports?.Count > 0)
        {
            foreach (var dto in request.ZapReports)
            {
                if (!TryResolveUrlId(dto.SiteWebName, urlByNormalizedHost, out var siteUrlId))
                {
                    // 無對應站點，略過
                    result.ZapReportsSkipped++;
                    result.SkippedReasons.Add($"ZapReport 略過：找不到 '{dto.SiteWebName}' 對應的 UrlList.Url。");
                    continue;
                }

                var reportId = SignatureHelper.ComputeReportId(siteUrlId, dto.GeneratedDay);
                var existed = await _dbContext.Zapreports
                    .FirstOrDefaultAsync(z => z.SiteUrlId == siteUrlId && z.GeneratedDay == dto.GeneratedDay);

                if (existed != null)
                {
                    existed.ReportId = reportId;
                    existed.GeneratedDate = dto.GeneratedDate;
                    existed.Zapversion = dto.Zapversion;
                    existed.Zapsupporter = dto.Zapsupporter;
                    existed.IsDeleted = dto.IsDeleted;
                    result.ZapReportsUpdated++;
                }
                else
                {
                    _dbContext.Zapreports.Add(new Zapreport
                    {
                        ReportId = reportId,
                        SiteUrlId = siteUrlId,
                        GeneratedDate = dto.GeneratedDate,
                        GeneratedDay = dto.GeneratedDay,
                        Zapversion = dto.Zapversion,
                        Zapsupporter = dto.Zapsupporter,
                        IsDeleted = dto.IsDeleted,
                    });
                    result.ZapReportsInserted++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        // ZapAlertDetail insert; 可選擇先清掉指定日的既有資料避免重複
        if (request.ZapAlerts?.Count > 0)
        {
            var validAlerts = new List<ZapAlertDetailImportDto>();
            var rootUrlIdLookup = new Dictionary<ZapAlertDetailImportDto, Guid>();

            foreach (var alert in request.ZapAlerts)
            {
                if (TryResolveUrlId(alert.RootWebName, urlByNormalizedHost, out var rootUrlId))
                {
                    validAlerts.Add(alert);
                    rootUrlIdLookup[alert] = rootUrlId;
                }
                else
                {
                    result.ZapAlertsSkipped++;
                    result.SkippedReasons.Add($"ZapAlert 略過：找不到 '{alert.RootWebName}' 對應的 UrlList.Url。");
                }
            }

            var groups = validAlerts
                .GroupBy(x => (RootUrlId: rootUrlIdLookup[x], x.ReportDay))
                .ToList();

            if (groups.Count > 0 && request.ReplaceAlertsForSubmittedDays)
            {
                foreach (var g in groups)
                {
                    await _dbContext.ZapalertDetails
                        .Where(a => a.RootUrlId == g.Key.RootUrlId && a.ReportDay == g.Key.ReportDay)
                        .ExecuteDeleteAsync();
                }
            }

            foreach (var g in groups)
            {
                foreach (var alert in g)
                {
                    _dbContext.ZapalertDetails.Add(new ZapalertDetail
                    {
                        RootUrlId = rootUrlIdLookup[alert],
                        Url = alert.Url,
                        ReportDate = alert.ReportDate,
                        ReportDay = alert.ReportDay,
                        RiskName = alert.RiskName,
                        Level = alert.Level,
                        Method = alert.Method,
                        Parameter = alert.Parameter,
                        Attack = alert.Attack,
                        Evidence = alert.Evidence,
                        Status = alert.Status ?? "Open",
                        OtherInfo = alert.OtherInfo,
                    });
                    result.ZapAlertsInserted++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        await trx.CommitAsync();
        return result;
    }

    private static Dictionary<string, Guid> BuildUrlIdByHostLookup(IEnumerable<UrlList> urlLists)
    // 建立 UrlId 對應的 Dictionary
    // 以 Url 的 host 為 key，UrlId 為 value
    // 例如：https://www.google.com 的 host 為 www.google.com
    // 所以 lookup["www.google.com"] = UrlId
    {
        var lookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in urlLists)
        {
            if (string.IsNullOrWhiteSpace(item.Url))
            {
                continue;
            }

            var host = NormalizeHost(item.Url);
            if (string.IsNullOrEmpty(host))
            {
                continue;
            }

            if (!lookup.ContainsKey(host))
            {
                lookup[host] = item.UrlId;
            }
        }

        return lookup;
    }

    private static bool TryResolveUrlId(string? candidate, IDictionary<string, Guid> byHost, out Guid urlId)
    // 嘗試解析 UrlId 對應的 Url
    // 以 Url 的 host 為 key，UrlId 為 value
    // 例如：byHost["www.google.com"] = UrlId
    // 這樣可以快速查詢 UrlId 對應的 Url
    // 例如：byHost["www.google.com"] = UrlId
    {
        urlId = default;

        var normalized = NormalizeHost(candidate);
        if (!string.IsNullOrEmpty(normalized) && byHost.TryGetValue(normalized, out urlId))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeHost(string? value)
    // 正規化 Url 的 host
    // 例如：https://www.google.com 的 host 為 www.google.com
    // 所以 NormalizeHost("https://www.google.com") = "www.google.com"
    // 並且 NormalizeHost("www.google.com/test") = "www.google.com"
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }

        candidate = trimmed.ToLowerInvariant();
        if (candidate.StartsWith("http://", StringComparison.Ordinal))
        {
            candidate = candidate[7..];
        }
        else if (candidate.StartsWith("https://", StringComparison.Ordinal))
        {
            candidate = candidate[8..];
        }

        var slashIndex = candidate.IndexOf('/');
        if (slashIndex >= 0)
        {
            candidate = candidate[..slashIndex];
        }

        if (candidate.StartsWith("www.", StringComparison.Ordinal))
        {
            candidate = candidate[4..];
        }

        return candidate;
    }
}


