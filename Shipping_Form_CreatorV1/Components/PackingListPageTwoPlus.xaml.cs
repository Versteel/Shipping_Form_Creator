using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class PackingListPageTwoPlus
    {
        public PackingListPageTwoPlus()
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel vm)
            {
                PackingUnitCategories = vm.IsDittoUser ? Constants.DittoPackingUnitCategories : Constants.PackingUnitCategories;
            }
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
        public static string[] PackingUnitCategories;
                
        public ICommand AddPackUnitCommand => new RelayCommand(param =>
        {
            if (param is not LineItem lineItemCopy) return;
            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            var originalLineItem = viewModel.SelectedReport.LineItems
                .FirstOrDefault(li => li.LineItemHeader?.LineItemNumber == lineItemCopy.LineItemHeader?.LineItemNumber);

            if (originalLineItem == null) return;

            var newPackingUnit = new LineItemPackingUnit
            {
                Id = 0,
                TruckNumber = viewModel.Trucks.FirstOrDefault() ?? "TRUCK 1",
                Quantity = 1,
                CartonOrSkid = CartonOrSkidOptions.FirstOrDefault() ?? "BOX",
                TypeOfUnit = string.Empty,
                Weight = 0,
                LineItem = originalLineItem,
                LineItemId = originalLineItem.Id
            };

            newPackingUnit.PropertyChanged += viewModel.OnPackingUnitPropertyChanged;

            originalLineItem.LineItemPackingUnits.Add(newPackingUnit);
            lineItemCopy.LineItemPackingUnits.Add(newPackingUnit);
            
            viewModel.UpdateViewOptions();
            viewModel.MarkAsUnsaved();
        });

        private void OnInputChanged(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MarkAsUnsaved();
            }
        }

        public ICommand RemovePackUnitCommand => new RelayCommand(param =>
        {
            if (param is not LineItemPackingUnit unitToRemove) return;

            if (Application.Current.MainWindow?.DataContext is not MainViewModel viewModel) return;

            var masterReport = viewModel.SelectedReport;
            if (masterReport == null) return;

            foreach (var lineItem in masterReport.LineItems)
            {
                if (lineItem.LineItemPackingUnits.Contains(unitToRemove))
                {
                    if (unitToRemove.HandlingUnitId.HasValue && unitToRemove.HandlingUnit is not null)
                    {
                        unitToRemove.HandlingUnit.ContainedUnits.Remove(unitToRemove);
                        unitToRemove.HandlingUnitId = null;
                        unitToRemove.HandlingUnit = null;
                    }

                    lineItem.LineItemPackingUnits.Remove(unitToRemove);
                    viewModel.OnPropertyChanged(nameof(MainViewModel.SelectedReport));
                   
                    viewModel.UpdateViewOptions();
                    break;
                }
            }
            viewModel.MarkAsUnsaved();
        });

        private void TruckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!IsLoaded) return;

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
            viewModel.MarkAsUnsaved();
        }
    }
}
