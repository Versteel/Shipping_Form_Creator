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
using Shipping_Form_CreatorV1.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class PrintService
{
    public List<UserControl> BuildAllPackingListPages(MainViewModel viewModel)
    {
        var pages = new List<UserControl>();
        var report = viewModel.SelectedReport;
        var header = report.Header;

        // Normalize "TABLE LEGS"
        var tableLegUnits = report.LineItems
            .SelectMany(li => li.LineItemPackingUnits)
            .Where(pu => pu.TypeOfUnit == Constants.PackingUnitCategories[1]);

        foreach (var packingUnit in tableLegUnits)
            packingUnit.TypeOfUnit = "TABLE LEGS";

        // Page 1
        if (report.LineItems.Count > 0)
        {
            var pageOne = new PackingListPageOne
            {
                Header = header,
                LineItem = report.LineItems.ToList()[0],
                // Page number text set later in a post-pass
                Details = new ObservableCollection<LineItemDetail>(
                    report.LineItems.ToList()[0].LineItemDetails.ToList())
            };
            pages.Add(pageOne);
        }

        // Page 2+ (if more line items)
        if (report.LineItems.Count > 1)
        {
            const int itemsPerPage = 2; // Adjust as needed

            var lineItems = report.LineItems
                .Where(li => !IsNoteOnly(li))
                .Where(li => li.LineItemDetails.All(d => d.PackingListFlag == "Y"))
                .OrderBy(li => li.LineItemHeader?.LineItemNumber ?? 0)
                .ToList();

            for (var i = 1; i < lineItems.Count; i += itemsPerPage)
            {
                var items = new ObservableCollection<LineItem>(lineItems.Skip(i).Take(itemsPerPage));
                var filteredItems = new ObservableCollection<LineItem>(
                    items.Where(li => li.LineItemDetails.Any(d => d.NoteSequenceNumber < 950)));
                items = new ObservableCollection<LineItem>(filteredItems.Take(filteredItems.Count - 1));

                var pageTwoPlus = new PackingListPageTwoPlus
                {
                    Header = header,
                    Items = items
                    // Page number text set later in a post-pass
                };
                pages.Add(pageTwoPlus);
            }
        }

        // Notes page
        if (viewModel.PackingListNotes is { Count: > 0 })
        {
            var notesPage = new PackingListNotesPage
            {
                Header = header,
                Details = viewModel.PackingListNotes
                // Page number text set later in a post-pass
            };
            pages.Add(notesPage);
        }

        // --- Post-pass: set "Page X of Y" on any page type that exposes a page number property ---
        var total = pages.Count;
        for (int i = 0; i < total; i++)
        {
            var label = $"Page {i + 1} of {total}";
            // Try common property names used by your page controls
            SetStringPropIfExists(pages[i], "PageNumberText", label);
            SetStringPropIfExists(pages[i], "PageNumberTwoPlusText", label);
        }

        return pages;
    }

    static bool IsNoteOnly(LineItem li)
    {
        var h = li.LineItemHeader;
        var noProduct = string.IsNullOrWhiteSpace(h?.ProductNumber);
        var qtyZero = h is { OrderedQuantity: 0m, PickOrShipQuantity: 0m, BackOrderQuantity: 0m };
        return noProduct && qtyZero;
    }

    // Reflection helper so we don't have to special-case each page class
    private static void SetStringPropIfExists(object target, string propertyName, string value)
    {
        var prop = target.GetType().GetProperty(propertyName);
        if (prop is { CanWrite: true } && prop.PropertyType == typeof(string))
            prop.SetValue(target, value);
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

                var fixedPage = new FixedPage { Width = pageWidth, Height = pageHeight };
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

    public BillOfLading BuildBillOfLadingPage(MainViewModel viewModel)
    {
        return new BillOfLading(viewModel);
    }

    public void PrintBillOfLading(Page bolPage)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        var pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 816;
        var pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1056;

        var fixedDoc = new FixedDocument();

        var printArea = bolPage.FindName("BillOfLadingPrintArea") as FrameworkElement;
        if (printArea == null)
        {
            MessageBox.Show("BillOfLadingPrintArea not found.");
            return;
        }

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

        var fixedPage = new FixedPage { Width = pageWidth, Height = pageHeight };
        FixedPage.SetLeft(rect, 0);
        FixedPage.SetTop(rect, 0);
        fixedPage.Children.Add(rect);

        fixedPage.Measure(new Size(pageWidth, pageHeight));
        fixedPage.Arrange(new Rect(new Size(pageWidth, pageHeight)));
        fixedPage.UpdateLayout();

        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(fixedPage);
        fixedDoc.Pages.Add(pageContent);

        printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Bill of Lading");
    }


}
