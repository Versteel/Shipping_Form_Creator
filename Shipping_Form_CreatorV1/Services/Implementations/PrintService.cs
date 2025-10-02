using PdfSharp.Xps;
using PdfSharp.Xps;
using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;
using Syncfusion.Windows.Controls.Notification;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Utility.Hocr.Enums;
using Utility.Hocr.Pdf;
using Path = System.IO.Path;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class PrintService
{
    private SfBusyIndicator? _busyIndicator;
    private Window? _loadingWindow;
    private const string PdfPrinterDriveName = "Microsoft Print To PDF";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType;
    }

    private void ConvertXpsToSearchablePdf(byte[] xpsBytes, string outputFilePath)
    {
        // 1. Create a temporary file path for the source XPS document 
        string tempXpsFilePath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".xps");
        try
        {
            // 2. Write your in-memory XPS data to the temporary file 
            File.WriteAllBytes(tempXpsFilePath, xpsBytes);
            // 3. Call the converter using the temporary file path as the source 
            XpsConverter.Convert(tempXpsFilePath, outputFilePath, 0);
        }
        finally
        {
            // 4. Ensure the temporary file is deleted, even if an error occurs 
            if (File.Exists(tempXpsFilePath))
            {
                File.Delete(tempXpsFilePath);
            }
        }
    }
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
                .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
                .ToList();

            // This part remains the same
            var tableLegUnits = lineItems
                .SelectMany(li => li.LineItemPackingUnits)
                .Where(pu => pu.TypeOfUnit == Constants.PackingUnitCategories[1]);
            foreach (var packingUnit in tableLegUnits)
                packingUnit.TypeOfUnit = "TABLE LEGS";

            if (lineItems.Count > 0)
            {
                UpdateLoadingMessage("Creating page 1...");
                var firstLineItem = lineItems[0];

                var pageOne = new PackingListPageOne
                {
                    Header = header,
                    LineItem = firstLineItem,
                    Details = new ObservableCollection<LineItemDetail>(GetDetailsFor(firstLineItem)),
                    PackingUnits = new ObservableCollection<LineItemPackingUnit>(firstLineItem.LineItemPackingUnits),
                    IsPrinting = true
                };
                pages.Add(pageOne);
                await Task.Delay(25);
            }

            var remainingItems = lineItems.Skip(1).ToList();

            const int maxDetailsPerPage = 35;
            var currentPageItems = new List<LineItem>();
            var currentDetailsCount = 0;

            foreach (var item in remainingItems)
            {
                var detailsCount = GetDetailsFor(item).Count;

                if (currentDetailsCount + detailsCount > maxDetailsPerPage && currentPageItems.Count > 0)
                {
                    var pageTwoPlus = new PackingListPageTwoPlus
                    {
                        Header = header,
                        Items = new ObservableCollection<LineItem>(currentPageItems),
                        IsPrinting = true
                    };
                    pages.Add(pageTwoPlus);
                    await Task.Delay(25);

                    currentPageItems = [];
                    currentDetailsCount = 0;
                }

                currentPageItems.Add(item);
                currentDetailsCount += detailsCount;
            }

            if (currentPageItems.Count > 0)
            {
                var pageTwoPlus = new PackingListPageTwoPlus
                {
                    Header = header,
                    Items = new ObservableCollection<LineItem>(currentPageItems),
                    IsPrinting = true
                };
                pages.Add(pageTwoPlus);
                await Task.Delay(25);
            }

            if (viewModel.PackingListNotes is { Count: > 0 })
            {
                UpdateLoadingMessage("Creating notes page...");
                var notesPage = new PackingListNotesPage
                {
                    Header = header,
                    Details = viewModel.PackingListNotes,
                    IsPrinting = true
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

    private static List<LineItemDetail> GetDetailsFor(LineItem li) =>
    [
        .. li.LineItemDetails
            .Where(d => !string.IsNullOrWhiteSpace(d.NoteText))
            .Where(d => d.PackingListFlag?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) == true)
            .Where(d => d.NoteText is not null &&
                        !d.NoteText.Contains("OPTIONS BEGIN") &&
                        !d.NoteText.Contains("OPTIONS END"))
            .OrderBy(d => d.NoteSequenceNumber)
    ];

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

    public async Task ConvertPackingListsToPdf(MainViewModel viewModel)
    {
        try
        {
            ShowLoadingIndicator("Converting Packing Lists to PDF...");
            var fixedDoc = new FixedDocument();
            foreach (var report in viewModel.SearchByDateResults)
            {
                viewModel.SelectedReport = report;
                var pages = await BuildAllPackingListPages(viewModel);

                const double pageWidth = 816;
                const double pageHeight = 1056;

                var totalPages = pages.Count;

                for (var i = 0; i < totalPages; i++)
                {
                    var page = pages[i];
                    UpdateLoadingMessage($"Rendering page {i + 1} of {totalPages} for {report.Header.OrderNumber}...");

                    var printArea = await GetPrintAreaAsync(page);
                    if (printArea == null) continue;

                    await PerformLayoutAsync(printArea, pageWidth, pageHeight);
                    var fixedPage = await CreateFixedPageFromVisualAsync(printArea, pageWidth, pageHeight);

                    var pageContent = new PageContent();
                    ((IAddChild)pageContent).AddChild(fixedPage);
                    fixedDoc.Pages.Add(pageContent);
                }
            }

            var ms = new MemoryStream();
            var package = Package.Open(ms, FileMode.Create);
            var doc = new XpsDocument(package);
            var writer = XpsDocument.CreateXpsDocumentWriter(doc);
            writer.Write(fixedDoc.DocumentPaginator);
            doc.Close();
            package.Close();

            var bytes = ms.ToArray();
            ms.Dispose();

            var outputDir = System.IO.Path.Combine(Constants.PACKING_LIST_FOLDER, DateTime.Now.ToShortDateString().Replace('/', '.') + "-TEST.pdf");
            //PrintXpsToPdf(bytes, outputDir, "PackingList");
            ShowLoadingIndicator("Saving searchable PDF...");
            ConvertXpsToSearchablePdf(bytes, outputDir);
        }
        catch (Exception ex)
        {

            throw;
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    public async Task ConvertBillOfLadingsToPdf(MainViewModel viewModel)
    {
        try
        {
            ShowLoadingIndicator("Converting Bill of Ladings to PDF...");
            var fixedDocument = new FixedDocument();
            foreach (var report in viewModel.SearchByDateResults)
            {
                viewModel.SelectedReport = report;
                var billOfLading = BuildBillOfLadingPage(viewModel);

                const double pageWidth = 816;
                const double pageHeight = 1056;

                UpdateLoadingMessage($"Rendering Bill of Lading for {report.Header.OrderNumber}...");

                try
                {
                    (billOfLading as dynamic).IsPrinting = true;
                }
                catch
                {
                    // ignored
                }

                await PerformLayoutAsync(billOfLading, pageWidth, pageHeight);
                var fixedPage = await CreateFixedPageFromVisualAsync(billOfLading, pageWidth, pageHeight);
                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
            }

            var ms = new MemoryStream();
            var package = Package.Open(ms, FileMode.Create);
            var doc = new XpsDocument(package);
            var writer = XpsDocument.CreateXpsDocumentWriter(doc);
            writer.Write(fixedDocument.DocumentPaginator);
            doc.Close();
            package.Close();

            var bytes = ms.ToArray();
            var outputDir = System.IO.Path.Combine(Constants.BOL_FOLDER, DateTime.Now.ToShortDateString().Replace('/', '-') + "-TEST.pdf");
            //PrintXpsToPdf(bytes, outputDir, "BillOfLading");
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            HideLoadingIndicator();
        }
    }

    public async Task ConvertSearchResultsToPdf(MainViewModel viewModel)
    {
        try
        {
            await ConvertPackingListsToPdf(viewModel);
            //await ConvertBillOfLadingsToPdf(viewModel);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during PDF conversion: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            MessageBox.Show("Packing Lists & Bill of Ladings process successfully.");
        }
    }

    //public void PrintXpsToPdf(byte[] bytes, string outputFilePath, string documentTitle)
    //{
    //    // Get Microsoft Print to PDF print queue
    //    var pdfPrintQueue = GetMicrosoftPdfPrintQueue();

    //    // Copy byte array to unmanaged pointer
    //    var ptrUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
    //    Marshal.Copy(bytes, 0, ptrUnmanagedBytes, bytes.Length);

    //    // Prepare document info
    //    var di = new DOCINFOA
    //    {
    //        pDocName = documentTitle,
    //        pOutputFile = outputFilePath,
    //        pDataType = "RAW"
    //    };

    //    // Print to PDF
    //    var errorCode = SendBytesToPrinter(pdfPrintQueue.Name, ptrUnmanagedBytes, bytes.Length, di, out var jobId);

    //    ShowLoadingIndicator("Preparing PDF....");
    //    // Free unmanaged memory
    //    Marshal.FreeCoTaskMem(ptrUnmanagedBytes);

    //    // Check if job in error state (for example not enough disk space)
    //    var jobFailed = false;
    //    try
    //    {
    //        var pdfPrintJob = pdfPrintQueue.GetJob(jobId);
    //        if (pdfPrintJob.IsInError)
    //        {
    //            jobFailed = true;
    //            pdfPrintJob.Cancel();
    //        }
    //    }
    //    catch
    //    {
    //        // If job succeeds, GetJob will throw an exception. Ignore it. 
    //    }
    //    finally
    //    {
    //        pdfPrintQueue.Dispose();
    //        HideLoadingIndicator();
    //    }

    //    if (errorCode > 0 || jobFailed)
    //    {
    //        try
    //        {
    //            if (File.Exists(outputFilePath))
    //            {
    //                File.Delete(outputFilePath);
    //            }
    //        }
    //        catch
    //        {
    //            // ignored
    //        }
    //    }

    //    if (errorCode > 0)
    //    {
    //        throw new Exception($"Printing to PDF failed. Error code: {errorCode}.");
    //    }

    //    if (jobFailed)
    //    {
    //        throw new Exception("PDF Print job failed.");
    //    }
    //}

    //private static int SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount, DOCINFOA documentInfo, out int jobId)
    //{
    //    jobId = 0;
    //    var dwWritten = 0;
    //    var success = false;

    //    if (OpenPrinter(szPrinterName.Normalize(), out var hPrinter, IntPtr.Zero))
    //    {
    //        jobId = StartDocPrinter(hPrinter, 1, documentInfo);
    //        if (jobId > 0)
    //        {
    //            if (StartPagePrinter(hPrinter))
    //            {
    //                success = WritePrinter(hPrinter, pBytes, dwCount, out dwWritten);
    //                EndPagePrinter(hPrinter);
    //            }

    //            EndDocPrinter(hPrinter);
    //        }

    //        ClosePrinter(hPrinter);
    //    }

    //    // TODO: The other methods such as OpenPrinter also have return values. Check those?

    //    if (success == false)
    //    {
    //        return Marshal.GetLastWin32Error();
    //    }

    //    return 0;
    //}

    private static PrintQueue GetMicrosoftPdfPrintQueue()
    {
        PrintQueue? pdfPrintQueue = null;

        try
        {
            using (var printServer = new PrintServer())
            {
                var flags = new[] { EnumeratedPrintQueueTypes.Local };

                pdfPrintQueue = printServer.GetPrintQueues(flags).FirstOrDefault(lq => lq.QueueDriver.Name == PdfPrinterDriveName);
            }

            if (pdfPrintQueue == null)
            {
                throw new Exception($"Could not find printer with driver name: {PdfPrinterDriveName}");
            }

            return !pdfPrintQueue.IsXpsDevice ? throw new Exception($"PrintQueue '{pdfPrintQueue.Name}' does not understand XPS page description language.") : pdfPrintQueue;
        }
        catch
        {
            pdfPrintQueue?.Dispose();
            throw;
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