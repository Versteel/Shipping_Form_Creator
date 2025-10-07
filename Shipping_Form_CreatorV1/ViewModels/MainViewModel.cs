using Microsoft.Data.Sqlite;
using Serilog;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shipping_Form_CreatorV1.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // -------------------------
    // Services
    // -------------------------
    private readonly ISqliteService _sqliteService;
    private readonly IOdbcService _odbcService;

    // -------------------------
    // Constructor
    // -------------------------
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

        Trucks = new ObservableCollection<string>(GenerateTruckList(1));
        _selectedTruck = Trucks.First();
        UpdateViewOptions();
    }

    // -------------------------
    // INotifyPropertyChanged Implementation
    // -------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // -------------------------
    // Core Data Properties
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
            UpdateViewOptions();
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

    // -------------------------
    // UI State & Input Properties
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
            OnPropertyChanged(nameof(IsPrintAvailable));
        }
    }

    private string _selectedReportView = "ALL";
    public string SelectedReportView
    {
        get => _selectedReportView;
        set
        {
            if (_selectedReportView == value) return;
            _selectedReportView = value;
            OnPropertyChanged(nameof(SelectedReportView));
            UpdateGroups();
        }
    }

    private ObservableCollection<string> _viewOptions = [];
    public ObservableCollection<string> ViewOptions
    {
        get => _viewOptions;
        set
        {
            if (_viewOptions == value) return;
            _viewOptions = value;
            OnPropertyChanged(nameof(ViewOptions));
        }
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
            OnPropertyChanged(nameof(SearchDateString));
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

    public string PrintProgress { get; set; } = "";

    // ------------------------------------
    //  Dynamic Truck Dropdown Properties
    // ------------------------------------
    public ObservableCollection<string> Trucks { get; set; }

    private string _selectedTruck;
    public string SelectedTruck
    {
        get => _selectedTruck;
        set
        {
            if (_selectedTruck == value) return;

            _selectedTruck = value;
            OnPropertyChanged(nameof(SelectedTruck));
            UpdateTruckList();
            //UpdateViewOptions();
        }
    }

    // -------------------------
    // Computed & Derived Properties
    // -------------------------
    public bool IsSaveEnabled => SelectedReportTitle == "PACKING LIST";
    public bool IsPrintAvailable => SelectedReportTitle != "SEARCH RESULTS";
    public string CurrentUser => Environment.UserName;
    //public static string[] ViewOptions => Constants.ViewOptions;
    public string SearchDateString => SearchByDate?.ToShortDateString() ?? string.Empty;
    public int BolTotalPieces => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int BolTotalWeight => SelectedReportsGroups.Sum(r => r.TotalWeight);
    public int AllPiecesTotal => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int AllWeightTotal => SelectedReportsGroups.Sum(r => r.TotalWeight);

    public ObservableCollection<LineItemDetail> BolSpecialInstructions =>
        new(SelectedReport.LineItems
            .SelectMany(li => li.LineItemDetails)
            .Where(detail => detail.BolFlag != null && detail.BolFlag.Equals("Y", StringComparison.InvariantCultureIgnoreCase)));

    public ObservableCollection<LineItemDetail>? PackingListNotes { get; set; }

    // -------------------------
    // Public Methods / Commands
    // -------------------------
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
                        $"Frontier has not created a packing list for Sales Order {orderNumber}-{suffix}.");
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
        catch (OperationCanceledException) { }
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
                    report.Header.Id = cached.Header.Id;

                    foreach (var erpLine in report.LineItems)
                    {
                        var cachedLine = cached.LineItems.FirstOrDefault(li =>
                            li.LineItemHeader?.LineItemNumber == erpLine.LineItemHeader?.LineItemNumber);
                        if (cachedLine == null) continue;
                        erpLine.Id = cachedLine.Id;

                        if (erpLine.LineItemPackingUnits.Count == 0)
                        {
                            erpLine.LineItemPackingUnits = cachedLine.LineItemPackingUnits;
                        }
                    }
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

    public async Task GetSearchByOrderNumberResults(int orderNumber, CancellationToken ct = default)
    {
        Log.Information($"User {CurrentUser} is searching for Sales Order {orderNumber}");
        SelectedReportTitle = "SEARCH RESULTS";
        try
        {
            IsBusy = true;
            await Task.Delay(1, ct);

            var orders = await _odbcService.GetOrdersByOrderNumber(orderNumber, ct);
            var fullReports = new List<ReportModel>();

            foreach (var order in orders)
            {
                var orderNum = order.Header.OrderNumber;
                var suffix = order.Header.Suffix;
                var report = await _odbcService.GetReportAsync(orderNum, suffix, ct);
                if (report == null) continue;
                var cached = await _sqliteService.GetReportAsync(orderNum, suffix, ct);

                if (cached == null) continue;
                report.Id = cached.Id;
                report.Header.Id = cached.Header.Id;

                foreach (var erpLine in report.LineItems)
                {
                    var cachedLine = cached.LineItems.FirstOrDefault(li =>
                        li.LineItemHeader?.LineItemNumber == erpLine.LineItemHeader?.LineItemNumber);
                    if (cachedLine == null) continue;
                    erpLine.Id = cachedLine.Id;

                    if (erpLine.LineItemPackingUnits.Count == 0)
                    {
                        erpLine.LineItemPackingUnits = cachedLine.LineItemPackingUnits;
                    }
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
            Log.Information($"User {CurrentUser} finished searching for Sales Order {orderNumber}");
            IsBusy = false;
        }
    }

    public async Task SaveCurrentReportAsync(CancellationToken ct = default)
    {
        await _sqliteService.SaveReportAsync(SelectedReport, ct);
        UpdateViewOptions();
    }

    // -------------------------
    // Private Helper Methods
    // -------------------------
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

    private void UpdateGroups()
    {
        var items = SelectedReport?.LineItems ?? Enumerable.Empty<LineItem>();
        var selectedView = SelectedReportView;
        var isAllView = selectedView == "ALL";

        var units = items
            .SelectMany(li => li.LineItemPackingUnits ?? Enumerable.Empty<LineItemPackingUnit>())
            .Where(pu => !string.IsNullOrWhiteSpace(pu.TypeOfUnit))
            .Where(pu => isAllView || string.Equals(pu.TruckNumber, selectedView, StringComparison.OrdinalIgnoreCase))
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
                CartonCount = g.Where(x => x.CartonOrSkid?.Equals("BOX", StringComparison.InvariantCultureIgnoreCase) == true
                                           || x.CartonOrSkid?.Equals("SINGLE PACK") == true)
                    .Sum(x => x.Quantity),
                SkidCount = g.Where(x => x.CartonOrSkid?.Equals("SKID", StringComparison.InvariantCultureIgnoreCase) == true)
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
                    var t when t == Constants.PackingUnitCategories[6] => "250",
                    var t when t == Constants.PackingUnitCategories[7] => "125",
                    var t when t == Constants.PackingUnitCategories[8] => "71",
                    var t when t == Constants.PackingUnitCategories[9] => "70",
                    var t when t == Constants.PackingUnitCategories[10] => "70",
                    var t when t == Constants.PackingUnitCategories[11] => "70",
                    var t when t == Constants.PackingUnitCategories[12] => "70",
                    var t when t == Constants.PackingUnitCategories[13] => "85",
                    var t when t == Constants.PackingUnitCategories[14] => "100",
                    var t when t == Constants.PackingUnitCategories[15] => "125",
                    _ => "0",
                },
                NMFC = g.Key.TypeOfUnit switch
                {
                    var t when t == Constants.PackingUnitCategories[0] => "79300-09",
                    var t when t == Constants.PackingUnitCategories[1] => "79300-09",
                    var t when t == Constants.PackingUnitCategories[2] => "79300-03",
                    var t when t == Constants.PackingUnitCategories[3] => "79300-03",
                    var t when t == Constants.PackingUnitCategories[4] => "79300-03",
                    var t when t == Constants.PackingUnitCategories[5] => "79300-03",
                    var t when t == Constants.PackingUnitCategories[6] => "79300-05",
                    var t when t == Constants.PackingUnitCategories[7] => "79300-05",
                    var t when t == Constants.PackingUnitCategories[8] => "61680-01",
                    var t when t == Constants.PackingUnitCategories[9] => "189035",
                    var t when t == Constants.PackingUnitCategories[10] => "95190-09",
                    var t when t == Constants.PackingUnitCategories[11] => "95190-09",
                    var t when t == Constants.PackingUnitCategories[12] => "95190-09",
                    var t when t == Constants.PackingUnitCategories[13] => "83060",
                    var t when t == Constants.PackingUnitCategories[14] => "22260-06",
                    var t when t == Constants.PackingUnitCategories[15] => "79300-05",
                    _ => "000000-00",
                }
            })
            .OrderBy(r => r.TypeOfUnit)
            .ThenBy(r => r.CartonOrSkid);

        SelectedReportsGroups = new ObservableCollection<BolSummaryRow>(summary);
    }

    private void UpdateTruckList()
    {
        if (string.IsNullOrEmpty(SelectedTruck)) return;

        string numberPart = SelectedTruck.Split(' ').Last();
        if (int.TryParse(numberPart, out int selectedNumber))
        {
            var newTruckList = GenerateTruckList(selectedNumber);

            var itemsToRemove = Trucks.Except(newTruckList).ToList();
            var itemsToAdd = newTruckList.Except(Trucks).ToList();

            foreach (var item in itemsToRemove) Trucks.Remove(item);
            foreach (var item in itemsToAdd) Trucks.Add(item);
        }
    }

    public void UpdateViewOptions()
    {
        // Start with a fresh, empty list
        ViewOptions.Clear();
        // Always add "ALL" as the first option
        ViewOptions.Add("ALL");

        if (SelectedReport?.LineItems != null)
        {
            // Go through every line item and every packing unit,
            // find all the unique truck numbers that are being used.
            var usedTrucks = SelectedReport.LineItems
                .SelectMany(li => li.LineItemPackingUnits)
                .Select(pu => pu.TruckNumber)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t =>
                {
                    // This helps sort "TRUCK 1", "TRUCK 2", "TRUCK 10" correctly
                    int.TryParse(t.Split(' ').Last(), out int num);
                    return num;
                });

            // Add each of the used trucks to our list
            foreach (var truck in usedTrucks)
            {
                ViewOptions.Add(truck);
            }
        }

        if (!ViewOptions.Contains(SelectedReportView))
        {
            SelectedReportView = "ALL";
        }
    }

    private List<string> GenerateTruckList(int selectedTruckNumber)
    {
        int totalTrucks = Math.Max(10, selectedTruckNumber + 5);
        return Enumerable.Range(1, totalTrucks)
            .Select(i => $"TRUCK {i}")
            .ToList();
    }

    private static string NormalizeUnitType(string? typeOfUnit)
    {
        if (string.IsNullOrWhiteSpace(typeOfUnit))
            return "Unknown";

        return typeOfUnit.ToUpperInvariant() switch
        {
            "CHAIRS" or "CURVARE" or "IMMIX" or "OH!" or "OLLIE" => "CHAIRS",
            "TABLE LEGS, TUBULAR STL EXC 1 QTR DIAMETER, N-G-T 2 INCH DIAMETER" => "TABLE LEGS",
            _ => typeOfUnit.ToUpperInvariant()
        };
    }

    private static bool TryGetOrderNumber(string input, out int orderNumber)
        => int.TryParse(input, out orderNumber);
}