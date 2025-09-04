using System.Windows.Controls;
using Shipping_Form_CreatorV1.ViewModels;

namespace Shipping_Form_CreatorV1.Components
{
    public partial class BillOfLading : Page
    {
        private readonly MainViewModel _viewModel;
        public BillOfLading(MainViewModel vm)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel; 
        }
        
        public string TodaysDate => System.DateTime.Now.ToShortDateString();
        public string TotalPieces => _viewModel.AllPiecesTotal.ToString();
        public string TotalWeight => _viewModel.AllWeightTotal.ToString();
    }
}
