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
                displayItems.Add(itemCopy);
            }
        }


        if (displayItems.Count == 0)
        {
            var emptyPage = new PackingListPageOne { DataContext = _viewModel, Header = header, PageNumberText = "Page 1 of 1" };
            PageContainer.Children.Add(emptyPage);
            _viewModel.PageCount = 1;
        }
        else
        {
            // Page 1: Handle the FIRST item separately
            var firstItem = displayItems.First();
            var pageOne = new PackingListPageOne
            {
                DataContext = _viewModel,
                Header = header,
                LineItem = firstItem,
                Details = new ObservableCollection<LineItemDetail>(GetDetailsFor(firstItem))
            };
            PageContainer.Children.Add(pageOne);

            // Handle all REMAINING items for subsequent pages.
            var remainingItems = displayItems.Skip(1).ToList();
            if (remainingItems.Count != 0)
            {
                var currentPageItems = new List<LineItem>();
                var currentDetailsOnPage = 0;
                const int maxDetailsPerPage = 35;
                const double basePackingUnitHeight = 50;
                const double heightIncreasePerUnit = 20;
                const double detailsPerBlock = 2;
                const double maxPackingUnitHeight = 450;

                foreach (var item in remainingItems)
                {
                    var itemDetailsCount = GetDetailsFor(item).Count;
                    var detailsBlocks = itemDetailsCount / detailsPerBlock;

                    // Simplified: Calculate height and cap it, removing the special 'lastItem' logic
                    var calculatedHeight = basePackingUnitHeight + (detailsBlocks * heightIncreasePerUnit);
                    item.PackingUnitHeight = Math.Min(calculatedHeight, maxPackingUnitHeight);

                    // If adding this item would exceed the max, start a new page
                    // **This is your primary page break condition.**
                    if (currentDetailsOnPage + itemDetailsCount > maxDetailsPerPage && currentPageItems.Count != 0)
                    {
                        var nextPage = new PackingListPageTwoPlus
                        {
                            DataContext = _viewModel,
                            Header = header,
                            Items = new ObservableCollection<LineItem>(currentPageItems)
                        };
                        PageContainer.Children.Add(nextPage);

                        currentPageItems = [];
                        currentDetailsOnPage = 0;
                    }

                    currentPageItems.Add(item);
                    currentDetailsOnPage += itemDetailsCount;
                }

                // Add the final batch of items
                if (currentPageItems.Count != 0)
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

        // Add the notes page if it exists
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