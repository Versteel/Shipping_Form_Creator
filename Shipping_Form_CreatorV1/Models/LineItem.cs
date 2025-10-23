using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices; // You'll need this for ToList() or similar if used elsewhere

namespace Shipping_Form_CreatorV1.Models;

public class LineItem : INotifyPropertyChanged
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int LineItemHeaderId { get; set; }
    public LineItemHeader? LineItemHeader { get; set; }


    public ObservableCollection<LineItemDetail> LineItemDetails { get; set; } = [];
    public ObservableCollection<LineItemPackingUnit> LineItemPackingUnits { get; set; } = [];

    public int ReportModelId { get; set; }
    public ReportModel ReportModel { get; set; } = null!;

    private double _packingUnitHeight = 60;
    [NotMapped]
    public double PackingUnitHeight
    {
        get => _packingUnitHeight;
        set => SetField(ref _packingUnitHeight, value);
    }

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
        Id = original.Id;
        LineItemHeaderId = original.LineItemHeaderId;
        LineItemHeader = original.LineItemHeader; // Copying reference is fine for immutable headers
        ReportModelId = original.ReportModelId;
        ReportModel = original.ReportModel;

        LineItemDetails = new ObservableCollection<LineItemDetail>(original.LineItemDetails);

        LineItemPackingUnits = newPackingUnits;
        PackingUnitHeight = original.PackingUnitHeight;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

