using System.Windows;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class DialogService
    {
        public static void ShowErrorDialog(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
