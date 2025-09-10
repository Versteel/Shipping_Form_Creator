using Shipping_Form_CreatorV1.Models;

namespace Shipping_Form_CreatorV1.Services.Interfaces;

public interface IOdbcService
{
    Task<List<ReportModel>> GetAllReportsAsync(CancellationToken ct = default);
    Task<ReportModel?> GetReportAsync(int orderNumberInput, CancellationToken ct = default);
}
