using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;
using System.Data.Odbc;
using System.Runtime.InteropServices;

namespace Shipping_Form_CreatorV1.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ISqliteService _sqliteService;
    private readonly IOdbcService _odbcService;

    public MainViewModel(ISqliteService sqliteService, IOdbcService odbcService, UserGroupService userGroupService)
    {
        _sqliteService = sqliteService;
        _odbcService = odbcService;

        IsDittoUser = userGroupService.IsCurrentUserInDittoGroup();

        SelectedReport = new ReportModel { Header = new ReportHeader() };
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // -------------------------
    // Simple UI state
    // -------------------------
    private string _salesOrderNumber = string.Empty;

    public string SalesOrderNumber
    {
        get => _salesOrderNumber;
        set
        {
            if (_salesOrderNumber == value) return;
            _salesOrderNumber = value;
            OnPropertyChanged(nameof(SalesOrderNumber));
        }
    }

    private string _selectedReportTitle = "PACKING LIST";

    public string SelectedReportTitle
    {
        get => _selectedReportTitle;
        set
        {
            if (_selectedReportTitle == value) return;
            _selectedReportTitle = value;
            OnPropertyChanged(nameof(SelectedReportTitle));
            OnPropertyChanged(nameof(IsSaveEnabled));
        }
    }

    public bool IsSaveEnabled => SelectedReportTitle != "BILL OF LADING";


    private int _pageCount;

    public int PageCount
    {
        get => _pageCount;
        set
        {
            if (_pageCount == value) return;
            _pageCount = value;
            OnPropertyChanged(nameof(PageCount));
        }
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    private bool _isDittoUser;

    public bool IsDittoUser
    {
        get => _isDittoUser;
        set
        {
            if (_isDittoUser == value) return;
            _isDittoUser = value;
            OnPropertyChanged(nameof(IsDittoUser));
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set {
            if(_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }
    public string PrintProgress { get; set; } = "";


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
            .Where(detail => detail.BolFlag != null && detail.BolFlag.Equals("Y", StringComparison.InvariantCultureIgnoreCase)));

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

    public int AllPiecesTotal => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int AllWeightTotal => SelectedReportsGroups.Sum(r => r.TotalWeight);

    private void UpdateGroups()
    {
        var units = SelectedReport.LineItems.SelectMany(li => li.LineItemPackingUnits);

        var summary = units
            .GroupBy(u => new
            {
                TypeOfUnit = string.IsNullOrWhiteSpace(u.TypeOfUnit) ? "Unknown" : u.TypeOfUnit,
                CartonOrSkid = string.IsNullOrWhiteSpace(u.CartonOrSkid) ? "Unknown" : u.CartonOrSkid
            })
            .Select(g => new BolSummaryRow
            {
                TypeOfUnit = g.Key.TypeOfUnit,
                CartonOrSkid = g.Key.CartonOrSkid,

                // FIX: sum quantities, don’t count rows
                CartonCount = g.Where(x => string.Equals(x.CartonOrSkid, "Carton", StringComparison.InvariantCultureIgnoreCase))
                               .Sum(x => x.Quantity),
                SkidCount = g.Where(x => string.Equals(x.CartonOrSkid, "Skid", StringComparison.InvariantCultureIgnoreCase))
                               .Sum(x => x.Quantity),

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
                    var type when type == Constants.PackingUnitCategories[9] => "79300-05",
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

    public async Task SeedData()
    {
        var reports = await _odbcService.GetAllReportsAsync();
        foreach (var report in reports)
        {
            // check if this report already exists in sqlite
            var existing = await _sqliteService.GetReportAsync(report.Header.OrderNumber);

            if (existing == null) 
            {
                SelectedReport = report;
                await SaveCurrentReportAsync();
            }
            else
            {
                continue;
            }
        }
    }

    public async Task LoadDocumentAsync(string orderNumberInput, CancellationToken ct = default)
    {
        await Task.Yield();
        if (string.IsNullOrWhiteSpace(orderNumberInput)) return;


        if (!TryGetOrderNumber(orderNumberInput, out var orderNumber))
        {
            DialogService.ShowErrorDialog("Invalid Sales Order Number. Please enter a valid number.");
            return;
        }
        IsBusy = true;
        await Task.Delay(1, ct);
        try
        {
            var erpDocument = await _odbcService.GetReportAsync(orderNumber, ct);
            if (erpDocument is null)
            {
                DialogService.ShowErrorDialog($"Sales Order {orderNumber} does not have a packing list.");
                return;
            }

            var cachedDocument = await _sqliteService.GetReportAsync(orderNumber, ct);

            if (cachedDocument is not null)
            {
                erpDocument.Id = cachedDocument.Id;
                erpDocument.Header.Id = cachedDocument.Header.Id;
                erpDocument.Header.LogoImagePath = cachedDocument.Header.LogoImagePath;

                await Task.Run(() =>
                {
                    foreach (var erpLineItem in erpDocument.LineItems)
                    {
                        var cachedLineItem = cachedDocument.LineItems
                            .FirstOrDefault(li =>
                                li.LineItemHeader?.LineItemNumber == erpLineItem.LineItemHeader?.LineItemNumber);

                        if (cachedLineItem is null) continue;
                        erpLineItem.Id = cachedLineItem.Id;
                        erpLineItem.LineItemPackingUnits = cachedLineItem.LineItemPackingUnits;
                    }
                }, ct);

            }

            SelectedReport = erpDocument;
        }
        catch (OperationCanceledException)
        {
        }
        catch (OdbcException ex)
        {
            DialogService.ShowErrorDialog("ODBC connection error: " + ex.Message);
        }
        catch (SqliteException ex)
        {
            DialogService.ShowErrorDialog("Sqlite database error: " + ex.Message);
        }
        catch (Exception ex)
        {
            DialogService.ShowErrorDialog("An unexpected error occurred: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveCurrentReportAsync(CancellationToken ct = default)
    {
        await _sqliteService.SaveReportAsync(SelectedReport, ct);
    }
}