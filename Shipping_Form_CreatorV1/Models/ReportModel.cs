namespace Shipping_Form_CreatorV1.Models;

public class ReportModel
{
    public int Id { get; set; }

    public int ReportHeaderId { get; set; }
    public ReportHeader Header { get; set; } = null!;
    public ICollection<LineItem> LineItems { get; set; } = [];
}

