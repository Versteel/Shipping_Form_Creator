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



    public PackingListPage(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedReport) || (e.PropertyName == nameof(MainViewModel.SelectedReportView) && _viewModel.SelectedReport != null) && _viewModel.SelectedReportTitle == "PACKING LIST")
            {
                BuildPagesWithBusy();
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

        var selectedReport = _viewModel.SelectedReport ?? new ReportModel
        {
            Header = new ReportHeader(),
            LineItems = new ObservableCollection<LineItem>()
        };
        var isDittoUser = _viewModel.IsDittoUser;

        var header = selectedReport.Header;
        header.LogoImagePath = isDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;

        var selectedView = _viewModel.SelectedReportView;

        var rawLineItems = selectedReport.LineItems
            .Where(li => li.LineItemHeader?.LineItemNumber < 950)
            .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
            .ToList();

        var lineItems = new List<LineItem>();
        var isAllView = selectedView == "ALL";

        foreach (var li in rawLineItems)
        {
            if (isAllView)
            {
                var lineItemCopy = new LineItem(li, li.LineItemPackingUnits);
                lineItems.Add(lineItemCopy);
            }
            else
            {
                var filteredPackingUnits = li.LineItemPackingUnits
                    .Where(pu => string.Equals(pu.TruckNumber, selectedView, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filteredPackingUnits.Any())
                {
                    var lineItemCopy = new LineItem(li, new ObservableCollection<LineItemPackingUnit>(filteredPackingUnits));
                    lineItems.Add(lineItemCopy);
                }
            }
        }

        var trailerNotes = selectedReport.LineItems
            .SelectMany(li => li.LineItemDetails)
            .Where(d => d.ModelItem == 950m)
            .Where(d => string.Equals(d.PackingListFlag, "Y", StringComparison.OrdinalIgnoreCase))
            .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
            .OrderBy(d => d.ModelItem)
            .ThenBy(d => d.NoteSequenceNumber)
            .ToList();
        _viewModel.PackingListNotes = new ObservableCollection<LineItemDetail>(trailerNotes);

        var physicalItems = lineItems
            .Where(li => !string.IsNullOrEmpty(li.LineItemHeader?.ProductNumber))
            .ToList();

        if (!physicalItems.Any())
        {
            var emptyPage = new PackingListPageOne { PageNumberText = "Page 1 of 1", Header = header };
            PageContainer.Children.Add(emptyPage);
            return;
        }

        var firstItem = physicalItems[0];
        var firstDetails = new ObservableCollection<LineItemDetail>(GetDetailsFor(firstItem));
        var filteredDetails = new ObservableCollection<LineItemDetail>(firstDetails.Where(d =>
            d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true));

        var pageOne = new PackingListPageOne
        {
            PageNumberText = "Page 1 of 1",
            Header = header,
            LineItem = firstItem,
            Details = filteredDetails
        };
        PageContainer.Children.Add(pageOne);

        var remainingItems = physicalItems.Skip(1).ToList();
        const int maxLinesPerPage = 35;
        var currentPageItems = new List<LineItem>();
        var currentLinesOnPage = 0;
        var pageCounter = 2; // Start counting from page 2

        foreach (var item in remainingItems)
        {
            var linesForItem = 1 + GetDetailsFor(item).Count;
            if (currentLinesOnPage + linesForItem > maxLinesPerPage && currentPageItems.Any())
            {
                var multiPage = new PackingListPageTwoPlus
                {
                    Header = header,
                    Items = new ObservableCollection<LineItem>(currentPageItems)
                };
                PageContainer.Children.Add(multiPage);

                currentPageItems = new List<LineItem>();
                currentLinesOnPage = 0;
                pageCounter++;
            }
            currentPageItems.Add(item);
            currentLinesOnPage += linesForItem;
        }

        if (currentPageItems.Count != 0)
        {
            var multiPage = new PackingListPageTwoPlus
            {
                Header = header,
                Items = new ObservableCollection<LineItem>(currentPageItems)
            };
            PageContainer.Children.Add(multiPage);
            pageCounter++;
        }

        // --- RE-ADDED NOTES PAGE LOGIC ---
        var hasTrailerNotes = trailerNotes.Count != 0;
        if (hasTrailerNotes)
        {
            var trailerNotesPage = new PackingListNotesPage
            {
                Header = header,
                Details = new ObservableCollection<LineItemDetail>(trailerNotes)
            };
            PageContainer.Children.Add(trailerNotesPage);
            pageCounter++;
        }
        // --- END OF ADDED LOGIC ---

        // Final page numbering logic
        var totalPages = PageContainer.Children.Count;
        if (PageContainer.Children[0] is PackingListPageOne firstPage)
        {
            firstPage.PageNumberText = $"Page 1 of {totalPages}";
        }
        for (var i = 1; i < PageContainer.Children.Count; i++)
        {
            if (PageContainer.Children[i] is PackingListPageTwoPlus page)
            {
                page.PageNumberTwoPlusText = $"Page {i + 1} of {totalPages}";
            }
            else if (PageContainer.Children[i] is PackingListNotesPage notesPage)
            {
                notesPage.PageNumberText = $"Page {i + 1} of {totalPages}";
            }
        }
        _viewModel.PageCount = totalPages;

        return; // This was already here, keeping it for consistency

        static List<LineItemDetail> GetDetailsFor(LineItem li) =>
        [
            .. li.LineItemDetails
        .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
        .Where(d => d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true)
        .Where(d => d.NoteText is not null &&
                    !d.NoteText.Contains("OPTIONS BEGIN") &&
                    !d.NoteText.Contains("OPTIONS END"))
        .OrderBy(d => d.NoteSequenceNumber)
        ];
    }
}