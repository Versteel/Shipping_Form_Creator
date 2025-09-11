using System.Windows;
using System.Windows.Documents;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for PrintPreviewWindow.xaml
    /// </summary>
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow()
        {
            InitializeComponent();
        }

        public IDocumentPaginatorSource Document
        {
            get => _viewer.Document;
            set => _viewer.Document = value;
        }
    }
}
