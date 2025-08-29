using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Shipping_Form_CreatorV1.Components.Dialogs;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.Utilities;
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

            SelectedReport = new ReportModel { Header = new ReportHeader() };
        }

        public sealed class BolSummaryRow
        {
            public string? TypeOfUnit { get; init; }
            public string? CartonOrSkid { get; init; }
            public int TotalPieces { get; init; }
            public int TotalWeight { get; init; }

            public int CartonCount { get; set; }
            public int SkidCount { get; set; }
            public string Class { get; set; }
            public string NMFC { get; set; }
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
            set { 
                if (_selectedReportTitle != value) 
                { _selectedReportTitle = value; 
                    OnPropertyChanged(nameof(SelectedReportTitle));
                    OnPropertyChanged(nameof(IsSaveEnabled));
                } 
            }
        }
        public bool IsSaveEnabled => SelectedReportTitle != "BILL OF LADING";


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
        private ReportModel _selectedReport;
        public ReportModel SelectedReport
        {
            get => _selectedReport;
            set
            {
                if (_selectedReport == value) return;
                _selectedReport = value;
                UpdateGroups();
                OnPropertyChanged(nameof(SelectedReport));
                OnPropertyChanged(nameof(BolSpecialInstructions));
                OnPropertyChanged(nameof(PackingListNotes));
            }
        }

        public ObservableCollection<LineItemDetail> BolSpecialInstructions =>
            new(SelectedReport.LineItems
                .SelectMany(li => li.LineItemDetails)
                .Where(detail => detail.BolFlag.Equals("Y", StringComparison.InvariantCultureIgnoreCase)));

        public ObservableCollection<LineItemDetail>? PackingListNotes { get; set; }

        private ObservableCollection<BolSummaryRow> _selectedReportsGroups;
        public ObservableCollection<BolSummaryRow> SelectedReportsGroups
        {
            get => _selectedReportsGroups;
            set
            {
                _selectedReportsGroups = value;
                OnPropertyChanged(nameof(SelectedReportsGroups));
                OnPropertyChanged(nameof(BolTotalPieces));
                OnPropertyChanged(nameof(BolTotalWeight));
                OnPropertyChanged(nameof(AllPiecesTotal)); 
                OnPropertyChanged(nameof(AllWeightTotal));
            }
        }
        public int AllPiecesTotal => SelectedReportsGroups?.Sum(r => r.TotalPieces) ?? 0;
        public int AllWeightTotal => SelectedReportsGroups?.Sum(r => r.TotalWeight) ?? 0;

        private void UpdateGroups()
        {
            var units = SelectedReport?.LineItems?.SelectMany(li => li.LineItemPackingUnits)
                        ?? [];

            var summary = units
                .GroupBy(u => new {
                    TypeOfUnit = string.IsNullOrWhiteSpace(u.TypeOfUnit) ? "Unknown" : u.TypeOfUnit,
                    CartonOrSkid = string.IsNullOrWhiteSpace(u.CartonOrSkid) ? "Unknown" : u.CartonOrSkid
                })
                .Select(g => new MainViewModel.BolSummaryRow
                {
                    TypeOfUnit = g.Key.TypeOfUnit,
                    CartonOrSkid = g.Key.CartonOrSkid,
                    CartonCount = g.Count(x => x.CartonOrSkid.Equals("Carton", StringComparison.InvariantCultureIgnoreCase)),
                    SkidCount = g.Count(x => x.CartonOrSkid.Equals("Skid", StringComparison.InvariantCultureIgnoreCase)),
                    TotalPieces = g.Sum(x => x.Quantity),
                    TotalWeight = g.Sum(x => x.Weight),
                    Class = g.Key.TypeOfUnit switch
                    {
                        var type when type == Constants.PackingUnitCategories[0] => "70",
                        var type when type == Constants.PackingUnitCategories[1] => "70",
                        var type when type == Constants.PackingUnitCategories[2] => "250",
                        var type when type == Constants.PackingUnitCategories[3] => "125",
                        var type when type == Constants.PackingUnitCategories[4] => "71",
                        var type when type == Constants.PackingUnitCategories[5] => "70",
                        var type when type == Constants.PackingUnitCategories[6] => "70",
                        var type when type == Constants.PackingUnitCategories[7] => "85",
                        var type when type == Constants.PackingUnitCategories[8] => "100",
                        var type when type == Constants.PackingUnitCategories[9] => "125",
                        _ => "0",
                    },
                    NMFC = g.Key.TypeOfUnit switch
                    {
                        var type when type == Constants.PackingUnitCategories[0] => "79300-09",
                        var type when type == Constants.PackingUnitCategories[1] => "79300-09",
                        var type when type == Constants.PackingUnitCategories[2] => "79300-03",
                        var type when type == Constants.PackingUnitCategories[3] => "79300-05",
                        var type when type == Constants.PackingUnitCategories[4] => "61680-01",
                        var type when type == Constants.PackingUnitCategories[5] => "189035",
                        var type when type == Constants.PackingUnitCategories[6] => "95190-09",
                        var type when type == Constants.PackingUnitCategories[7] => "83060",
                        var type when type == Constants.PackingUnitCategories[8] => "22260-06",
                        var type when type == Constants.PackingUnitCategories[9] => "793000-05",
                        _ => "000000-00",
                    }
                })
                .OrderBy(r => r.TypeOfUnit)
                .ThenBy(r => r.CartonOrSkid);

            SelectedReportsGroups = new ObservableCollection<BolSummaryRow>(summary);
        }


        public int BolTotalPieces => SelectedReportsGroups.Sum(r => r.TotalPieces);
        public int BolTotalWeight => SelectedReportsGroups.Sum(r => r.TotalWeight);

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
