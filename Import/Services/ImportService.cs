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

        // Build lookup for UrlList by WebName
        var urlByWebName = await _dbContext.UrlLists.AsNoTracking().ToDictionaryAsync(x => x.WebName, x => x.UrlId);

        // ZapReport upsert by (SiteUrlId, GeneratedDay)
        // ReportId 由 (SiteUrlId, GeneratedDay) 計算而得
        if (request.ZapReports?.Count > 0)
        {
            foreach (var dto in request.ZapReports)
            {
                if (!urlByWebName.TryGetValue(dto.SiteWebName, out var siteUrlId))
                {
                    // 無對應站點，略過
                    result.ZapReportsSkipped++;
                    result.SkippedReasons.Add($"ZapReport 略過：找不到 WebName='{dto.SiteWebName}' 對應的 UrlList。請確認 siteWebName 與 UrlList.webName 一致。");
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
            var validAlerts = request.ZapAlerts.Where(x => urlByWebName.ContainsKey(x.RootWebName)).ToList();
            var skippedAlerts = request.ZapAlerts.Where(x => !urlByWebName.ContainsKey(x.RootWebName)).ToList();

            foreach (var alert in skippedAlerts)
            {
                result.ZapAlertsSkipped++;
                result.SkippedReasons.Add($"ZapAlert 略過：找不到 WebName='{alert.RootWebName}' 對應的 UrlList。請確認 rootWebName 與 UrlList.webName 一致。");
            }

            var groups = validAlerts
                .GroupBy(x => (RootUrlId: urlByWebName[x.RootWebName], x.ReportDay))
                .ToList();

            if (request.ReplaceAlertsForSubmittedDays)
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
                foreach (var dto in g)
                {
                    _dbContext.ZapalertDetails.Add(new ZapalertDetail
                    {
                        RootUrlId = g.Key.RootUrlId,
                        Url = dto.Url,
                        ReportDate = dto.ReportDate,
                        ReportDay = dto.ReportDay,
                        RiskName = dto.RiskName,
                        Level = dto.Level,
                        Method = dto.Method,
                        Parameter = dto.Parameter,
                        Attack = dto.Attack,
                        Evidence = dto.Evidence,
                        Status = dto.Status ?? "Open",
                        OtherInfo = dto.OtherInfo,
                    });
                    result.ZapAlertsInserted++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        await trx.CommitAsync();
        return result;
    }
}


