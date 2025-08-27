namespace Shipping_Form_CreatorV1.Utilites;

public class Constants
{
    public static readonly Dictionary<string, string> FreightTermCodes = new()
    {
        { "COD", "COD" }, { "COL", "COLLECT" }, { "CPU", "CUSTOMER PICK UP" },
        { "DEL", "DELIVERED PRICING" }, { "DES", "FOB DESTINATION" }, { "DEST", "FOB DESTINATION" },
        { "DNF", "DELIVERED NO FS" }, { "JAS", "FOB JASPER IN" }, { "PPA", "PRE-PAID ADD" },
        { "PPD", "PRE-PAID NO ADD" }, { "3RD", "THIRD PARTY BILLING" }
    };

    public static readonly string[] CartonOrSkidOptions =
    [
        "Carton",
        "Skid",
        "Packed with line "
    ];

    public static readonly string DITTO_LOGO = "pack://application:,,,/ditto_logo.jpg";
    public static readonly string VERSTEEL_LOGO = "pack://application:,,,/blackoutlinelogo.png";
}