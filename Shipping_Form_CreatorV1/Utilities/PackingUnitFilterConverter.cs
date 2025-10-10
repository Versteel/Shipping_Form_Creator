using Shipping_Form_CreatorV1.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Shipping_Form_CreatorV1.Utilities;

public class PackingUnitFilterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not ObservableCollection<LineItemPackingUnit> allUnits || values[1] is not string selectedView)
        {
            return new ObservableCollection<LineItemPackingUnit>();
        }

        var isAllView = string.Equals(selectedView, "ALL", StringComparison.OrdinalIgnoreCase);

        if (isAllView)
        {
            return allUnits; // If "ALL" is selected, return the original collection
        }

        // Otherwise, return a new filtered list
        var filteredUnits = allUnits.Where(pu => string.Equals(pu.TruckNumber, selectedView, StringComparison.OrdinalIgnoreCase));
        return new ObservableCollection<LineItemPackingUnit>(filteredUnits);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}