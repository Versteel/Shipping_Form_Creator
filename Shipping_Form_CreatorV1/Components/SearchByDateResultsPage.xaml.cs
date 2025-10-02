using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.ViewModels;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class SearchByDateResultsPage
    {
        private readonly MainViewModel _vm;
        private readonly PrintService _printService;

        public static readonly RoutedUICommand OpenPackingListCommand =
            new("Open Packing List", nameof(OpenPackingListCommand), typeof(SearchByDateResultsPage));

        public static readonly RoutedUICommand OpenBolCommand =
            new("Open Bill of Lading", nameof(OpenBolCommand), typeof(SearchByDateResultsPage));

        public static readonly RoutedUICommand ProcessAllDocumentsCommand =
            new("Process All Documents", nameof(ProcessAllDocumentsCommand), typeof(SearchByDateResultsPage));

        public SearchByDateResultsPage(MainViewModel viewModel, PrintService printService)
        {
            _vm = viewModel;
            _printService = printService;
            DataContext = _vm;
            InitializeComponent();
        }

        private void SearchResultsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (searchResultsDataGrid.SelectedItem is ReportModel report)
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(report, "BILL OF LADING");
        }

        private async void OpenPackingList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.Parameter is not ReportModel report) return;
                await _vm.LoadDocumentAsync(report.Header.OrderNumber.ToString(), report.Header.Suffix.ToString());
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(_vm.SelectedReport, "PACKING LIST");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OpenBol_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.Parameter is not ReportModel report) return;
                await _vm.LoadDocumentAsync(report.Header.OrderNumber.ToString(), report.Header.Suffix.ToString());
                (Window.GetWindow(this) as MainWindow)?.NavigateToReport(_vm.SelectedReport, "BILL OF LADING");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ProcessAllDocuments_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                await _printService.ConvertSearchResultsToPdf(_vm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing documents: {ex.Message}");
                throw;
            }
        }
    }
}
