using Shipping_Form_CreatorV1.Utilities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shipping_Form_CreatorV1.Models;

public class ReportHeader
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <-- VERIFY THIS EXISTS
    public int Id { get; set; }
    public string LogoImagePath { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public int Suffix { get; set; }
    [NotMapped]
    public string OrderNumberWithSuffix => $"{OrderNumber}-{Suffix}";
    public int PageCount { get; set; }
    public string OrdEnterDate { get; set; }
    public string ShipDate { get; set; }
    public string SoldToCustNumber { get; set; }
    public string SoldToCustomerDisplay => $"Sold To: {SoldToCustNumber}";
    public string ShipToCustNumber { get; set; }
    public string ShipToCustomerDisplay => $"Ship To: {ShipToCustNumber}";
    public string SoldToName { get; set; }
    public string SoldToCustAddressLine1 { get; set; }
    public string SoldToCustAddressLine2 { get; set; }
    public string SoldToCustAddressLine3 { get; set; }
    public string SoldToCity { get; set; }
    public string SoldToSt { get; set; }
    public string SoldToZipCode { get; set; }
    public string? SoldToCityStZip => $"{SoldToCity?.Trim()}, {SoldToSt?.Trim()}  {SoldToZipCode?.Trim()}";
    public string ShipToName { get; set; }
    public string ShipToCustAddressLine1 { get; set; }
    public string ShipToCustAddressLine2 { get; set; }
    public string ShipToCustAddressLine3 { get; set; }
    public string ShipToCity { get; set; }
    public string ShipToSt { get; set; }
    public string ShipToZipCode { get; set; }
    public string ShipToCityStZip => $"{ShipToCity?.Trim()}, {ShipToSt?.Trim()}  {ShipToZipCode?.Trim()}";
    public string CustomerPONumber { get; set; }
    public string DueDate { get; set; }
    public string SalesPerson { get; set; }
    public string CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string FreightTerms { get; set; }

    // Navigation properties
    public int ReportModelId { get; set; }
    public ReportModel ReportModel { get; set; } = null!;
}