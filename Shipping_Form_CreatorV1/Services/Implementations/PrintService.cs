using Shipping_Form_CreatorV1.Components;
using Shipping_Form_CreatorV1.ViewModels;
using Shipping_Form_CreatorV1.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class PrintService
{
    public List<UserControl> BuildAllPackingListPages(MainViewModel viewModel)
    {
        var pages = new List<UserControl>();
        var report = viewModel.SelectedReport;
        var header = report.Header;

        // Page 1
        if (report.LineItems.Count > 0)
        {
            var pageOne = new PackingListPageOne
            {
                Header = header,
                LineItem = report.LineItems.ToList()[0],
                PageNumberText = "Page 1",
                Details = new ObservableCollection<LineItemDetail>(
                    report.LineItems.ToList()[0].LineItemDetails.ToList())
            };
            pages.Add(pageOne);
        }

        // Page 2+ (if more line items)
        if (report.LineItems.Count > 1)
        {
            const int itemsPerPage = 2; // Adjust as needed
            var pageNum = 2;


            var lineItems = report.LineItems
                .Where(li => !IsNoteOnly(li))
                .Where(li => li.LineItemDetails.All(d => d.PackingListFlag == "Y"))
                .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
                .ToList();



            for (var i = 1; i < lineItems.Count; i += itemsPerPage)
            {
                var items = new ObservableCollection<LineItem>(
                    lineItems.Skip(i).Take(itemsPerPage)
                );
                var filteredItems = new ObservableCollection<LineItem>([.. items.Where(li => li.LineItemDetails.Any(d => d.NoteSequenceNumber < 950))]);
                items = new ObservableCollection<LineItem>(filteredItems.Take(filteredItems.Count - 1));
                var pageTwoPlus = new PackingListPageTwoPlus
                {
                    Header = header,
                    Items = items,
                    PageNumberTwoPlusText = $"Page {pageNum++}"
                };
                pages.Add(pageTwoPlus);
            }
        }

        // Notes page
        if (viewModel.PackingListNotes is { Count: <= 0 }) return pages;
        var notesPage = new PackingListNotesPage
        {
            Header = header,
            Details = viewModel.PackingListNotes,
            PageNumberText = $"Page {pages.Count + 1}"
        };
        pages.Add(notesPage);

        return pages;
    }

    static bool IsNoteOnly(LineItem li)
    {
        var h = li.LineItemHeader;
        var noProduct = string.IsNullOrWhiteSpace(h?.ProductNumber);
        var qtyZero = h is { OrderedQuantity: 0m, PickOrShipQuantity: 0m, BackOrderQuantity: 0m };
        return noProduct && qtyZero;
    }

    public void PrintPackingListPages(List<UserControl>? pages)
    {
        if (pages == null || pages.Count == 0)
            return;

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        var pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 816;  // 8.5" @96dpi
        var pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1056; // 11"  @96dpi

        var fixedDoc = new FixedDocument();

        try
        {
            foreach (var page in pages)
            {
                (page as dynamic).IsPrinting = true;

                var printArea = page.FindName("PackingListPage1PrintArea") as FrameworkElement ??
                                page.FindName("PackingListPage2PlusPrintArea") as FrameworkElement ??
                                page.FindName("PackingListNotesPage1PrintArea") as FrameworkElement;
                if (printArea == null) continue;

                if (double.IsNaN(printArea.Width)) printArea.Width = pageWidth;
                if (double.IsNaN(printArea.Height)) printArea.Height = pageHeight;
                printArea.Measure(new Size(printArea.Width, printArea.Height));
                printArea.Arrange(new Rect(0, 0, printArea.Width, printArea.Height));
                printArea.UpdateLayout();

                var brush = new VisualBrush(printArea)
                {
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                var rect = new Rectangle
                {
                    Width = pageWidth,
                    Height = pageHeight,
                    Fill = brush
                };

                var fixedPage = new FixedPage
                {
                    Width = pageWidth,
                    Height = pageHeight
                };
                FixedPage.SetLeft(rect, 0);
                FixedPage.SetTop(rect, 0);
                fixedPage.Children.Add(rect);

                fixedPage.Measure(new Size(pageWidth, pageHeight));
                fixedPage.Arrange(new Rect(new Size(pageWidth, pageHeight)));
                fixedPage.UpdateLayout();

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);
            }
        }
        finally
        {
            foreach (var page in pages)
            {
                //(page as dynamic).IsPrinting = false;
            }
        }
        
        printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Packing List");
    }
}