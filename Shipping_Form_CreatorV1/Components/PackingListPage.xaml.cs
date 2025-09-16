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
            if (e.PropertyName == nameof(MainViewModel.SelectedReport))
                BuildPagesWithBusy();
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
        var isDittoUser = _viewModel.IsDittoUser;

        var header = selectedReport.Header;
        header.LogoImagePath = isDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;

        var lineItems = selectedReport.LineItems
            .Where(li => li.LineItemHeader?.LineItemNumber < 950)
            .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
            .ToList();

        var trailerNotes = selectedReport.LineItems
            .SelectMany(li => li.LineItemDetails)
            .Where(d => d.ModelItem == 950m)
            .Where(d => string.Equals(d.PackingListFlag, "Y", StringComparison.OrdinalIgnoreCase))
            .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
            .OrderBy(d => d.ModelItem)
            .ThenBy(d => d.NoteSequenceNumber)
            .ToList();
        _viewModel.PackingListNotes = new ObservableCollection<LineItemDetail>(trailerNotes);

        if (lineItems.Count == 0)
        {
            var emptyPage = new PackingListPageOne { PageNumberText = "Page 1 of 1" };
            PageContainer.Children.Add(emptyPage);
        }
        else
        {
            var firstItem = lineItems[0];
            var firstDetails = new ObservableCollection<LineItemDetail>(GetDetailsFor(firstItem));
            var filteredDetails = new ObservableCollection<LineItemDetail>(firstDetails.Where(d =>
                d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true));

            var pageOne = new PackingListPageOne
            {
                PageNumberText = "Page 1 of 1",
                Header = header,
                LineItem = firstItem,
                Details = filteredDetails,
                PackingUnits = new ObservableCollection<LineItemPackingUnit>(firstItem.LineItemPackingUnits)
            };
            PageContainer.Children.Add(pageOne);

            var remainingItems = lineItems
                .Skip(1)
                .Where(li => GetDetailsFor(li).Count > 0)
                .ToList();

            const int maxDetailsPerPage = 45;
            var currentPageItems = new List<LineItem>();
            var currentDetailsCount = 0;
            var pageCounter = 2;

            foreach (var item in remainingItems)
            {
                var detailsCount = GetDetailsFor(item).Count;
                if (currentDetailsCount + detailsCount > maxDetailsPerPage && currentPageItems.Count > 0)
                {
                    var multiPage = new PackingListPageTwoPlus
                    {
                        Header = header,
                        PageNumberTwoPlusText = $"Page {pageCounter} of X",
                        Items = new ObservableCollection<LineItem>(currentPageItems)
                    };
                    PageContainer.Children.Add(multiPage);

                    currentPageItems = [];
                    currentDetailsCount = 0;
                    pageCounter++;
                }

                currentPageItems.Add(item);
                currentDetailsCount += detailsCount;
            }

            if (currentPageItems.Count > 0)
            {
                var multiPage = new PackingListPageTwoPlus
                {
                    Header = header,
                    PageNumberTwoPlusText = $"Page {pageCounter} of X",
                    Items = new ObservableCollection<LineItem>(currentPageItems)
                };
                PageContainer.Children.Add(multiPage);
                pageCounter++;
            }

            var hasTrailerNotes = trailerNotes.Count > 0;
            if (hasTrailerNotes)
            {
                var trailerNotesPage = new PackingListNotesPage
                {
                    PageNumberText = $"Page {pageCounter} of {pageCounter}",
                    Header = header,
                    Details = new ObservableCollection<LineItemDetail>(trailerNotes)
                };
                PageContainer.Children.Add(trailerNotesPage);
                pageCounter++;
            }

            var totalPages = pageCounter - 1;

            if (lineItems.Count == 0)
            {
                totalPages = hasTrailerNotes ? 2 : 1;
            }

            if (PageContainer.Children.Count > 0 && PageContainer.Children[0] is PackingListPageOne firstPage)
            {
                firstPage.PageNumberText = $"Page 1 of {totalPages}";
            }

            for (var i = 1; i < PageContainer.Children.Count; i++)
            {
                if (PageContainer.Children[i] is PackingListPageTwoPlus page)
                {
                    page.PageNumberTwoPlusText = $"Page {i + 1} of {totalPages}";
                }
            }

            _viewModel.PageCount = totalPages;
        }

        return;

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