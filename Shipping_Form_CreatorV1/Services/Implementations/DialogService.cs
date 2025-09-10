using Shipping_Form_CreatorV1.Components.Dialogs;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class DialogService
    {
        public static void ShowErrorDialog(string message) => new InvalidSalesOrderDialog(message).ShowDialog();

    }
}
