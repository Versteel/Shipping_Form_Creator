using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using Syncfusion.Windows.Tools.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for PackingListPageOne.xaml
    /// </summary>
    public partial class PackingListPageOne
    {
        public static readonly DependencyProperty PageNumberTextProperty =
            DependencyProperty.Register(nameof(PageNumberText), typeof(string), typeof(PackingListPageOne),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ReportHeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(ReportHeader),
                typeof(PackingListPageOne), new PropertyMetadata(null));

        public static readonly DependencyProperty LineItemProperty =
            DependencyProperty.Register(nameof(LineItem), typeof(LineItem),
                typeof(PackingListPageOne), new PropertyMetadata(null));

        public static readonly DependencyProperty DetailsProperty =
            DependencyProperty.Register(nameof(Details), typeof(ObservableCollection<LineItemDetail>),
                typeof(PackingListPageOne), new PropertyMetadata(null));

        public static readonly DependencyProperty PackingUnitsProperty =
            DependencyProperty.Register(
                nameof(PackingUnits),
                typeof(ObservableCollection<LineItemPackingUnit>),
                typeof(PackingListPageOne),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public bool IsPrinting
        {
            get => (bool)GetValue(IsPrintingProperty);
            set => SetValue(IsPrintingProperty, value);
        }

        public static readonly DependencyProperty IsPrintingProperty =
            DependencyProperty.Register(
                nameof(IsPrinting),
                typeof(bool),
                typeof(PackingListPageOne),
                new PropertyMetadata(false));

        public int PackUnitQty { get; set; }
        public string CartonOrSkid { get; set; } = string.Empty;
        public string TypeOfUnit { get; set; } = string.Empty;
        public string TruckNumber { get; set; } = Constants.TruckNumbers[0];
        public int Weight { get; set; }

        public string UomText => string.Equals(CartonOrSkid?.Trim(), Constants.CartonOrSkidOptions[2], StringComparison.OrdinalIgnoreCase) ? string.Empty : "Lbs";
        public static string[] CartonOrSkidOptions => Constants.CartonOrSkidOptions;
        public static string[] PackingUnitCategories => Constants.PackingUnitCategories;
        public static string[] TruckNumbers => Constants.TruckNumbers;

        public ObservableCollection<LineItemPackingUnit> PackingUnits
        {
            get => (ObservableCollection<LineItemPackingUnit>)GetValue(PackingUnitsProperty);
            set => SetValue(PackingUnitsProperty, value);
        }

        public LineItem? LineItem
        {
            get => (LineItem?)GetValue(LineItemProperty);
            set => SetValue(LineItemProperty, value);
        }

        public ICommand AddPackUnitCommand => new RelayCommand(param =>
        {
            // The parameter is the temporary LineItem copy from the UI
            if (param is not LineItem lineItemCopy)
            {
                return;
            }

            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            // Find the original LineItem in the master data source using the ID from the copy
            var originalLineItem = viewModel.SelectedReport.LineItems
                .FirstOrDefault(li => li.Id == lineItemCopy.Id);

            if (originalLineItem == null)
            {
                return;
            }

            // This is the corrected part:
            var newPackingUnit = new LineItemPackingUnit
            {
                Id = 0, // Use 0 to indicate a new entity. The database will generate the real ID.
                TruckNumber = viewModel.Trucks.FirstOrDefault() ?? "TRUCK 1",
                Quantity = 1,
                CartonOrSkid = CartonOrSkidOptions.FirstOrDefault() ?? "BOX",
                TypeOfUnit = string.Empty,
                Weight = 0,
                LineItem = originalLineItem,
                LineItemId = originalLineItem.Id
            };

            // Add the new unit to the ORIGINAL LineItem's collection
            if (originalLineItem.LineItemPackingUnits is { } observable)
            {
                observable.Add(newPackingUnit);
            }
            else
            {
                originalLineItem.LineItemPackingUnits = [newPackingUnit];
            }

            viewModel.UpdateViewOptions();
        });

        public ICommand RemovePackUnitCommand => new RelayCommand(param =>
        {
            if (param is not LineItemPackingUnit unitToRemove) return;

            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            // Get the master report from the ViewModel (the single source of truth)
            var masterReport = viewModel.SelectedReport;
            if (masterReport == null) return;

            // Find the line item in the MASTER list that contains the unit and remove it
            foreach (var lineItem in masterReport.LineItems)
            {
                if (lineItem.LineItemPackingUnits.Contains(unitToRemove))
                {

                    if (unitToRemove.HandlingUnitId.HasValue && unitToRemove.HandlingUnit is not null)
                    {
                        // If the packing unit is associated with a handling unit, remove that association
                        unitToRemove.HandlingUnit.ContainedUnits.Remove(unitToRemove);
                        unitToRemove.HandlingUnitId = null;
                        unitToRemove.HandlingUnit = null;
                    }

                    // This modifies the original collection
                    lineItem.LineItemPackingUnits.Remove(unitToRemove);

                    // Now, tell the ViewModel to update everything, which will trigger the refresh
                    viewModel.UpdateViewOptions();
                    break; // Exit loop once the item is found and removed
                }
            }
        });

        public PackingListPageOne()
        {

            InitializeComponent();
        }

        public string PageNumberText
        {
            get => (string)GetValue(PageNumberTextProperty);
            set => SetValue(PageNumberTextProperty, value);
        }

        public ReportHeader? Header
        {
            get => (ReportHeader?)GetValue(ReportHeaderProperty);
            set => SetValue(ReportHeaderProperty, value);
        }

        public ObservableCollection<LineItemDetail>? Details
        {
            get => (ObservableCollection<LineItemDetail>?)GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
        }

        private void TruckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure we have a view model and something was actually selected
            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel || e.AddedItems.Count == 0)
            {
                return;
            }

            // Get the newly selected truck value from the event arguments
            if (e.AddedItems[0] is not string selectedTruck) return;

            // Get the ComboBox and its DataContext (the specific LineItemPackingUnit copy)
            if (sender is not FrameworkElement comboBox || comboBox.DataContext is not LineItemPackingUnit unitCopy)
            {
                return;
            }

            var originalLineItem = viewModel.SelectedReport.LineItems
                .FirstOrDefault(li => li.Id == unitCopy.LineItemId);

            if (originalLineItem == null) return;

            
            viewModel.SelectedTruck = selectedTruck;
        }
    }
}