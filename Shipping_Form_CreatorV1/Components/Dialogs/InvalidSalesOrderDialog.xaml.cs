using System.Windows;

namespace Shipping_Form_CreatorV1.Components.Dialogs
{
    /// <summary>
    /// Interaction logic for InvalidSalesOrderDialog.xaml
    /// </summary>
    public partial class InvalidSalesOrderDialog : Window
    {
        public string MessageText { get; set; } = string.Empty;

        public InvalidSalesOrderDialog(string messageText)
        {
            InitializeComponent();
            
            MessageText = messageText;

            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
