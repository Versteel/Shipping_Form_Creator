using System.ComponentModel.DataAnnotations.Schema;

namespace Shipping_Form_CreatorV1.Models;

public class LineItemHeader
{
    public int Id { get; set; }
    public decimal LineItemNumber { get; set; }
    public string ProductNumber { get; set; }
    public string ProductDescription { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal PickOrShipQuantity { get; set; }
    public decimal BackOrderQuantity { get; set; }
    [NotMapped]
    public int OrderedQuantityInt => (int)OrderedQuantity;
    [NotMapped]
    public int PickOrShipQuantityInt => (int)PickOrShipQuantity;
    [NotMapped]
    public int BackOrderQuantityInt => (int)BackOrderQuantity;
}

