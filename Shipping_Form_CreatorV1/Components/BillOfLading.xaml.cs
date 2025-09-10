using System.Windows.Controls;
using Shipping_Form_CreatorV1.Utilities;
using Shipping_Form_CreatorV1.ViewModels;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class BillOfLading : Page
    {
        public BillOfLading(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.SelectedReport.Header.LogoImagePath = vm.IsDittoUser ? Constants.DITTO_LOGO : Constants.VERSTEEL_LOGO;
        }
        
        public static string TodaysDate => DateTime.Now.ToShortDateString();
    }
}
