using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shipping_Form_CreatorV1.Models
{
    public class PackingListSummaryItem
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int TotalWeight { get; set; }
    }
}
