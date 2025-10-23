using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices; // Needed for CallerMemberName
using System.Collections.Generic;    // Needed for EqualityComparer

namespace Shipping_Form_CreatorV1.Models;

// 1. Add INotifyPropertyChanged
public class LineItemPackingUnit : INotifyPropertyChanged
{
    // --- Database/Model Properties ---
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int LineItemId { get; set; }
    public int? HandlingUnitId { get; set; }
    public virtual HandlingUnit? HandlingUnit { get; set; }

    // Use [DesignerSerializationVisibility] only if needed for specific designer behavior
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual LineItem LineItem { get; set; } = null!; // Keep virtual if lazy loading used

    // --- INotifyPropertyChanged Implementation ---
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // Also notify dependent computed properties
        if (propertyName == nameof(Quantity) ||
            propertyName == nameof(CartonOrSkid) ||
            propertyName == nameof(TypeOfUnit) ||
            propertyName == nameof(CartonOrSkidContents))
        {
            OnPropertyChanged(nameof(PackingUnitSummary));
        }
        if (propertyName == nameof(CartonOrSkid) ||
            propertyName == nameof(TypeOfUnit) ||
            propertyName == nameof(CartonOrSkidContents))
        {
            OnPropertyChanged(nameof(DisplayTypeOfUnit));
        }
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName); // This calls the combined notification logic above
        return true;
    }

    // --- Properties Bound to UI (with Backing Fields) ---
    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    private string? _cartonOrSkid;
    public string? CartonOrSkid
    {
        get => _cartonOrSkid;
        // When CartonOrSkid changes, DisplayTypeOfUnit and PackingUnitSummary might also change
        set => SetField(ref _cartonOrSkid, value);
    }

    private int _lineNumber;
    public int LineNumber
    {
        get => _lineNumber;
        // When LineNumber changes, PackingUnitSummary might also change
        set => SetField(ref _lineNumber, value);
    }

    private string? _typeOfUnit;
    public string? TypeOfUnit
    {
        get => _typeOfUnit;
        // When TypeOfUnit changes, DisplayTypeOfUnit and PackingUnitSummary might also change
        set => SetField(ref _typeOfUnit, value);
    }

    private string? _cartonOrSkidContents;
    public string? CartonOrSkidContents
    {
        get => _cartonOrSkidContents;
        // When CartonOrSkidContents changes, DisplayTypeOfUnit and PackingUnitSummary might also change
        set => SetField(ref _cartonOrSkidContents, value);
    }

    private int _weight;
    public int Weight
    {
        get => _weight;
        set => SetField(ref _weight, value);
    }

    private string _truckNumber = Constants.TruckNumbers[0]; // Initialize backing field
    public string TruckNumber
    {
        get => _truckNumber;
        set => SetField(ref _truckNumber, value);
    }

    // --- Computed Properties ---
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
            string lineNum = LineItem?.LineItemHeader?.LineItemNumber.ToString() ?? "N/A";

            // Use DisplayTypeOfUnit which already contains normalization logic
            string displayUnit = DisplayTypeOfUnit;

            if (CartonOrSkid == PackedWithLineIdentifier)
            {
                return $"{Quantity} x {displayUnit} (Line {lineNum})";
            }
            else
            {
                // Include CartonOrSkid only if it's not null/empty
                return string.IsNullOrWhiteSpace(CartonOrSkid)
                    ? $"{Quantity} x {displayUnit} (Line {lineNum})"
                    : $"{Quantity} x {CartonOrSkid} {displayUnit} (Line {lineNum})";
            }
        }
    }

    // --- Helper Methods & Constructors ---
    private string GetNormalizedTypeOfUnit()
    {
        if (string.IsNullOrWhiteSpace(TypeOfUnit)) return string.Empty;
        var upperTypeOfUnit = TypeOfUnit.ToUpperInvariant();
        return upperTypeOfUnit switch
        {
            "CHAIRS" or "CURVARE" or "IMMIX" or "OH!" or "OLLIE" => "CHAIRS",
            "TABLE LEGS, TUBULAR STL EXC 1 QTR DIAMETER, N-G-T 2 INCH DIAMETER" => "TABLE LEGS",
            _ => upperTypeOfUnit
        };
    }

    public LineItemPackingUnit() { }

    // Copy constructor remains the same
    public LineItemPackingUnit(LineItemPackingUnit original)
    {
        // Initialize fields directly or via properties if validation needed
        _quantity = original.Quantity;
        _cartonOrSkid = original.CartonOrSkid;
        _lineNumber = original.LineNumber;
        _typeOfUnit = original.TypeOfUnit;
        _cartonOrSkidContents = original.CartonOrSkidContents;
        _weight = original.Weight;
        _truckNumber = original.TruckNumber;

        // Non-notifying properties
        this.Id = original.Id;
        this.LineItemId = original.LineItemId;
        this.HandlingUnitId = original.HandlingUnitId;
        // Don't copy LineItem/HandlingUnit references directly unless intended
    }
}