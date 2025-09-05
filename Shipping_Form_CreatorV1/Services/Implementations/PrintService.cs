using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using Syncfusion.UI.Xaml.ProgressBar;
using Syncfusion.Windows.Controls.Notification;
using System.Collections.ObjectModel;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                    .Where(lih => lih.LineItemHeader?.LineItemNumber > 0)
                    .Where(li => li.LineItemDetails.All(d => d.PackingListFlag == "Y"))
                    .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
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

    public async Task PreviewBillOfLadingAsync(Page bolPage)
    {
        try
        {
            const double pageWidth = 816, pageHeight = 1056;

            if (bolPage.FindName("BillOfLadingPrintArea") is not FrameworkElement printArea)
            {
                MessageBox.Show("BillOfLadingPrintArea not found.");
                return;
            }

            await PerformLayoutAsync(printArea, pageWidth, pageHeight);

            // Make sure bindings/visual states (IsChecked) are up-to-date
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

            // >>> Apply print-safe checkbox template (or disable fallback) <<<
            var snapshots = PrepareCheckBoxesForPrint(printArea /*, useDisableFallback: true */);
            try
            {
                var fixedDoc = new FixedDocument();
                var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);

                var preview = new PrintPreviewWindow
                {
                    Owner = Application.Current.MainWindow,
                    Document = fixedDoc
                };
                preview.ShowDialog();
            }
            finally
            {
                // >>> Restore original checkbox styles/states <<<
                RestoreCheckBoxes(snapshots);
            }
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    public async Task PreviewPackingListPages(List<UserControl>? pages)
    {
        if (pages == null || pages.Count == 0) return;

        ShowLoadingIndicator("Preparing Packing List Preview...");
        try
        {
            double pageWidth = 816, pageHeight = 1056;

            var fixedDoc = new FixedDocument();
            var totalPages = pages.Count;

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                UpdateLoadingMessage($"Rendering page {i + 1} of {totalPages}...");

                try { (page as dynamic).IsPrinting = true; } catch { }

                var printArea = await GetPrintAreaAsync(page);
                if (printArea == null) continue;

                await PerformLayoutAsync(printArea, pageWidth, pageHeight);

                var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);
                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);
            }

            var preview = new PrintPreviewWindow
            {
                Owner = Application.Current.MainWindow,
                Document = fixedDoc
            };
            preview.ShowDialog();
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    private sealed record CheckBoxSnapshot(CheckBox Box, Style? OldStyle, bool OldIsEnabled);

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) yield break;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (var d in FindVisualChildren<T>(child))
                yield return d;
        }
    }

    private List<CheckBoxSnapshot> PrepareCheckBoxesForPrint(FrameworkElement root, bool useDisableFallback = false)
    {
        var snapshots = new List<CheckBoxSnapshot>();
        var printStyle = root.TryFindResource("PrintCheckBoxStyle") as Style;

        foreach (var cb in FindVisualChildren<CheckBox>(root))
        {
            var snap = new CheckBoxSnapshot(cb, cb.Style, cb.IsEnabled);
            snapshots.Add(snap);

            if (printStyle != null)
            {
                // Best: apply print template that renders ticks reliably
                cb.Style = printStyle;
            }
            else if (useDisableFallback)
            {
                // Simple fallback: disabling often forces state to render on print
                cb.IsEnabled = false;
            }
        }

        return snapshots;
    }

    private static void RestoreCheckBoxes(IEnumerable<CheckBoxSnapshot> snapshots)
    {
        foreach (var s in snapshots)
        {
            s.Box.Style = s.OldStyle;
            s.Box.IsEnabled = s.OldIsEnabled;
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

            //UpdateLoadingMessage("Rendering Bill of Lading...");

            //var bitmap = new RenderTargetBitmap((int)pageWidth, (int)pageHeight, 96, 96, PixelFormats.Pbgra32);
            //bitmap.Render(printArea);

            //var image = new Image
            //{
            //    Source = bitmap,
            //    Width = pageWidth,
            //    Height = pageHeight
            //};

            //var fixedPage = new FixedPage
            //{
            //    Width = pageWidth,
            //    Height = pageHeight
            //};
            //FixedPage.SetLeft(image, 0);
            //FixedPage.SetTop(image, 0);
            //fixedPage.Children.Add(image);

            //fixedPage.Measure(new Size(pageWidth, pageHeight));
            //fixedPage.Arrange(new Rect(new Size(pageWidth, pageHeight)));
            //fixedPage.UpdateLayout();

            //var fixedDoc = new FixedDocument();
            //var pageContent = new PageContent();
            //((IAddChild)pageContent).AddChild(fixedPage);
            //fixedDoc.Pages.Add(pageContent);

            //UpdateLoadingMessage("Sending to printer...");


            //printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Bill of Lading");

            await PerformLayoutAsync(printArea, pageWidth, pageHeight);
            //UpdateLoadingMessage("Rendering Bill of Lading...");

            // VECTOR: build a FixedPage from the live visual (no bitmap)
            var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);

            var fixedDoc = new FixedDocument();
            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(fixedPage);
            fixedDoc.Pages.Add(pageContent);

            //UpdateLoadingMessage("Sending to printer...");
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

        var rect = new System.Windows.Shapes.Rectangle
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


    private async Task<FixedPage> CreateFixedPageFromImageAsync(FrameworkElement printArea, double pageWidth, double pageHeight)
    {
        await Task.Yield();

        var bitmap = new RenderTargetBitmap((int)pageWidth, (int)pageHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(printArea);

        var image = new Image
        {
            Source = bitmap,
            Width = pageWidth,
            Height = pageHeight
        };

        var fixedPage = new FixedPage
        {
            Width = pageWidth,
            Height = pageHeight
        };

        FixedPage.SetLeft(image, 0);
        FixedPage.SetTop(image, 0);
        fixedPage.Children.Add(image);

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
