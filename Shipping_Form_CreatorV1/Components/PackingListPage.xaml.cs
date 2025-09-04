using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.ViewModels;
using Shipping_Form_CreatorV1.Utilities;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shipping_Form_CreatorV1.Components
{
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
            _viewModel.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedReport))
                    _ = BuildPagesWithBusyAsync();
            };

            Loaded += async (_, _) => await BuildPagesWithBusyAsync();
        }

       
        private async Task BuildPagesWithBusyAsync()
        {
            try
            {
                _viewModel.IsBusy = true;

                await Task.Yield();

                BuildPages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error building packing list pages: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsBusy = false;
            }
        }

        private void BuildPages()
        {
            PageContainer.Children.Clear();

            var selectedReport = _viewModel.SelectedReport;
            var isDittoUser = _viewModel.IsDittoUser;

            var header = selectedReport.Header;
            header.LogoImagePath = isDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;
            static List<LineItemDetail> GetDetailsFor(LineItem li) => [.. li.LineItemDetails
                    .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
                    .Where(d => d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true)
                    .Where(d => d.NoteText is not null && 
                        !d.NoteText.Contains("OPTIONS BEGIN") && 
                        !d.NoteText.Contains("OPTIONS END"))
                    .OrderBy(d => d.NoteSequenceNumber)
                    ];

            var lineItems = selectedReport.LineItems
                .Where(li => !IsNoteOnly(li))
                .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
                .ToList();

            lineItems = [.. lineItems.Where(li => li.LineItemDetails.Any(d => d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true && 
                !string.IsNullOrWhiteSpace(d.NoteText) &&
                !d.NoteText.Contains("OPTIONS BEGIN") && 
                !d.NoteText.Contains("OPTIONS END")))];

            var trailerNotes = selectedReport.LineItems
                .SelectMany(li => li.LineItemDetails)
                .Where(d => d.ModelItem == 950m) // Shipping / BOL Notes
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

                var pageOne = new PackingListPageOne
                {
                    PageNumberText = "Page 1 of 1",
                    Header = header,
                    LineItem = firstItem,
                    Details = firstDetails,
                    PackingUnits = new ObservableCollection<LineItemPackingUnit>(firstItem.LineItemPackingUnits)
                };
                PageContainer.Children.Add(pageOne);

                var blocks = lineItems
                    .Skip(1)
                    .Where(li => GetDetailsFor(li).Count > 0)
                    .ToList();

                const int perPage = 2;

                static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
                {
                    var list = new List<T>(size);
                    foreach (var x in source)
                    {
                        list.Add(x);
                        if (list.Count != size) continue;
                        yield return list;
                        list = new List<T>(size);
                    }
                    if (list.Count > 0)
                    {
                        yield return list;
                    }
                }

                var pages = Chunk(blocks, perPage).ToList();

                for (var i = 0; i < pages.Count; i++)
                {
                    var multiPage = new PackingListPageTwoPlus
                    {
                        Header = header,
                        PageNumberTwoPlusText = $"Page {i + 2} of {pages.Count + 1}",
                        Items = new ObservableCollection<LineItem>(pages[i]),
                    };
                    PageContainer.Children.Add(multiPage);
                }

                var hasTrailerNotes = trailerNotes.Count > 0;
                if (hasTrailerNotes)
                {
                    var trailerNotesPage = new PackingListNotesPage
                    {
                        PageNumberText = $"Page {pages.Count + 2} of {pages.Count + 2}",
                        Header = header,
                        Details = new ObservableCollection<LineItemDetail>(trailerNotes)
                    };
                    PageContainer.Children.Add(trailerNotesPage);
                }

                var totalPages =
                    1 + Math.Max(0, (lineItems.Count - 1 + 1) / 2) +
                    (hasTrailerNotes ? 1 : 0);

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

            static bool IsNoteOnly(LineItem li)
            {
                var h = li.LineItemHeader;
                var noProduct = string.IsNullOrWhiteSpace(h?.ProductNumber);
                var qtyZero = h is { OrderedQuantity: 0m, PickOrShipQuantity: 0m, BackOrderQuantity: 0m };
                return noProduct && qtyZero;
            }
        }
    }
}
