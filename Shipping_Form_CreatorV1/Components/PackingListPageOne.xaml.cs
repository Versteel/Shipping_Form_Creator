using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for PackingListPageOne.xaml
    /// </summary>
    public partial class PackingListPageOne : UserControl
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
        public int Weight { get; set; }

        public string UomText => string.Equals(CartonOrSkid?.Trim(), Constants.CartonOrSkidOptions[2], StringComparison.OrdinalIgnoreCase) ? string.Empty : "Lbs";
        public static string[] CartonOrSkidOptions => Constants.CartonOrSkidOptions;

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

        public ICommand AddPackUnitCommand => new RelayCommand(() =>
        {
            if (LineItem == null)
            {
                MessageBox.Show("LineItem is not set.");
                return;
            }

            var newPackingUnit = new LineItemPackingUnit
            {
                Quantity = PackUnitQty,
                CartonOrSkid = CartonOrSkid,
                TypeOfUnit = TypeOfUnit,
                Weight = Weight,
                LineItem = LineItem,
                LineItemId = LineItem.Id
            };

            LineItem.LineItemPackingUnits.Add(newPackingUnit);
            System.Diagnostics.Debug.WriteLine($"[CMD] After Add: Count = {LineItem.LineItemPackingUnits.Count}");
        });

        public ICommand RemovePackUnitCommand => new RelayCommand(pu =>
        {
            if (pu is not LineItemPackingUnit packUnit || LineItem == null) return;
            LineItem.LineItemPackingUnits.Remove(packUnit);
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
            set
            {
                ObservableCollection<LineItemDetail> list = value != null ? [.. value] : [];

                if (list.Count >= 2)
                {
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(0);
                }

                SetValue(DetailsProperty, list);
            }
        }
    }
}
