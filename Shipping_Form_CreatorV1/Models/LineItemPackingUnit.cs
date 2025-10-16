using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shipping_Form_CreatorV1.Models;

public class LineItemPackingUnit
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? CartonOrSkid { get; set; }
    public int LineNumber { get; set; }
    public string? TypeOfUnit { get; set; }
    public string? CartonOrSkidContents { get; set; }
    public int Weight { get; set; }
    public string TruckNumber { get; set; } = Constants.TruckNumbers[0];
    public int LineItemId { get; set; }
    public int? HandlingUnitId { get; set; }
    public virtual HandlingUnit? HandlingUnit { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LineItem LineItem { get; set; } = null!;
    [NotMapped]
    public string DisplayTypeOfUnit
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TypeOfUnit)) return "";
            return TypeOfUnit.ToUpperInvariant() switch
            {
                "CHAIRS" or "CURVARE" or "IMMIX" or "OH!" or "OLLIE" => "CHAIRS",
                "TABLE LEGS, TUBULAR STL EXC 1 QTR DIAMETER, N-G-T 2 INCH DIAMETER" => "TABLE LEGS",
                "PACKED WITH LINE " => CartonOrSkidContents,
                _ => TypeOfUnit.ToUpperInvariant()
            };
        }
    }

    // Default constructor
    public LineItemPackingUnit() { }

    // --- ADD THIS NEW CONSTRUCTOR ---
    /// <summary>
    /// Creates a copy of a LineItemPackingUnit instance.
    /// </summary>
    /// <param name="original">The object to copy.</param>
    public LineItemPackingUnit(LineItemPackingUnit original)
    {
        this.Id = original.Id;
        this.Quantity = original.Quantity;
        this.CartonOrSkid = original.CartonOrSkid;
        this.LineNumber = original.LineNumber;
        this.TypeOfUnit = original.TypeOfUnit;
        this.CartonOrSkidContents = original.CartonOrSkidContents;
        this.Weight = original.Weight;
        this.TruckNumber = original.TruckNumber;
        this.LineItemId = original.LineItemId;
        this.HandlingUnitId = original.HandlingUnitId;
    }
}