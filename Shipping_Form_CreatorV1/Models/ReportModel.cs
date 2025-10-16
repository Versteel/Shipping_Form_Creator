using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shipping_Form_CreatorV1.Models;

public class ReportModel
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <-- VERIFY THIS EXISTS
    public int Id { get; set; }

    public ReportHeader Header { get; set; } = null!;
    public ICollection<LineItem> LineItems { get; set; } = [];
    public virtual ObservableCollection<HandlingUnit> HandlingUnits { get; set; } = [];

}

