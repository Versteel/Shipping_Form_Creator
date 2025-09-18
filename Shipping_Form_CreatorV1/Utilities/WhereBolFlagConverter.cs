using Shipping_Form_CreatorV1.Models;
using System.Globalization;
using System.Windows.Data;

namespace Shipping_Form_CreatorV1.Utilities
{
    public class BolFlagFilterConverter : IValueConverter
    {
        public string FlagValue { get; set; } = "Y";

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<LineItem> lineItems)
            {
                return lineItems
                    .SelectMany(li => li.LineItemDetails)
                    .Where(d => string.Equals(d.BolFlag, FlagValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Array.Empty<LineItemDetail>();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
