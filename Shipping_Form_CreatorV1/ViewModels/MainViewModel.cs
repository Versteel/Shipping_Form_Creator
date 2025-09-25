using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel;
using System.Data.Odbc;
using Serilog;

namespace Shipping_Form_CreatorV1.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ISqliteService _sqliteService;
    private readonly IOdbcService _odbcService;

    public MainViewModel(ISqliteService sqliteService, IOdbcService odbcService)
    {
        _sqliteService = sqliteService;
        _odbcService = odbcService;
        IsDittoUser = UserGroupService.IsCurrentUserInDittoGroup();

        SearchByDateResults = [];
        SelectedReportsGroups = [];
        SearchByDateResults = Array.Empty<ReportModel>();
        SelectedReport = new ReportModel
        {
            Header = new ReportHeader(),
            LineItems = []
        };
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

    public bool IsSaveEnabled => SelectedReportTitle == "PACKING LIST";


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

    private ObservableCollection<int> _lineNumbersForDropdown;
    public ObservableCollection<int> LineNumbersForDropdown
    {
        get => _lineNumbersForDropdown;
        set
        {
            if (_lineNumbersForDropdown == value) return;
            _lineNumbersForDropdown = value;
            OnPropertyChanged(nameof(LineNumbersForDropdown));
        }
    }

    public string CurrentUser => Environment.UserName;

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
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }
    public string PrintProgress { get; set; } = "";
    private void UpdateLineNumbers()
    {
        {
            var numbers = SelectedReport.LineItems
                .Where(li => li.LineItemHeader?.LineItemNumber is < 900 && !string.IsNullOrWhiteSpace(li.LineItemHeader.ProductDescription))
                .Select(li =>
                {
                    if (li.LineItemHeader != null) return (int)li.LineItemHeader.LineItemNumber;
                    return 0;
                })
                .ToList();

            LineNumbersForDropdown = new ObservableCollection<int>(numbers);
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

            OnPropertyChanged(nameof(SelectedReport));
            OnPropertyChanged(nameof(BolSpecialInstructions));
            OnPropertyChanged(nameof(PackingListNotes));
            UpdateGroups();
            OnPropertyChanged(nameof(BolTotalPieces));
            OnPropertyChanged(nameof(BolTotalWeight));
            OnPropertyChanged(nameof(AllPiecesTotal));
            OnPropertyChanged(nameof(AllWeightTotal));

        }
    }


    private ICollection<ReportModel> _searchByDateResults;
    public ICollection<ReportModel> SearchByDateResults
    {
        get => _searchByDateResults;
        set
        {
            if (Equals(_searchByDateResults, value)) return;
            _searchByDateResults = value;
            OnPropertyChanged(nameof(SearchByDateResults));
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
        var items = SelectedReport?.LineItems ?? Enumerable.Empty<LineItem>();

        var units = items
            .SelectMany(li => li.LineItemPackingUnits ?? Enumerable.Empty<LineItemPackingUnit>())
            .Where(pu => !string.IsNullOrWhiteSpace(pu.TypeOfUnit))
            .ToList();

        var summary = units
            .GroupBy(u => new
            {
                TypeOfUnit = NormalizeUnitType(u.TypeOfUnit),
                CartonOrSkid = string.IsNullOrWhiteSpace(u.CartonOrSkid) ? "Unknown" : u.CartonOrSkid
            })
            .Select(g => new BolSummaryRow
            {
                TypeOfUnit = g.Key.TypeOfUnit,
                CartonOrSkid = g.Key.CartonOrSkid,
                CartonCount = g.Where(x => x.CartonOrSkid?.Equals("Box", StringComparison.InvariantCultureIgnoreCase) == true)
                               .Sum(x => x.Quantity),
                SkidCount = g.Where(x => x.CartonOrSkid?.Equals("Skid", StringComparison.InvariantCultureIgnoreCase) == true)
                               .Sum(x => x.Quantity),
                TotalPieces = g.Sum(x => x.Quantity),
                TotalWeight = g.Sum(x => x.Weight),
                Class = g.Key.TypeOfUnit switch
                {
                    var t when t == Constants.PackingUnitCategories[0] => "70",
                    var t when t == Constants.PackingUnitCategories[1] => "70",
                    var t when t == Constants.PackingUnitCategories[2] => "250",
                    var t when t == Constants.PackingUnitCategories[3] => "250",
                    var t when t == Constants.PackingUnitCategories[4] => "250",
                    var t when t == Constants.PackingUnitCategories[5] => "250",
                    var t when t == Constants.PackingUnitCategories[6] => "125",
                    var t when t == Constants.PackingUnitCategories[7] => "71",
                    var t when t == Constants.PackingUnitCategories[8] => "70",
                    var t when t == Constants.PackingUnitCategories[9] => "70",
                    var t when t == Constants.PackingUnitCategories[10] => "85",
                    var t when t == Constants.PackingUnitCategories[11] => "100",
                    var t when t == Constants.PackingUnitCategories[12] => "125",
                    _ => "0",
                },
                NMFC = g.Key.TypeOfUnit switch
                {
                    var t when t ==  Constants.PackingUnitCategories[0]  => "79300-09",
                    var t when t ==  Constants.PackingUnitCategories[1]  => "79300-09",
                    var t when t ==  Constants.PackingUnitCategories[2]  => "79300-03",
                    var t when t ==  Constants.PackingUnitCategories[3]  => "79300-03",
                    var t when t ==  Constants.PackingUnitCategories[4]  => "79300-03",
                    var t when t ==  Constants.PackingUnitCategories[5]  => "79300-03",
                    var t when t ==  Constants.PackingUnitCategories[6]  => "79300-05",
                    var t when t ==  Constants.PackingUnitCategories[7]  => "61680-01",
                    var t when t ==  Constants.PackingUnitCategories[8]  => "189035",
                    var t when t ==  Constants.PackingUnitCategories[9]  => "95190-09",
                    var t when t ==  Constants.PackingUnitCategories[10] => "83060",
                    var t when t ==  Constants.PackingUnitCategories[11] => "22260-06",
                    var t when t ==  Constants.PackingUnitCategories[12] => "79300-05",
                    _ => "000000-00",
                }
            })
            .OrderBy(r => r.TypeOfUnit)
            .ThenBy(r => r.CartonOrSkid);

        SelectedReportsGroups = new ObservableCollection<BolSummaryRow>(summary);
    }

    private static string NormalizeUnitType(string? typeOfUnit)
    {
        if (string.IsNullOrWhiteSpace(typeOfUnit))
            return "Unknown";

        return typeOfUnit.ToUpperInvariant() switch
        {
            "CHAIRS" or "CURVARE" or "IMMIX" or "OLLIE" => "CHAIRS",
            _ => typeOfUnit.ToUpperInvariant()
        };
    }



    private DateTime? _searchByDate = DateTime.Today.Date;
    public DateTime? SearchByDate
    {
        get => _searchByDate;
        set
        {
            if (_searchByDate == value) return;
            _searchByDate = value;
            OnPropertyChanged(nameof(SearchByDate));
            OnPropertyChanged(nameof(SearchDateString)); // notify UI that string also changed
        }
    }

    public string SearchDateString =>
        SearchByDate?.ToShortDateString() ?? string.Empty;



    public int BolTotalPieces => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int BolTotalWeight => SelectedReportsGroups.Sum(r => r.TotalWeight);

    // -------------------------
    // Commands / Ops
    // -------------------------
    private static bool TryGetOrderNumber(string input, out int orderNumber)
        => int.TryParse(input, out orderNumber);

    public async Task LoadDocumentAsync(string orderNumberInput, string suffix, CancellationToken ct = default)
    {
        Log.Information($"User {CurrentUser} is attempting to load Sales Order {orderNumberInput}-{suffix}");

        SelectedReport = new ReportModel
        {
            Header = new ReportHeader(),
            LineItems = []
        };
        await Task.Yield();
        if (string.IsNullOrWhiteSpace(orderNumberInput)) return;
        if (string.IsNullOrWhiteSpace(suffix)) suffix = "00";


        if (!TryGetOrderNumber(orderNumberInput, out var orderNumber))
        {
            DialogService.ShowErrorDialog("Invalid Sales Order Number. Please enter a valid number.");
            return;
        }
        if (!int.TryParse(suffix, out var suffixNumber) || suffixNumber < 0 || suffixNumber > 99)
        {
            DialogService.ShowErrorDialog("Invalid Suffix. Please enter a number between 00 and 99.");
            return;
        }
        IsBusy = true;
        await Task.Delay(1, ct);
        try
        {
            var erpDocument = await _odbcService.GetReportAsync(orderNumber, suffixNumber, ct);
            var cachedDocument = await _sqliteService.GetReportAsync(orderNumber, suffixNumber, ct);

            switch (erpDocument)
            {
                case null when cachedDocument is null:
                    DialogService.ShowErrorDialog(
                        $"Frontier has not created a packing list for Sales Order {orderNumber}.");
                    return;
                case null:
                    SelectedReport = cachedDocument!;
                    return;
            }

            if (cachedDocument is not null)
            {
                erpDocument.Id = cachedDocument.Id;
                erpDocument.Header.Id = cachedDocument.Header.Id;

                var erpHeader = erpDocument.Header;
                var cacheHeader = cachedDocument.Header;

                erpHeader.LogoImagePath = !string.IsNullOrWhiteSpace(erpHeader.LogoImagePath) ? erpHeader.LogoImagePath : cacheHeader.LogoImagePath;
                erpHeader.OrderNumber = erpHeader.OrderNumber != 0 ? erpHeader.OrderNumber : cacheHeader.OrderNumber;
                erpHeader.Suffix = erpHeader.Suffix != 0 ? erpHeader.Suffix : cacheHeader.Suffix;
                erpHeader.PageCount = erpHeader.PageCount != 0 ? erpHeader.PageCount : cacheHeader.PageCount;
                erpHeader.OrdEnterDate = !string.IsNullOrWhiteSpace(erpHeader.OrdEnterDate) ? erpHeader.OrdEnterDate : cacheHeader.OrdEnterDate;
                erpHeader.ShipDate = !string.IsNullOrWhiteSpace(erpHeader.ShipDate) ? erpHeader.ShipDate : cacheHeader.ShipDate;
                erpHeader.SoldToCustNumber = !string.IsNullOrWhiteSpace(erpHeader.SoldToCustNumber) ? erpHeader.SoldToCustNumber : cacheHeader.SoldToCustNumber;
                erpHeader.ShipToCustNumber = !string.IsNullOrWhiteSpace(erpHeader.ShipToCustNumber) ? erpHeader.ShipToCustNumber : cacheHeader.ShipToCustNumber;
                erpHeader.SoldToName = !string.IsNullOrWhiteSpace(erpHeader.SoldToName) ? erpHeader.SoldToName : cacheHeader.SoldToName;
                erpHeader.SoldToCustAddressLine1 = !string.IsNullOrWhiteSpace(erpHeader.SoldToCustAddressLine1) ? erpHeader.SoldToCustAddressLine1 : cacheHeader.SoldToCustAddressLine1;
                erpHeader.SoldToCustAddressLine2 = !string.IsNullOrWhiteSpace(erpHeader.SoldToCustAddressLine2) ? erpHeader.SoldToCustAddressLine2 : cacheHeader.SoldToCustAddressLine2;
                erpHeader.SoldToCustAddressLine3 = !string.IsNullOrWhiteSpace(erpHeader.SoldToCustAddressLine3) ? erpHeader.SoldToCustAddressLine3 : cacheHeader.SoldToCustAddressLine3;
                erpHeader.SoldToCity = !string.IsNullOrWhiteSpace(erpHeader.SoldToCity) ? erpHeader.SoldToCity : cacheHeader.SoldToCity;
                erpHeader.SoldToSt = !string.IsNullOrWhiteSpace(erpHeader.SoldToSt) ? erpHeader.SoldToSt : cacheHeader.SoldToSt;
                erpHeader.SoldToZipCode = !string.IsNullOrWhiteSpace(erpHeader.SoldToZipCode) ? erpHeader.SoldToZipCode : cacheHeader.SoldToZipCode;
                erpHeader.ShipToName = !string.IsNullOrWhiteSpace(erpHeader.ShipToName) ? erpHeader.ShipToName : cacheHeader.ShipToName;
                erpHeader.ShipToCustAddressLine1 = !string.IsNullOrWhiteSpace(erpHeader.ShipToCustAddressLine1) ? erpHeader.ShipToCustAddressLine1 : cacheHeader.ShipToCustAddressLine1;
                erpHeader.ShipToCustAddressLine2 = !string.IsNullOrWhiteSpace(erpHeader.ShipToCustAddressLine2) ? erpHeader.ShipToCustAddressLine2 : cacheHeader.ShipToCustAddressLine2;
                erpHeader.ShipToCustAddressLine3 = !string.IsNullOrWhiteSpace(erpHeader.ShipToCustAddressLine3) ? erpHeader.ShipToCustAddressLine3 : cacheHeader.ShipToCustAddressLine3;
                erpHeader.ShipToCity = !string.IsNullOrWhiteSpace(erpHeader.ShipToCity) ? erpHeader.ShipToCity : cacheHeader.ShipToCity;
                erpHeader.ShipToSt = !string.IsNullOrWhiteSpace(erpHeader.ShipToSt) ? erpHeader.ShipToSt : cacheHeader.ShipToSt;
                erpHeader.ShipToZipCode = !string.IsNullOrWhiteSpace(erpHeader.ShipToZipCode) ? erpHeader.ShipToZipCode : cacheHeader.ShipToZipCode;
                erpHeader.CustomerPONumber = !string.IsNullOrWhiteSpace(erpHeader.CustomerPONumber) ? erpHeader.CustomerPONumber : cacheHeader.CustomerPONumber;
                erpHeader.DueDate = !string.IsNullOrWhiteSpace(erpHeader.DueDate) ? erpHeader.DueDate : cacheHeader.DueDate;
                erpHeader.SalesPerson = !string.IsNullOrWhiteSpace(erpHeader.SalesPerson) ? erpHeader.SalesPerson : cacheHeader.SalesPerson;
                erpHeader.CarrierName = !string.IsNullOrWhiteSpace(erpHeader.CarrierName) ? erpHeader.CarrierName : cacheHeader.CarrierName;
                erpHeader.TrackingNumber = !string.IsNullOrWhiteSpace(erpHeader.TrackingNumber) ? erpHeader.TrackingNumber : cacheHeader.TrackingNumber;
                if (string.IsNullOrWhiteSpace(erpHeader.FreightTerms))
                {
                    erpHeader.FreightTerms = cacheHeader.FreightTerms;
                }

                foreach (var erpLine in erpDocument.LineItems)
                {
                    var cachedLine = cachedDocument.LineItems
                        .FirstOrDefault(li =>
                            li.LineItemHeader?.LineItemNumber == erpLine.LineItemHeader?.LineItemNumber);

                    if (cachedLine is null) continue;

                    erpLine.Id = cachedLine.Id;

                    if (erpLine.LineItemPackingUnits.Count == 0)
                        erpLine.LineItemPackingUnits = cachedLine.LineItemPackingUnits;
                }
            }

            SelectedReport = erpDocument;
            UpdateLineNumbers();
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
            Log.Information($"User {CurrentUser} finished loading Sales Order {orderNumber}-{suffix}");
            IsBusy = false;
        }
    }

    public async Task GetSearchByDateResults(DateTime date, CancellationToken ct = default)
    {
        Log.Information($"User {CurrentUser} is searching for shipped orders on {date:d}");
        SelectedReportTitle = "SEARCH RESULTS";
        try
        {
            IsBusy = true;
            await Task.Delay(1, ct);

            var orders = await _odbcService.GetShippedOrdersByDate(date.Date, ct);

            var fullReports = new List<ReportModel>();

            foreach (var order in orders)
            {
                var orderNumber = order.Header.OrderNumber;
                var suffix = order.Header.Suffix;

                var report = await _odbcService.GetReportAsync(orderNumber, suffix, ct);
                if (report == null) continue;
                var cached = await _sqliteService.GetReportAsync(orderNumber, suffix, ct);
                if (cached != null)
                {
                    report.Id = cached.Id;
                }

                fullReports.Add(report);
            }

            SearchByDateResults = fullReports;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}. Inner Exception {ex.InnerException}");
        }
        finally
        {
            Log.Information($"User {CurrentUser} finished searching for shipped orders on {date:d}");
            IsBusy = false;
        }
    }


    public async Task SaveCurrentReportAsync(CancellationToken ct = default)
    {
        await _sqliteService.SaveReportAsync(SelectedReport, ct);
    }
}