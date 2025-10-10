using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.Linq; // You'll need this for ToList() or similar if used elsewhere

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

    // ------------------------------------------------------------------
    // NEW: Copy Constructor for Print Filtering
    // ------------------------------------------------------------------
    public LineItem() { }

    /// <summary>
    /// Creates a deep copy of a LineItem, allowing for a new collection of Packing Units.
    /// </summary>
    /// <param name="original">The original LineItem to copy properties from.</param>
    /// <param name="newPackingUnits">The (already filtered) collection of Packing Units to use.</param>
    public LineItem(LineItem original, ObservableCollection<LineItemPackingUnit> newPackingUnits)
    {
        // Copy simple value types and references
        Id = original.Id;
        LineItemHeaderId = original.LineItemHeaderId;
        LineItemHeader = original.LineItemHeader; // Copying reference is fine for immutable headers
        ReportModelId = original.ReportModelId;
        ReportModel = original.ReportModel;

        // Copy collections (shallow copy of items is usually fine, unless Details are modified)
        LineItemDetails = new ObservableCollection<LineItemDetail>(original.LineItemDetails);

        // Assign the NEW (filtered) collection
        LineItemPackingUnits = newPackingUnits;
    }
}

