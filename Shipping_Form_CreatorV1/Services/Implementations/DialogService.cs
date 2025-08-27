using Shipping_Form_CreatorV1.Components.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shipping_Form_CreatorV1.Services.Implementations
{
    public class DialogService
    {
        public void ShowErrorDialog(string message) => new InvalidSalesOrderDialog(message).ShowDialog();

    }
}
