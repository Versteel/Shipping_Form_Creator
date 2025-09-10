namespace Shipping_Form_CreatorV1.Models
{
    public class BolSummaryRow
    {
        public string? TypeOfUnit { get; init; }
        public string? CartonOrSkid { get; init; }
        public int TotalPieces { get; init; }
        public int TotalWeight { get; init; }

        public int CartonCount { get; set; }
        public int SkidCount { get; set; }
        public string Class { get; set; }
        public string NMFC { get; set; }
    }
}
