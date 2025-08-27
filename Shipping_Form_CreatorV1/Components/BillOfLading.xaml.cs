using Shipping_Form_CreatorV1.ViewModels;
using System.Windows.Controls;

namespace Shipping_Form_CreatorV1.Components
{
    /// <summary>
    /// Interaction logic for BillOfLading.xaml
    /// </summary>
    public partial class BillOfLading : Page
    {
        private readonly MainViewModel _viewModel;

        public BillOfLading(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
        }
    }
}
