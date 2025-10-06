using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;
using System.Reflection.Metadata;

namespace Shipping_Form_CreatorV1.Models;

public class LineItemPackingUnit
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? CartonOrSkid { get; set; }
    public int LineNumber { get; set; }
    public string? TypeOfUnit { get; set; }
    public string? CartonOrSkidContents { get; set; }
    public int Weight { get; set; }
    public string TruckNumber { get; set; } = Constants.TruckNumbers[0];
    public int LineItemId { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LineItem LineItem { get; set; } = null!;
}

