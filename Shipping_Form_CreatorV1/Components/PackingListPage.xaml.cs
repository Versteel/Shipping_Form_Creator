using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.ViewModels;
using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shipping_Form_CreatorV1.Components;

/// <summary>
/// Interaction logic for PackingListPage.xaml
/// </summary>
public partial class PackingListPage
{
    private readonly MainViewModel _viewModel;
    public MainViewModel VM => _viewModel;

    public PackingListPage(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedReport) || e.PropertyName == nameof(MainViewModel.SelectedReportView))
            {
                if (_viewModel.SelectedReport != null && _viewModel.SelectedReportTitle == "PACKING LIST")
                {
                    BuildPagesWithBusy();
                }
            }
        };

        Loaded += (_, _) => BuildPagesWithBusy();
    }

    private void BuildPagesWithBusy()
    {
        try
        {
            BuildPages();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error building packing list pages: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildPages()
    {
        PageContainer.Children.Clear();
        var selectedReport = _viewModel.SelectedReport;
        if (selectedReport == null) return;

        var header = selectedReport.Header;
        header.LogoImagePath = _viewModel.IsDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;
        header.HeaderPhoneNumber = _viewModel.IsDittoUser ? "812.482.3043" : "800.876.2120";
        header.HeaderEmail = _viewModel.IsDittoUser ? "dittosales.com" : "versteel.com";

        var trailerNotes = selectedReport.LineItems
            .SelectMany(li => li.LineItemDetails)
            .Where(d => d.ModelItem == 950m)
            .Where(d => string.Equals(d.PackingListFlag, "Y", StringComparison.OrdinalIgnoreCase))
            .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
            .OrderBy(d => d.ModelItem)
            .ThenBy(d => d.NoteSequenceNumber)
            .ToList();
        _viewModel.PackingListNotes = new ObservableCollection<LineItemDetail>(trailerNotes);

        var displayItems = new List<LineItem>();
        var selectedView = _viewModel.SelectedReportView;
        var isAllView = selectedView == "ALL";

        var allPhysicalLineItems = selectedReport.LineItems
            .Where(li => !string.IsNullOrEmpty(li.LineItemHeader?.ProductNumber))
            .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
            .ToList();

        foreach (var originalItem in allPhysicalLineItems)
        {
            var filteredPackingUnits = new ObservableCollection<LineItemPackingUnit>(
                originalItem.LineItemPackingUnits.Where(pu =>
                    isAllView || string.Equals(pu.TruckNumber, selectedView, StringComparison.OrdinalIgnoreCase))
            );

            if (!originalItem.LineItemPackingUnits.Any() || filteredPackingUnits.Any())
            {
                var itemCopy = new LineItem(originalItem, filteredPackingUnits);
                itemCopy.LineItemDetails = new ObservableCollection<LineItemDetail>(GetDetailsFor(itemCopy));
                displayItems.Add(itemCopy);
            }
        }

        if (displayItems.Count == 0)
        {
            var emptyPage = new PackingListPageOne { DataContext = _viewModel, Header = header, PageNumberText = "Page 1 of 1", Items = [] };
            PageContainer.Children.Add(emptyPage);
            _viewModel.PageCount = 1;
        }
        else
        {
            // Pagination Constants
            const int maxItemsPageOne = 3;       // Max line items allowed on first page
            const int maxDetailsPageOne = 25;    // Max detail lines allowed on first page
            const int maxDetailsPerPage = 25;    // Max detail lines allowed on subsequent pages
            const double basePackingUnitHeight = 50;
            const double heightIncreasePerUnit = 20;
            const double detailsPerBlock = 2;
            const double maxPackingUnitHeight = 450;

            var currentPageItems = new List<LineItem>();
            var currentDetailsOnPage = 0;
            var isFirstPage = true;

            foreach (var item in displayItems)
            {
                var itemDetailsCount = GetDetailsFor(item).Count;
                var detailsBlocks = itemDetailsCount / detailsPerBlock;
                var calculatedHeight = basePackingUnitHeight + (detailsBlocks * heightIncreasePerUnit);
                item.PackingUnitHeight = Math.Min(calculatedHeight, maxPackingUnitHeight);

                // Logic to determine if a page break is needed
                bool shouldFlipPage = false;
                if (isFirstPage)
                {
                    // Flip if we hit the 3-item limit OR the detail text limit
                    if (currentPageItems.Count >= maxItemsPageOne || currentDetailsOnPage + itemDetailsCount > maxDetailsPageOne)
                    {
                        shouldFlipPage = true;
                    }
                }
                else
                {
                    // Flip if we hit the detail text limit
                    if (currentDetailsOnPage + itemDetailsCount > maxDetailsPerPage)
                    {
                        shouldFlipPage = true;
                    }
                }

                if (shouldFlipPage && currentPageItems.Count != 0)
                {
                    if (isFirstPage)
                    {
                        var pageOne = new PackingListPageOne
                        {
                            DataContext = _viewModel,
                            Header = header,
                            Items = new ObservableCollection<LineItem>(currentPageItems)
                        };
                        PageContainer.Children.Add(pageOne);
                        isFirstPage = false;
                    }
                    else
                    {
                        var nextPage = new PackingListPageTwoPlus
                        {
                            DataContext = _viewModel,
                            Header = header,
                            Items = new ObservableCollection<LineItem>(currentPageItems)
                        };
                        PageContainer.Children.Add(nextPage);
                    }

                    currentPageItems = new List<LineItem>();
                    currentDetailsOnPage = 0;
                }

                currentPageItems.Add(item);
                currentDetailsOnPage += itemDetailsCount;
            }

            // Add the final batch of items
            if (currentPageItems.Count != 0)
            {
                if (isFirstPage)
                {
                    var pageOne = new PackingListPageOne
                    {
                        DataContext = _viewModel,
                        Header = header,
                        Items = new ObservableCollection<LineItem>(currentPageItems)
                    };
                    PageContainer.Children.Add(pageOne);
                }
                else
                {
                    var finalPage = new PackingListPageTwoPlus
                    {
                        DataContext = _viewModel,
                        Header = header,
                        Items = new ObservableCollection<LineItem>(currentPageItems)
                    };
                    PageContainer.Children.Add(finalPage);
                }
            }
        }

        if (!_viewModel.IsDittoUser)
        {
            if ((_viewModel.PackingListNotes != null && _viewModel.PackingListNotes.Any()) ||
                (_viewModel.SelectedReport.HandlingUnits != null && _viewModel.SelectedReport.HandlingUnits.Any()))
            {
                _viewModel.UpdateOrderSummary();

                var orderSummaryPage = new PackingListNotesPage
                {
                    DataContext = _viewModel,
                    Header = header,
                    ShippingInstructions = _viewModel.ShippingInstructions,
                    ConsolidatedSummary = _viewModel.ConsolidatedSummary,
                    OverallTotals = _viewModel.OverallTotals,
                    HandlingUnits = new ObservableCollection<HandlingUnit>(_viewModel.SelectedReport.HandlingUnits)
                };
                PageContainer.Children.Add(orderSummaryPage);
            }
        }

        // Final page numbering logic
        var totalPages = PageContainer.Children.Count;
        for (var i = 0; i < totalPages; i++)
        {
            if (PageContainer.Children[i] is PackingListPageOne p1)
            {
                p1.PageNumberText = $"Page {i + 1} of {totalPages}";
            }
            else if (PageContainer.Children[i] is PackingListPageTwoPlus p2)
            {
                p2.PageNumberTwoPlusText = $"Page {i + 1} of {totalPages}";
            }
            else if (PageContainer.Children[i] is PackingListNotesPage notesPage)
            {
                notesPage.PageNumberText = $"Page {i + 1} of {totalPages}";
            }
        }
        _viewModel.PageCount = totalPages;

        static List<LineItemDetail> GetDetailsFor(LineItem li) =>
        [
            .. li.LineItemDetails
               .Where(d => !string.IsNullOrWhiteSpace(d.NoteText) && d.PackingListFlag?.Trim() == "Y")
               .Where(d => d.NoteText is not null && !d.NoteText.Contains("OPTIONS"))
               .OrderBy(d => d.NoteSequenceNumber)
        ];
    }
}