using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using Syncfusion.Windows.Controls.Notification;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class PrintService
{
    private SfBusyIndicator? _busyIndicator;
    private Window? _loadingWindow;

    public async Task<List<UserControl>> BuildAllPackingListPages(MainViewModel viewModel)
    {
        ShowLoadingIndicator("Building pages...");

        try
        {
            var pages = new List<UserControl>();
            var report = viewModel.SelectedReport;
            var header = report.Header;

            var lineItems = report.LineItems
                .Where(li => !IsNoteOnly(li))
                .Where(li => !string.IsNullOrWhiteSpace(li.LineItemHeader?.ProductDescription))
                .Where(li => li.LineItemDetails != null && li.LineItemDetails.Any(d => d.PackingListFlag == "Y")).OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
                .ToList();

            var tableLegUnits = lineItems
                .SelectMany(li => li.LineItemPackingUnits)
                .Where(pu => pu.TypeOfUnit == Constants.PackingUnitCategories[1]);

            foreach (var packingUnit in tableLegUnits)
                packingUnit.TypeOfUnit = "TABLE LEGS";

            UpdateLoadingMessage("Creating page 1...");

            if (report.LineItems.Count > 0)
            {
                var firstLineItem = lineItems[0];

                var pageOne = new PackingListPageOne
                {
                    Header = header,
                    LineItem = firstLineItem,
                    Details = new ObservableCollection<LineItemDetail>(firstLineItem.LineItemDetails)
                };
                pages.Add(pageOne);
                await Task.Delay(25);
            }

            if (report.LineItems.Count > 1)
            {
                const int itemsPerPage = 2;

                for (var i = 1; i < lineItems.Count; i += itemsPerPage)
                {
                    var pageNumber = (i / itemsPerPage) + 2;
                    UpdateLoadingMessage($"Creating page {pageNumber}...");

                    var items = new ObservableCollection<LineItem>(lineItems.Skip(i).Take(itemsPerPage));

                    var pageTwoPlus = new PackingListPageTwoPlus
                    {
                        Header = header,
                        Items = items
                    };
                    pages.Add(pageTwoPlus);
                    await Task.Delay(25);
                }
            }

            if (viewModel.PackingListNotes is { Count: > 0 })
            {
                UpdateLoadingMessage("Creating notes page...");

                var notesPage = new PackingListNotesPage
                {
                    Header = header,
                    Details = viewModel.PackingListNotes
                };
                pages.Add(notesPage);
            }

            UpdateLoadingMessage("Finalizing pages...");

            var total = pages.Count;
            for (int i = 0; i < total; i++)
            {
                var label = $"Page {i + 1} of {total}";
                SetStringPropIfExists(pages[i], "PageNumberText", label);
                SetStringPropIfExists(pages[i], "PageNumberTwoPlusText", label);
            }

            return pages;
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    public async Task PrintPackingListPages(List<UserControl>? pages)
    {
        if (pages == null || pages.Count == 0)
            return;

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        ShowLoadingIndicator("Preparing print document...");

        try
        {
            var pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 816;
            var pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1056;

            var fixedDoc = new FixedDocument();
            var totalPages = pages.Count;

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                UpdateLoadingMessage($"Rendering page {i + 1} of {totalPages}...");

                try
                {
                    (page as dynamic).IsPrinting = true;
                }
                catch
                {
                    // ignored
                }

                var printArea = await GetPrintAreaAsync(page);
                if (printArea == null) continue;

                await PerformLayoutAsync(printArea, pageWidth, pageHeight);
                var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);
            }

            UpdateLoadingMessage("Sending to printer...");

            printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Packing List");
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    public async Task PrintBillOfLadingAsync(Page bolPage)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        //ShowLoadingIndicator("Preparing Bill of Lading...");

        try
        {
            var pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 816;
            var pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1056;

            if (bolPage.FindName("BillOfLadingPrintArea") is not FrameworkElement printArea)
            {
                MessageBox.Show("BillOfLadingPrintArea not found.");
                return;
            }

            await PerformLayoutAsync(printArea, pageWidth, pageHeight);
            await PerformLayoutAsync(printArea, pageWidth, pageHeight);
            var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);
            var fixedDoc = new FixedDocument();
            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(fixedPage);
            fixedDoc.Pages.Add(pageContent);
            printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Bill of Lading");
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    private async Task<FixedPage> CreateFixedPageFromVisualAsync(
        FrameworkElement printArea, double pageWidth, double pageHeight)
    {
        await Task.Yield();

        // Ensure the latest visual states (e.g., CheckBox.IsChecked) are fully rendered
        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        var fixedPage = new FixedPage { Width = pageWidth, Height = pageHeight };

        var rect = new Rectangle
        {
            Width = pageWidth,
            Height = pageHeight,
            Fill = new VisualBrush(printArea)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            }
        };

        FixedPage.SetLeft(rect, 0);
        FixedPage.SetTop(rect, 0);
        fixedPage.Children.Add(rect);

        fixedPage.Measure(new Size(pageWidth, pageHeight));
        fixedPage.Arrange(new Rect(new Size(pageWidth, pageHeight)));
        fixedPage.UpdateLayout();

        return fixedPage;
    }

    private async Task<FrameworkElement?> GetPrintAreaAsync(UserControl page)
    {
        await Task.Yield();

        return page.FindName("PackingListPage1PrintArea") as FrameworkElement ??
               page.FindName("PackingListPage2PlusPrintArea") as FrameworkElement ??
               page.FindName("PackingListNotesPage1PrintArea") as FrameworkElement;
    }

    private async Task PerformLayoutAsync(FrameworkElement printArea, double pageWidth, double pageHeight)
    {
        await Task.Yield();

        if (double.IsNaN(printArea.Width)) printArea.Width = pageWidth;
        if (double.IsNaN(printArea.Height)) printArea.Height = pageHeight;

        printArea.Measure(new Size(printArea.Width, printArea.Height));
        await Task.Delay(10);
        printArea.Arrange(new Rect(0, 0, printArea.Width, printArea.Height));
        await Task.Delay(10);
        printArea.UpdateLayout();
        await Task.Delay(10);
    }

    public BillOfLading BuildBillOfLadingPage(MainViewModel viewModel)
    {
        return new BillOfLading(viewModel);
    }

    private static bool IsNoteOnly(LineItem li)
    {
        var h = li.LineItemHeader;
        var noProduct = string.IsNullOrWhiteSpace(h?.ProductNumber);
        var qtyZero = h is { OrderedQuantity: 0m, PickOrShipQuantity: 0m, BackOrderQuantity: 0m };
        return noProduct && qtyZero;
    }

    private static void SetStringPropIfExists(object target, string propertyName, string value)
    {
        var prop = target.GetType().GetProperty(propertyName);
        if (prop is { CanWrite: true } && prop.PropertyType == typeof(string))
            prop.SetValue(target, value);
    }

    private void ShowLoadingIndicator(string message = "Loading...")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_loadingWindow != null) return;

            _busyIndicator = new SfBusyIndicator
            {
                IsBusy = true,
                Header = message,
                AnimationType = AnimationTypes.DoubleCircle,
                ViewboxWidth = 50,
                ViewboxHeight = 50,
                Width = 300,
                Height = 150
            };

            _loadingWindow = new Window
            {
                Title = "Processing",
                Content = _busyIndicator,
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                ShowInTaskbar = false,
                Owner = Application.Current.MainWindow
            };

            _loadingWindow.Show();
        });
    }

    private void UpdateLoadingMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_busyIndicator != null)
                _busyIndicator.Header = message;
        });
    }

    private void HideLoadingIndicator()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _loadingWindow?.Close();
            _loadingWindow = null;
            _busyIndicator = null;
        });
    }
}