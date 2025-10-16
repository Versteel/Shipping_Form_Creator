using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shipping_Form_CreatorV1.Models;

public class HandlingUnit : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int ReportModelId { get; set; }
    public virtual ReportModel ReportModel { get; set; } = null!;

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public virtual ObservableCollection<LineItemPackingUnit> ContainedUnits { get; set; } = new();


    [NotMapped]
    public int TotalWeight => ContainedUnits.Sum(u => u.Weight);
    [NotMapped]
    public int TotalPieces => ContainedUnits.Sum(u => u.Quantity);

    public HandlingUnit()
    {
        // This part is crucial: it listens for changes and updates the totals.
        ContainedUnits.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TotalWeight));
            OnPropertyChanged(nameof(TotalPieces));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}