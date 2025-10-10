using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;

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

    // Default constructor
    public LineItemPackingUnit() { }

    // --- ADD THIS NEW CONSTRUCTOR ---
    /// <summary>
    /// Creates a copy of a LineItemPackingUnit instance.
    /// </summary>
    /// <param name="original">The object to copy.</param>
    public LineItemPackingUnit(LineItemPackingUnit original)
    {
        // Copy all data properties from the original object.
        this.Id = original.Id;
        this.Quantity = original.Quantity;
        this.CartonOrSkid = original.CartonOrSkid;
        this.LineNumber = original.LineNumber;
        this.TypeOfUnit = original.TypeOfUnit;
        this.CartonOrSkidContents = original.CartonOrSkidContents;
        this.Weight = original.Weight;
        this.TruckNumber = original.TruckNumber;
        this.LineItemId = original.LineItemId;
        // NOTE: We intentionally do not copy the 'LineItem' parent reference.
    }
}