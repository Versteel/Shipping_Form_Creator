using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly MainViewModel _viewModel;
    private readonly PrintService _printService;

    public MainWindow(MainViewModel viewModel, PrintService printService)
    {
        _printService = printService;
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        OrderNumberTextBox.Focus();
        ContentFrame.Content = new PackingListPage(viewModel);
    }

    private void GoToPackingListBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmAndProceedWithNavigation()) return;

        OrderNumberTextBox.Clear();
        SuffixIntegerBox.Value = 0;
        _viewModel.SalesOrderNumber = string.Empty;
        _viewModel.SelectedReportTitle = "PACKING LIST";
        _viewModel.SelectedReportView = Constants.ViewOptions[0];
        _viewModel.SelectedReport = new ReportModel { Header = new ReportHeader() };
        ContentFrame.Content = new PackingListPage(_viewModel);
    }

    private void GoToBillOfLadingBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmAndProceedWithNavigation()) return;

        OrderNumberTextBox.Clear();
        SuffixIntegerBox.Value = 0;
        _viewModel.SalesOrderNumber = string.Empty;
        _viewModel.SelectedReportTitle = "BILL OF LADING";
        _viewModel.SelectedReportView = Constants.ViewOptions[0];
        _viewModel.SelectedReport = new ReportModel { Header = new ReportHeader() };
        ContentFrame.Content = new BillOfLading(_viewModel);
    }

    private async void OrderNumberTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (!OrderNumberIsValid(OrderNumberTextBox.Text.Trim())) return;
            if (e.Key != Key.Enter) return;

            if (!ConfirmAndProceedWithNavigation()) return;

            _viewModel.SelectedReportView = Constants.ViewOptions[0];
            await _viewModel.LoadDocumentAsync(OrderNumberTextBox.Text.Trim(), SuffixIntegerBox.Text.Trim());
            if (_viewModel.SelectedReportTitle == "SEARCH RESULTS")
            {
                _viewModel.SelectedReportTitle = "PACKING LIST";

                ContentFrame.Content = new PackingListPage(_viewModel);
            }

            if (_viewModel.SelectedReportTitle == "BILL OF LADING")
            {
                ContentFrame.Content = new BillOfLading(_viewModel);
            }
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorDialog($"Error: {ex.Message}");
        }
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ConfirmAndProceedWithNavigation()) return;

            _viewModel.SelectedReportView = Constants.ViewOptions[0];
            await _viewModel.LoadDocumentAsync(OrderNumberTextBox.Text.Trim(), SuffixIntegerBox.Text.Trim());
            if (_viewModel.SelectedReportTitle == "SEARCH RESULTS")
            {
                _viewModel.SelectedReportTitle = "PACKING LIST";

                ContentFrame.Content = new PackingListPage(_viewModel);
            }

            if (_viewModel.SelectedReportTitle == "BILL OF LADING")
            {
                ContentFrame.Content = new BillOfLading(_viewModel);
            }
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorDialog($"Error: {ex.Message} Inner: {ex.InnerException?.Message}");
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
            DialogService.ShowErrorDialog($"Database update error:\n{root.Message}");
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorDialog($"Unexpected error:\n{ex.Message}");
        }
    }

    private async void PrintBtn_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            switch (_viewModel.SelectedReportTitle)
            {
                case "PACKING LIST":
                    {
                        var pages = await _printService.BuildAllPackingListPages(_viewModel);
                        await _printService.PrintPackingListPages(pages);
                        break;
                    }
                case "BILL OF LADING":
                {
                        if (ContentFrame.Content is BillOfLading bolPage)
                        {
                            // 1. Get the current status from the screen the user is looking at
                            bool isCurrentlyChecked = bolPage.MarkCollectCheckBox.IsChecked ?? false;

                            // 2. Build the separate page for printing
                            var printInstance = _printService.BuildBillOfLadingPage(_viewModel);

                            // 3. Transfer the state to the print instance
                            printInstance.IsPrinting = true;
                            printInstance.ShowCollectText = isCurrentlyChecked;
                            printInstance.IsCollectChecked = isCurrentlyChecked;
                            await _printService.PrintBillOfLadingAsync(printInstance);
                        }
                        break;
                    }
                default:
                    MessageBox.Show($"Unknown report type: {_viewModel.SelectedReportTitle}",
                        "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle specific printing/UI related errors
            MessageBox.Show($"Print operation failed: {ex.Message}",
                "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle access/permission errors
            MessageBox.Show($"Access denied: {ex.Message}\n\nPlease check printer permissions.",
                "Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (OutOfMemoryException)
        {
            // Handle memory issues (large documents)
            MessageBox.Show("Insufficient memory to complete the print operation. Try closing other applications.",
                "Memory Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (TaskCanceledException)
        {
            // Handle timeout or cancellation
            MessageBox.Show("Print operation was cancelled or timed out.",
                "Operation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Handle any other unexpected errors
            MessageBox.Show(
                $"An unexpected error occurred while printing:\n\n{ex.Message}\n\nDetails: {ex.GetType().Name}",
                "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Log the full exception for debugging
            System.Diagnostics.Debug.WriteLine($"Print error: {ex}");
        }
    }

    private async void SearchByDateBtn_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ConfirmAndProceedWithNavigation()) return;

            if (_viewModel.SearchByDate is null)
            {
                DialogService.ShowErrorDialog("Please choose a ship date.");
                return;
            }

            _viewModel.SelectedReportView = Constants.ViewOptions[0];
            await _viewModel.GetSearchByDateResults(_viewModel.SearchByDate.Value);
            ContentFrame.Content = new SearchByDateResultsPage(_viewModel, _printService);
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorDialog($"Error: {ex.Message}");
        }
    }

    public void NavigateToReport(ReportModel report, string target)
    {
        if (!ConfirmAndProceedWithNavigation()) return;

        _viewModel.SelectedReport = report;
        _viewModel.SelectedReportTitle = target;
        _viewModel.SelectedReportView = Constants.ViewOptions[0];
        if (target == "PACKING LIST")
            ContentFrame.Content = new PackingListPage(_viewModel);
        else
            ContentFrame.Content = new BillOfLading(_viewModel);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them before exiting?\n\n" +
                "Click 'Yes' to return to the app and save.\n" +
                "Click 'No' to discard changes and exit.",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Cancel the close so the user can click the Save button
                // (Handling async saving directly inside OnClosing is complex/risky)
                e.Cancel = true;
            }
            else if (result == MessageBoxResult.Cancel)
            {
                // Just stay in the app
                e.Cancel = true;
            }
            // If 'No', do nothing and allow the app to close
        }

        base.OnClosing(e);
    }

    private bool ConfirmAndProceedWithNavigation()
    {
        if (!_viewModel.HasUnsavedChanges) return true;

        var result = MessageBox.Show(
            "You have unsaved changes that will be lost. Do you want to discard them and continue?",
            "Unsaved Changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.HasUnsavedChanges = false; // Reset the flag
            return true; // Proceed with navigation
        }

        return false; // Stay on the current page
    }
}
