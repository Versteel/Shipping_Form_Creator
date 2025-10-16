using Shipping_Form_CreatorV1.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for PackingListNotesPage.xaml
    /// </summary>
    public partial class PackingListNotesPage
    {
        // Original Properties
        public static readonly DependencyProperty PageNumberTextProperty =
            DependencyProperty.Register(nameof(PageNumberText), typeof(string), typeof(PackingListNotesPage), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ReportHeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(ReportHeader), typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty DetailsProperty =
           DependencyProperty.Register(nameof(Details), typeof(ObservableCollection<LineItemDetail>), typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty HandlingUnitsProperty =
            DependencyProperty.Register(nameof(HandlingUnits), typeof(ObservableCollection<HandlingUnit>), typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty IsPrintingProperty =
            DependencyProperty.Register(nameof(IsPrinting), typeof(bool), typeof(PackingListNotesPage), new PropertyMetadata(false));


        public static readonly DependencyProperty ShippingInstructionsProperty =
            DependencyProperty.Register(nameof(ShippingInstructions), typeof(ObservableCollection<string>), typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty ConsolidatedSummaryProperty =
           DependencyProperty.Register(nameof(ConsolidatedSummary), typeof(ObservableCollection<PackingListSummaryItem>), typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty OverallTotalsProperty =
            DependencyProperty.Register(nameof(OverallTotals), typeof(ObservableCollection<TotalsItem>),
                typeof(PackingListNotesPage), new PropertyMetadata(null));

        // Property Wrappers
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

        public ObservableCollection<HandlingUnit>? HandlingUnits
        {
            get => (ObservableCollection<HandlingUnit>?)GetValue(HandlingUnitsProperty);
            set => SetValue(HandlingUnitsProperty, value);
        }

        public bool IsPrinting
        {
            get => (bool)GetValue(IsPrintingProperty);
            set => SetValue(IsPrintingProperty, value);
        }

        public ObservableCollection<string>? ShippingInstructions
        {
            get => (ObservableCollection<string>?)GetValue(ShippingInstructionsProperty);
            set => SetValue(ShippingInstructionsProperty, value);
        }

        public ObservableCollection<PackingListSummaryItem>? ConsolidatedSummary
        {
            get => (ObservableCollection<PackingListSummaryItem>?)GetValue(ConsolidatedSummaryProperty);
            set => SetValue(ConsolidatedSummaryProperty, value);
        }

        public ObservableCollection<TotalsItem>? OverallTotals
        {
            get => (ObservableCollection<TotalsItem>?)GetValue(OverallTotalsProperty);
            set => SetValue(OverallTotalsProperty, value);
        }

        public PackingListNotesPage()
        {
            InitializeComponent();
        }
    }
}