using GongSolutions.Wpf.DragDrop;
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
using System.Windows;
using System.Windows.Input;

namespace Shipping_Form_CreatorV1.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDropTarget
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

        AddHandlingUnitCommand = new RelayCommand(AddNewHandlingUnit);
        RemoveHandlingUnitCommand = new RelayCommand(RemoveHandlingUnit);

        SearchByDateResults = [];
        SelectedReportsGroups = [];
        SearchByDateResults = Array.Empty<ReportModel>();

        _selectedReport = new ReportModel
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

    public void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // -------------------------
    // Commands
    // -------------------------
    public ICommand AddHandlingUnitCommand { get; }
    public ICommand RemoveHandlingUnitCommand { get; }

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
            OnPropertyChanged(nameof(HandlingUnits));
            OnPropertyChanged(nameof(BolSpecialInstructions));
            OnPropertyChanged(nameof(PackingListNotes));
            UpdateGroups();
            UpdateViewOptions();
            OnPropertyChanged(nameof(BolTotalPieces));
            OnPropertyChanged(nameof(BolTotalWeight));
            OnPropertyChanged(nameof(AllPiecesTotal));
            OnPropertyChanged(nameof(AllWeightTotal));
            OnPropertyChanged(nameof(ShouldDisplayTruckNumber));
            OnPropertyChanged(nameof(HandlingUnitPanelVisibility));
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

    public ObservableCollection<HandlingUnit>? HandlingUnits => new ObservableCollection<HandlingUnit>(SelectedReport.HandlingUnits);

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

    public ObservableCollection<string> ViewOptions { get; set; } = new();

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

    private ObservableCollection<int> _lineNumbersForDropdown = new();
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

    public ObservableCollection<string> Trucks { get; set; }

    private string _selectedTruck;
    // In MainViewModel.cs, replace the existing SelectedTruck property
    public string SelectedTruck
    {
        get => _selectedTruck;
        set
        {
            if (_selectedTruck == value) return;
            _selectedTruck = value;
            OnPropertyChanged(nameof(SelectedTruck));

            // Call our new, consolidated method
            if (int.TryParse(value?.Split(' ').Last(), out int selectedNum))
            {
                UpdateTrucksList(selectedNum);
            }
        }
    }

    // Computed & Derived Properties
    public bool IsSaveEnabled => SelectedReportTitle == "PACKING LIST";
    public bool IsPrintAvailable => SelectedReportTitle != "SEARCH RESULTS";
    public string CurrentUser => Environment.UserName;
    public string SearchDateString => SearchByDate?.ToShortDateString() ?? string.Empty;
    public int BolTotalPieces => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int BolTotalWeight => SelectedReportsGroups.Sum(r => r.TotalWeight);
    public int AllPiecesTotal => SelectedReportsGroups.Sum(r => r.TotalPieces);
    public int AllWeightTotal => SelectedReportsGroups.Sum(r => r.TotalWeight);
    public bool IsMultiTruckOrder => ViewOptions.Count > 2;
    public string HandlingUnitPanelVisibility => SelectedReport?.Header?.OrderNumber >= 1 ? "Visible" : "Collapsed";

    public bool ShouldDisplayTruckNumber =>
        SelectedReport?.LineItems
            .SelectMany(li => li.LineItemPackingUnits)
            .Select(pu => pu.TruckNumber)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .Count() > 1;

    public ObservableCollection<LineItemDetail> BolSpecialInstructions
    {
        get
        {
            if (SelectedReport == null)
            {
                return []; // Return an empty collection if the report is null
            }

            return new(SelectedReport.LineItems
                .SelectMany(li => li.LineItemDetails)
                .Where(detail => detail.BolFlag != null && detail.BolFlag.Equals("Y", StringComparison.InvariantCultureIgnoreCase)));
        }
    }

    public ObservableCollection<LineItemDetail>? PackingListNotes { get; set; }
    public ObservableCollection<string> ShippingInstructions { get; set; } = [];
    public ObservableCollection<PackingListSummaryItem> ConsolidatedSummary { get; set; } = [];
    public ObservableCollection<TotalsItem> OverallTotals { get; set; } = [];



    // -------------------------
    // Public Methods
    // -------------------------
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is LineItemPackingUnit && dropInfo.VisualTarget is FrameworkElement { DataContext: HandlingUnit })
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Move;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is LineItemPackingUnit packingUnit && dropInfo.VisualTarget is FrameworkElement { DataContext: HandlingUnit targetHandlingUnit })
        {
            var sourceLineItem = SelectedReport.LineItems.FirstOrDefault(li => li.LineItemPackingUnits.Contains(packingUnit));

            if (sourceLineItem != null)
            {
                targetHandlingUnit.ContainedUnits.Add(packingUnit);
                packingUnit.HandlingUnitId = targetHandlingUnit.Id;
                LinkPackingUnitsToHandlingUnits();
                OnPropertyChanged(nameof(SelectedReport));
            }
        }
    }
    public void UpdateOrderSummary()
    {
        ConsolidatedSummary.Clear();
        ShippingInstructions.Clear();
        OverallTotals.Clear();

        if (SelectedReport == null) return;

        // --- Create the structured summary list ---
        var summaryItems = SelectedReport.LineItems
             .Where(li => li.LineItemHeader != null &&
                          li.LineItemHeader.PickOrShipQuantityInt > 0 &&
                          !string.IsNullOrWhiteSpace(li.LineItemHeader.ProductDescription)) // <-- Add this line
             .GroupBy(li => li.LineItemHeader?.ProductDescription.Trim())
             .Select(g => new PackingListSummaryItem
             {
                 Description = g.Key,
                 Quantity = g.Sum(li => li.LineItemHeader.PickOrShipQuantityInt),
                 TotalWeight = g.SelectMany(li => li.LineItemPackingUnits).Sum(pu => pu.Weight)
             });

        foreach (var item in summaryItems)
        {
            ConsolidatedSummary.Add(item);
        }

        // --- Calculate the overall totals that will appear below the grid ---
        var allPackingUnits = SelectedReport.LineItems.SelectMany(li => li.LineItemPackingUnits).ToList();
        var totalPackages = allPackingUnits
            .Where(pu => pu.CartonOrSkid != "PACKED WITH LINE ")
            .GroupBy(pu => pu.CartonOrSkid)
            .Select(g => new { Type = g.Key, Count = g.Sum(pu => pu.Quantity) });

        foreach (var package in totalPackages)
        {
            OverallTotals.Add(new TotalsItem { Label = $"Total {package.Type} Count:", Value = package.Count.ToString() });
        }
        var totalWeight = allPackingUnits.Sum(pu => pu.Weight);
        OverallTotals.Add(new TotalsItem { Label = "Total Shipment Weight:", Value = $"{totalWeight} Lbs" });

        // --- Populate Shipping Instructions ---
        if (PackingListNotes != null)
        {
            foreach (var note in PackingListNotes)
            {
                ShippingInstructions.Add(note.NoteText);
            }
        }
    }

    public async Task LoadDocumentAsync(string orderNumberInput, string suffix, CancellationToken ct = default)
    {
        Log.Information($"User {CurrentUser} is attempting to load Sales Order {orderNumberInput}-{suffix}");

        if (string.IsNullOrWhiteSpace(orderNumberInput)) return;
        if (string.IsNullOrWhiteSpace(suffix)) suffix = "00";

        if (!TryGetOrderNumber(orderNumberInput, out var orderNumber) || !int.TryParse(suffix, out var suffixNumber))
        {
            DialogService.ShowErrorDialog("Invalid Sales Order Number or Suffix.");
            return;
        }

        IsBusy = true;
        try
        {
            // Step 1: Always fetch the latest data from the ERP. This is now our source of truth.
            var erpDocument = await _odbcService.GetReportAsync(orderNumber, suffixNumber, ct);
            if (erpDocument == null)
            {
                DialogService.ShowErrorDialog($"Could not find Sales Order {orderNumber}-{suffix} in the ERP.");
                return;
            }

            // Step 2: Check for a locally saved version to retrieve packing units.
            var cachedDocument = await _sqliteService.GetReportAsync(orderNumber, suffixNumber, ct);

            // Step 3: If a cached version exists, merge its packing units into the fresh ERP data.
            if (cachedDocument != null)
            {
                erpDocument.Id = cachedDocument.Id; // Preserve the database ID
                erpDocument.Header.Id = cachedDocument.Header.Id;

                foreach (var erpLineItem in erpDocument.LineItems)
                {
                    // Find the matching line item in the cached document
                    var cachedLineItem = cachedDocument.LineItems.FirstOrDefault(li =>
                        li.LineItemHeader?.LineItemNumber == erpLineItem.LineItemHeader?.LineItemNumber);

                    if (cachedLineItem != null)
                    {
                        erpLineItem.Id = cachedLineItem.Id; // Preserve the line item ID
                                                            // This is the key: we copy the user's work into the fresh data.
                        erpLineItem.LineItemPackingUnits = cachedLineItem.LineItemPackingUnits;
                    }
                }
            }

            SelectedReport = erpDocument; // The final, merged report is now set.
            UpdateLineNumbers();
            LinkPackingUnitsToHandlingUnits();
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

                        if (!erpLine.LineItemPackingUnits.Any())
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

                    if (!erpLine.LineItemPackingUnits.Any())
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

    private void LinkPackingUnitsToHandlingUnits()
    {
        if (SelectedReport == null) return;

        // This safely creates an empty dictionary if HandlingUnits is null or empty.
        var handlingUnitsById = SelectedReport.HandlingUnits?.ToDictionary(h => h.Id)
                                  ?? new Dictionary<int, HandlingUnit>();

        var allPackingUnits = SelectedReport.LineItems?.SelectMany(li => li.LineItemPackingUnits)
                               ?? Enumerable.Empty<LineItemPackingUnit>();

        foreach (var packingUnit in allPackingUnits)
        {
            if (packingUnit.HandlingUnitId.HasValue &&
                handlingUnitsById.TryGetValue(packingUnit.HandlingUnitId.Value, out var handlingUnit))
            {
                packingUnit.HandlingUnit = handlingUnit;
            }
            else
            {
                // This now correctly clears the reference if the pallet was deleted.
                packingUnit.HandlingUnit = null;
            }
        }

        // Notify the UI to refresh the packing unit list with the updated links.
        OnPropertyChanged(nameof(SelectedReport));
    }
    private void AddNewHandlingUnit(object? obj)
    {
        if (SelectedReport?.HandlingUnits == null) return;

        // --- NEW NAMING LOGIC ---
        // 1. Find the highest number used in the names of existing pallets.
        var highestPalletNumber = SelectedReport.HandlingUnits
            .Select(h => h.Description)
            .Where(d => d.StartsWith("Pallet "))
            .Select(d => int.TryParse(d.Replace("Pallet ", ""), out int num) ? num : 0)
            .DefaultIfEmpty(0)
            .Max();

        // 2. The new pallet's number will be one higher than the current max.
        var newDescription = $"Pallet {highestPalletNumber + 1}";

        // The ID logic for the database can remain the same.
        var newId = SelectedReport.HandlingUnits.Any()
                        ? SelectedReport.HandlingUnits.Max(h => h.Id) + 1
                        : 1;

        var newHandlingUnit = new HandlingUnit
        {
            Id = newId,
            Description = newDescription, // Use our new, sequential name
            ReportModelId = SelectedReport.Id
        };

        SelectedReport.HandlingUnits.Add(newHandlingUnit);
        OnPropertyChanged(nameof(SelectedReport));
    }

    private void RemoveHandlingUnit(object? obj)
    {
        if (obj is not HandlingUnit handlingUnit || SelectedReport?.HandlingUnits == null) return;
        if(handlingUnit.ContainedUnits.Count > 0)
        {
            var messageBoxAnswer = MessageBox.Show("Are you sure you want to remove this handling unit? This will unassign all contained packing units.", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxAnswer != MessageBoxResult.Yes) return;

            foreach (var unit in handlingUnit.ContainedUnits)
            {
                unit.HandlingUnitId = null;
            }
            
        }
        SelectedReport.HandlingUnits.Remove(handlingUnit);
        OnPropertyChanged(nameof(SelectedReport));
        LinkPackingUnitsToHandlingUnits();
    }

    private void UpdateLineNumbers()
    {
        var numbers = SelectedReport.LineItems
            .Where(li => li.LineItemHeader?.LineItemNumber is < 900 && !string.IsNullOrWhiteSpace(li.LineItemHeader.ProductDescription))
            .Select(li => (int)(li.LineItemHeader?.LineItemNumber ?? 0))
            .ToList();
        LineNumbersForDropdown = new ObservableCollection<int>(numbers);
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
            .GroupBy(u => new { TypeOfUnit = NormalizeUnitType(u.TypeOfUnit), CartonOrSkid = string.IsNullOrWhiteSpace(u.CartonOrSkid) ? "Unknown" : u.CartonOrSkid })
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

    private void UpdateTrucksList(int? newlySelectedNumber = null)
    {
        if(SelectedReport == null) return;
        // 1. Find the highest truck number currently used in the report's data.
        var maxTruckNumberInData = SelectedReport.LineItems
            .SelectMany(li => li.LineItemPackingUnits)
            .Select(pu => pu.TruckNumber)
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => int.TryParse(t.Split(' ').Last(), out int num) ? num : 0)
            .DefaultIfEmpty(1)
            .Max();

        // 2. Consider the newly selected number if it was provided.
        var requiredMax = Math.Max(maxTruckNumberInData, newlySelectedNumber ?? 1);

        // 3. Generate the definitive list that is required.
        var requiredTruckList = GenerateTruckList(requiredMax);

        // 4. FIX: Instead of clearing the list, just add the missing items.
        // This is not destructive and won't cause the UI to reset its selection.
        var itemsToAdd = requiredTruckList.Except(Trucks).ToList();
        foreach (var truck in itemsToAdd)
        {
            Trucks.Add(truck);
        }
    }

    public void UpdateViewOptions()
    {
        // Call our new method to ensure the editor's ItemsSource is up-to-date
        UpdateTrucksList();

        ViewOptions.Clear();
        ViewOptions.Add("ALL");

        if (SelectedReport?.LineItems != null)
        {
            var usedTrucks = SelectedReport.LineItems
                .SelectMany(li => li.LineItemPackingUnits)
                .Select(pu => pu.TruckNumber)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => { _ = int.TryParse(t.Split(' ').Last(), out int num); return num; });

            foreach (var truck in usedTrucks)
            {
                ViewOptions.Add(truck);
            }
        }

        if (!ViewOptions.Contains(SelectedReportView))
        {
            SelectedReportView = "ALL";
        }
        OnPropertyChanged(nameof(IsMultiTruckOrder));
        OnPropertyChanged(nameof(ShouldDisplayTruckNumber));
    }

    private List<string> GenerateTruckList(int selectedTruckNumber)
    {
        int totalTrucks = Math.Max(10, selectedTruckNumber + 5);
        return [.. Enumerable.Range(1, totalTrucks).Select(i => $"TRUCK {i}")];
    }

    private static string NormalizeUnitType(string? typeOfUnit)
    {
        if (string.IsNullOrWhiteSpace(typeOfUnit)) return "Unknown";
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