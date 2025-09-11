using Shipping_Form_CreatorV1.Models;

namespace Shipping_Form_CreatorV1.Services.Interfaces;

public interface IOdbcService
{
    Task<ReportModel?> GetReportAsync(int orderNumberInput, int suffixInput, CancellationToken ct = default);
    Task<List<ReportModel>> GetShippedOrdersByDate(DateTime shipDate, CancellationToken ct = default);
}
