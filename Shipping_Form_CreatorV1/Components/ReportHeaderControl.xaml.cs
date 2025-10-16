using Shipping_Form_CreatorV1.Models;
using System.Windows;
using System.Windows.Controls;

namespace Shipping_Form_CreatorV1.Components;
public partial class ReportHeaderControl : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(ReportHeader), typeof(ReportHeaderControl), new PropertyMetadata(null));

    public static readonly DependencyProperty PageNumberTextProperty =
        DependencyProperty.Register(nameof(PageNumberText), typeof(string), typeof(ReportHeaderControl), new PropertyMetadata(string.Empty));

    public ReportHeader Header
    {
        get => (ReportHeader)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string PageNumberText
    {
        get => (string)GetValue(PageNumberTextProperty);
        set => SetValue(PageNumberTextProperty, value);
    }

    public ReportHeaderControl()
    {
        InitializeComponent();
    }
}