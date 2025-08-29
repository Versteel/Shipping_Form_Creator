using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.ViewModels;
using System.Threading.Tasks;
using System.Windows;

namespace Shipping_Form_CreatorV1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DialogService _dialogService;
    private readonly PrintService _printService;

    public MainWindow(MainViewModel viewModel, DialogService dialogService, PrintService printService)
    {
        _dialogService = dialogService;
        _printService = printService;
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        OrderNumberTextBox.Focus();
        ContentFrame.Content = new PackingListPage(viewModel);
    }

    private void GoToPackingListBtn_Click(object sender, RoutedEventArgs e)
    {
        OrderNumberTextBox.Clear();
        _viewModel.SalesOrderNumber = string.Empty;
        _viewModel.SelectedReportTitle = "PACKING LIST";
        _viewModel.SelectedReport = new Models.ReportModel { Header = new Models.ReportHeader() };
        ContentFrame.Content = new PackingListPage(_viewModel);
    }

    private void GoToBillOfLadingBtn_Click(object sender, RoutedEventArgs e)
    {
        OrderNumberTextBox.Clear();
        _viewModel.SalesOrderNumber = string.Empty;
        _viewModel.SelectedReportTitle = "BILL OF LADING";
        _viewModel.SelectedReport = new Models.ReportModel { Header = new Models.ReportHeader() };
        ContentFrame.Content = new BillOfLading(_viewModel);
    }

    private async void OrderNumberTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        try
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (OrderNumberIsValid(OrderNumberTextBox.Text.Trim()))
                {
                    try
                    {
                        await _viewModel.LoadDocumentAsync(OrderNumberTextBox.Text.Trim());
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowErrorDialog($"Error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowErrorDialog($"Error: {ex.Message}");
        }
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (OrderNumberIsValid(OrderNumberTextBox.Text.Trim()))
            {
                await _viewModel.LoadDocumentAsync(OrderNumberTextBox.Text.Trim());
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowErrorDialog($"Error: {ex.Message} Inner: {ex.InnerException?.Message}");
        }
    }

    private static bool OrderNumberIsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var isNumeric = int.TryParse(input, out _);
        var isValidLength = input.Length == 6;

        return isNumeric && isValidLength;
    }


    private async void SaveChangesBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.SaveCurrentReportAsync();
            MessageBox.Show("Report saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException ex)
        {
            var root = ex.GetBaseException();
            _dialogService.ShowErrorDialog($"Database update error:\n{root.Message}");
        }
        catch (Exception ex)
        {
            _dialogService.ShowErrorDialog($"Unexpected error:\n{ex.Message}");
        }
    }

    private void PrintBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if(_viewModel.SelectedReportTitle == "PACKING LIST")
        {
            var pages = _printService.BuildAllPackingListPages(_viewModel);
            _printService.PrintPackingListPages(pages);
        }
        if(_viewModel.SelectedReportTitle == "BILL OF LADING")
        {
            var page = _printService.BuildBillOfLadingPage(_viewModel);
            _printService.PrintBillOfLading(page);
        }
    }
}