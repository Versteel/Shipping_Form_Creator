using System.Collections.ObjectModel;
using Shipping_Form_CreatorV1.Models;
using System.Windows;
using System.Windows.Controls;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for PackingListNotesPage.xaml
    /// </summary>
    public partial class PackingListNotesPage : UserControl
    {

        public static readonly DependencyProperty PageNumberTextProperty =
            DependencyProperty.Register(nameof(PageNumberText), typeof(string), typeof(PackingListNotesPage),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ReportHeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(ReportHeader),
                typeof(PackingListNotesPage), new PropertyMetadata(null));

        public static readonly DependencyProperty DetailsProperty =
           DependencyProperty.Register(nameof(Details), typeof(ObservableCollection<LineItemDetail>),
               typeof(PackingListNotesPage), new PropertyMetadata(null));
        public bool IsPrinting
        {
            get => (bool)GetValue(IsPrintingProperty);
            set => SetValue(IsPrintingProperty, value);
        }

        public static readonly DependencyProperty IsPrintingProperty =
            DependencyProperty.Register(
                nameof(IsPrinting),
                typeof(bool),
                typeof(PackingListNotesPage),
                new PropertyMetadata(false));

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

        public PackingListNotesPage()
        {
            InitializeComponent();
        }
    }
}
