using Shipping_Form_CreatorV1.Models;

namespace Shipping_Form_CreatorV1.Services.Interfaces;

public interface ISqliteService
{
    // Returns existing report from local database
    Task<ReportModel?> GetReportAsync(int orderNumber, CancellationToken ct = default);
    // Saves or updates report in local database
    Task SaveReportAsync(ReportModel report, CancellationToken ct = default);
}
