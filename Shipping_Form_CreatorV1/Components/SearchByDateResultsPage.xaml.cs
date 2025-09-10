using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.ViewModels;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class SearchByDateResultsPage : Page
    {
        private readonly MainViewModel _vm;

        public static readonly RoutedUICommand OpenPackingListCommand =
            new("Open Packing List", nameof(OpenPackingListCommand), typeof(SearchByDateResultsPage));

        public static readonly RoutedUICommand OpenBolCommand =
            new("Open Bill of Lading", nameof(OpenBolCommand), typeof(SearchByDateResultsPage));

        public SearchByDateResultsPage(MainViewModel viewModel)
        {
            _vm = viewModel;
            DataContext = _vm;
            InitializeComponent();
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row) row.IsSelected = true;
        }

        private void SearchResultsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (searchResultsDataGrid.SelectedItem is ReportModel report)
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(report, "BILL OF LADING");
        }

        private async void OpenPackingList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ReportModel report)
            {
                await _vm.LoadDocumentAsync(report.Header.OrderNumber.ToString(), report.Header.Suffix.ToString());
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(_vm.SelectedReport, "PACKING LIST");
            }
        }

        private async void OpenBol_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ReportModel report)
            {
                await _vm.LoadDocumentAsync(report.Header.OrderNumber.ToString(), report.Header.Suffix.ToString());
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(_vm.SelectedReport, "BILL OF LADING");
            }
        }
    }
}
