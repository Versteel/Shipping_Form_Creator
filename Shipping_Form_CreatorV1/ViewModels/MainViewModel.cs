using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Shipping_Form_CreatorV1.Components.Dialogs;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.Utilites;
using System.ComponentModel;
using System.Data.Odbc;
using System.Linq;

namespace Shipping_Form_CreatorV1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
       

        private readonly ISqliteService _sqliteService;
        private readonly IOdbcService _odbcService;
        private readonly DialogService _dialogService;

        public MainViewModel(ISqliteService sqliteService, IOdbcService odbcService, UserGroupService userGroupService, DialogService dialogService)
        {
            _sqliteService = sqliteService;
            _odbcService = odbcService;
            _dialogService = dialogService;

            IsDittoUser = userGroupService.IsCurrentUserInDittoGroup();

            // Initialize a safe default to avoid NREs
            SelectedReport = new ReportModel { Header = new ReportHeader() };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -------------------------
        // Simple UI state
        // -------------------------
        private string _salesOrderNumber = string.Empty;
        public string SalesOrderNumber
        {
            get => _salesOrderNumber;
            set { if (_salesOrderNumber != value) { _salesOrderNumber = value; OnPropertyChanged(nameof(SalesOrderNumber)); } }
        }

        private string _selectedReportTitle = "PACKING LIST";
        public string SelectedReportTitle
        {
            get => _selectedReportTitle;
            set { if (_selectedReportTitle != value) { _selectedReportTitle = value; OnPropertyChanged(nameof(SelectedReportTitle)); } }
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set { if (_pageCount != value) { _pageCount = value; OnPropertyChanged(nameof(PageCount)); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); } }
        }

        private bool _isDittoUser;
        public bool IsDittoUser
        {
            get => _isDittoUser;
            set
            {
                if (_isDittoUser == value) return;
                _isDittoUser = value; OnPropertyChanged(nameof(IsDittoUser));
            }
        }

        // -------------------------
        // Core data
        // -------------------------
        private ReportModel _selectedReport = new() { Header = new ReportHeader() };
        public ReportModel SelectedReport
        {
            get => _selectedReport;
            set
            {
                if (_selectedReport == value) return;
                _selectedReport = value;

                // Update other bindings as before
                OnPropertyChanged(nameof(SelectedReport));
                OnPropertyChanged(nameof(ReportHeader));
                OnPropertyChanged(nameof(LineItemHeaders));
                OnPropertyChanged(nameof(LineItemDetails));
                OnPropertyChanged(nameof(PackingListSpecialInstructions));
            }
        }

        // -------------------------
        // BuildPages-facing helpers
        // -------------------------
        public ReportHeader ReportHeader => SelectedReport.Header;

        public List<LineItemHeader?> LineItemHeaders =>
            SelectedReport?.LineItems?
                .Where(_ => true)
                .Select(li => li.LineItemHeader)
                .OrderBy(h => h.LineItemNumber)
                .ToList()
            ?? [];

        public List<LineItemDetail> LineItemDetails =>
            SelectedReport?.LineItems?
                .SelectMany(li => li.LineItemDetails ?? Enumerable.Empty<LineItemDetail>())
                .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
                .OrderBy(d => d.ModelItem)
                .ThenBy(d => d.NoteSequenceNumber)
                .ToList()
            ?? [];

        // “Trailer notes” for the final page:
        // all Packing List notes whose ModelItem doesn’t correspond to any LineItem header (header/trailer notes in FRENOT)
        public ObservableCollection<LineItemDetail>? PackingListSpecialInstructions =>
            [.. LineItemDetails
                .Where(d => string.Equals(d.PackingListFlag?.Trim(), "Y", StringComparison.OrdinalIgnoreCase))
                .Where(d => LineItemHeaders.All(h => h.LineItemNumber != d.ModelItem))
                .OrderBy(d => d.NoteSequenceNumber)];

        public ObservableCollection<LineItemDetail>? PackingListNotes { get; set; }

        // -------------------------
        // Commands / Ops
        // -------------------------
        private static bool TryGetOrderNumber(string input, out int orderNumber)
            => int.TryParse(input, out orderNumber);

        public async Task LoadDocumentAsync(string orderNumberInput, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumberInput)) return;

            IsLoading = true;

            if (!TryGetOrderNumber(orderNumberInput, out var orderNumber))
            {
                _dialogService.ShowErrorDialog("Invalid Sales Order Number. Please enter a valid number.");
                return;
            }
            await Task.Delay(1, ct);
            try
            {
                var cachedDocument = await _sqliteService.GetReportAsync(orderNumber, ct);
                if (cachedDocument is not null)
                {
                    SelectedReport = cachedDocument;
                    return;
                }

                var erpDocument = await _odbcService.GetReportAsync(orderNumber, ct);
                if (erpDocument is null)
                {
                    _dialogService.ShowErrorDialog($"Sales Order {orderNumber} does not have a packing list.");
                    return;
                }
                SelectedReport = erpDocument;
            }
            catch (OperationCanceledException)
            {
                // ignore (user canceled)
            }
            catch (OdbcException ex)
            {
                // Prefer an injected dialog service; keeping your current UX for now.
                _dialogService.ShowErrorDialog("ODBC connection error: " + ex.Message);
            }
            catch (SqliteException ex)
            {
                _dialogService.ShowErrorDialog("Sqlite database error: " + ex.Message);
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorDialog("An unexpected error occurred: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SaveCurrentReportAsync(CancellationToken ct = default)
        {
            await _sqliteService.SaveReportAsync(SelectedReport, ct);
        }


    }
}
