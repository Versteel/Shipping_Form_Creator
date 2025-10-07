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
            Debug.WriteLine($"[DEBUG] Received CommandParameter of type: {param?.GetType().Name}");

            if (param is not LineItem lineItem)
            {
                MessageBox.Show("LineItem not passed to AddPackUnitCommand");
                return;
            }

            // Get the MainViewModel
            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            var newPackingUnit = new LineItemPackingUnit
            {
                // Use the first truck from the dynamic list as the default
                TruckNumber = viewModel.Trucks.FirstOrDefault() ?? "TRUCK 1",
                Quantity = 1,
                CartonOrSkid = CartonOrSkidOptions.FirstOrDefault() ?? "BOX",
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

            viewModel.UpdateViewOptions();

            Debug.WriteLine($"[Add] Added new packing unit to LineItem {lineItem.Id}");
        });

        public ICommand RemovePackUnitCommand => new RelayCommand(pu =>
        {
            if (pu is not LineItemPackingUnit packUnit || LineItem == null) return;
            LineItem.LineItemPackingUnits.Remove(packUnit);

            // Get the MainViewModel and tell it to update the list
            if (Application.Current.MainWindow?.DataContext is MainViewModel viewModel)
            {
                viewModel.UpdateViewOptions();
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
            // Get the MainViewModel from the application's main window
            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            // If a truck was selected, tell the view model
            if (sender is ComboBoxAdv comboBox && comboBox.SelectedItem is string selectedTruck)
            {
                viewModel.SelectedTruck = selectedTruck;
            }
        }
    }
}