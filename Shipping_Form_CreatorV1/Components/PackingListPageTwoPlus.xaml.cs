using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static iTextSharp.text.pdf.AcroFields;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class PackingListPageTwoPlus
    {
        public PackingListPageTwoPlus()
        {
            InitializeComponent();
        }

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

        public ICommand AddPackUnitCommand => new RelayCommand(param =>
        {
            if (param is not LineItem lineItemCopy) return;
            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            // --- THIS IS THE CRITICAL LINE ---
            // Look up the original item using the unique LineItemNumber, NOT the Id.
            var originalLineItem = viewModel.SelectedReport.LineItems
                .FirstOrDefault(li => li.LineItemHeader?.LineItemNumber == lineItemCopy.LineItemHeader?.LineItemNumber);

            // If the lookup fails for any reason, stop here.
            if (originalLineItem == null) return;

            var newPackingUnit = new LineItemPackingUnit
            {
                Id = 0, // Mark as a new record for the database
                TruckNumber = viewModel.Trucks.FirstOrDefault() ?? "TRUCK 1",
                Quantity = 1,
                CartonOrSkid = CartonOrSkidOptions.FirstOrDefault() ?? "BOX",
                TypeOfUnit = string.Empty,
                Weight = 0,
                LineItem = originalLineItem,
                LineItemId = originalLineItem.Id
            };

            // Add to the master list (for saving)
            originalLineItem.LineItemPackingUnits.Add(newPackingUnit);

            // Add to the UI's list (for immediate visual update)
            lineItemCopy.LineItemPackingUnits.Add(newPackingUnit);

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
                    // This modifies the original collection
                    lineItem.LineItemPackingUnits.Remove(unitToRemove);

                    // Now, tell the ViewModel to update everything, which will trigger the refresh
                    viewModel.UpdateViewOptions();
                    break; // Exit loop once the item is found and removed
                }
            }
        });

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
