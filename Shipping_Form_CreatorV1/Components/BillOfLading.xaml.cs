using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using System.Windows;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class BillOfLading
    {
        public BillOfLading(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.SelectedReport.Header.LogoImagePath = vm.IsDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;
            MarkCollectCheckBox.IsChecked = vm.SelectedReport.Header.FreightTerms == "COLLECT";
            ShowCollectText = vm.SelectedReport.Header.FreightTerms == "COLLECT";
        }
        public bool ShowCollectText { get; set; }
        public bool ShowPrepaidText => !ShowCollectText;
        public bool IsCollectChecked { get; set; }
        public static string TodaysDate => DateTime.Now.ToShortDateString();
        
        public static readonly DependencyProperty IsPrintingProperty =
            DependencyProperty.Register(nameof(IsPrinting), typeof(bool), typeof(BillOfLading), new PropertyMetadata(false));

        public bool IsPrinting
        {
            get => (bool)GetValue(IsPrintingProperty);
            set => SetValue(IsPrintingProperty, value);
        }

        private void MarkCollectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsCollectChecked = true;
        }
    }
}
