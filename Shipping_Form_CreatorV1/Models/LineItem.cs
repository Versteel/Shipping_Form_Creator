using System.Collections.ObjectModel;

namespace Shipping_Form_CreatorV1.Models;

public class LineItem
{
    public int Id { get; set; }

    public int LineItemHeaderId { get; set; }
    public LineItemHeader? LineItemHeader { get; set; }
    public ObservableCollection<LineItemDetail> LineItemDetails { get; set; } = [];
    public ObservableCollection<LineItemPackingUnit> LineItemPackingUnits { get; set; } = [];
    
    public int ReportModelId { get; set; }
    public ReportModel ReportModel { get; set; } = null!;
}

