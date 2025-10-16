using System.ComponentModel.DataAnnotations.Schema;

namespace Shipping_Form_CreatorV1.Models;

public class LineItemDetail
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <-- VERIFY THIS EXISTS
    public int Id { get; set; }
    public decimal ModelItem { get; set; }
    public decimal NoteSequenceNumber { get; set; }
    public string? NoteText { get; set; }
    public string? PackingListFlag { get; set; }
    public string? BolFlag { get; set; }

    public int LineItemId { get; set; }
    public LineItem LineItem { get; set; } = null!;
}

