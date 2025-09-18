using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1.Components;

public partial class PackingListPageTwoPlus
{
    public static readonly DependencyProperty PageNumberTwoPlusTextProperty =
        DependencyProperty.Register(nameof(PageNumberTwoPlusText), typeof(string), typeof(PackingListPageTwoPlus), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ReportHeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(ReportHeader), typeof(PackingListPageTwoPlus), new PropertyMetadata(null));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<LineItem>), typeof(PackingListPageTwoPlus), new PropertyMetadata(null));

    public bool IsPrinting
    {
        get => (bool)GetValue(IsPrintingProperty);
        set => SetValue(IsPrintingProperty, value);
    }

    public static readonly DependencyProperty IsPrintingProperty =
        DependencyProperty.Register(
            nameof(IsPrinting),
            typeof(bool),
            typeof(PackingListPageTwoPlus),
            new PropertyMetadata(false));

    public string PageNumberTwoPlusText
    {
        get => (string)GetValue(PageNumberTwoPlusTextProperty);
        set => SetValue(PageNumberTwoPlusTextProperty, value);
    }

    public ReportHeader? Header
    {
        get => (ReportHeader?)GetValue(ReportHeaderProperty);
        set => SetValue(ReportHeaderProperty, value);
    }

    public ObservableCollection<LineItem>? Items
    {
        get => (ObservableCollection<LineItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static string[] CartonOrSkidOptions => Constants.CartonOrSkidOptions;
    public static string[] PackingUnitCategories => Constants.PackingUnitCategories;


    // You can bind to these globally and pass the LineItem as CommandParameter
    public ICommand AddPackUnitCommand => new RelayCommand(param =>
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Received CommandParameter of type: {param?.GetType().Name}");

        if (param is not LineItem lineItem)
        {
            MessageBox.Show("LineItem not passed to AddPackUnitCommand");
            return;
        }

        var newPackingUnit = new LineItemPackingUnit
        {
            Quantity = 1,
            CartonOrSkid = CartonOrSkidOptions.FirstOrDefault() ?? "Carton",
            TypeOfUnit = string.Empty,
            Weight = 0,
            LineItem = lineItem,
            LineItemId = lineItem.Id
        };

        if (lineItem.LineItemPackingUnits is { } observable)
        {
            observable.Add(newPackingUnit);
        }
        else
        {
            lineItem.LineItemPackingUnits = [newPackingUnit];
        }

        System.Diagnostics.Debug.WriteLine($"[Add] Added new packing unit to LineItem {lineItem.Id}");
    });

    public ICommand RemovePackUnitCommand => new RelayCommand(param =>
    {
        if (param is not LineItemPackingUnit unit) return;

        foreach (var lineItem in Items ?? [])
        {
            if (!lineItem.LineItemPackingUnits.Contains(unit))
            {
                continue;
            }
            lineItem.LineItemPackingUnits.Remove(unit);
            break;
        }
    });

    public PackingListPageTwoPlus()
    {
        InitializeComponent();
    }
}