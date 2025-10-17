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


    private const string PackedWithLineIdentifier = "PACKED WITH LINE ";
    [NotMapped]
    public string DisplayTypeOfUnit =>
        CartonOrSkid == PackedWithLineIdentifier
            ? CartonOrSkidContents ?? string.Empty
            : GetNormalizedTypeOfUnit();

    [NotMapped] 
    public string PackingUnitSummary
    {
        get
        {
            // Safely get the line item number, providing a fallback if it's null
            string lineNum = LineItem?.LineItemHeader?.LineItemNumber.ToString() ?? "N/A";

            // Check the condition just like in your DisplayTypeOfUnit property
            if (CartonOrSkid == PackedWithLineIdentifier)
            {
                // Case 1: "PACKED WITH LINE" -> Omit the CartonOrSkid part
                return $"{Quantity} x {DisplayTypeOfUnit} (Line {lineNum})";
            }
            else
            {
                // Case 2: Standard item -> Include all parts
                return $"{Quantity} x {CartonOrSkid} {DisplayTypeOfUnit} (Line {lineNum})";
            }
        }
    }

    private string GetNormalizedTypeOfUnit()
    {
        // Handle the null or empty case first.
        if (string.IsNullOrWhiteSpace(TypeOfUnit))
        {
            return string.Empty;
        }

        // Convert to uppercase once to avoid repeating the call.
        var upperTypeOfUnit = TypeOfUnit.ToUpperInvariant();

        // The switch expression is already clean and efficient.
        return upperTypeOfUnit switch
        {
            "CHAIRS" or "CURVARE" or "IMMIX" or "OH!" or "OLLIE" => "CHAIRS",
            "TABLE LEGS, TUBULAR STL EXC 1 QTR DIAMETER, N-G-T 2 INCH DIAMETER" => "TABLE LEGS",
            _ => upperTypeOfUnit
        };
    }

    public LineItemPackingUnit() { }

    
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