namespace Shipping_Form_CreatorV1.Utilities;

public class Constants
{
    public static readonly Dictionary<string, string> FreightTermCodes = new()
    {
        { "COD", "COD" }, { "COL", "COLLECT" }, { "CPU", "CUSTOMER PICK UP" },
        { "DEL", "DELIVERED PRICING" }, { "DES", "FOB DESTINATION" }, { "DEST", "FOB DESTINATION" },
        { "DNF", "DELIVERED NO FS" }, { "JAS", "FOB JASPER IN" }, { "PPA", "PRE-PAID ADD" },
        { "PPD", "PRE-PAID NO ADD" }, { "3RD", "THIRD PARTY BILLING" }
    };

    public static readonly string[] TruckNumbers =
    [
        "TRUCK 1",
        "TRUCK 2",
        "TRUCK 3",
        "TRUCK 4",
        "TRUCK 5",
        "TRUCK 6",
        "TRUCK 7",
        "TRUCK 8",
        "TRUCK 9",
        "TRUCK 10"
    ];

    public static readonly string[] ViewOptions =
    [
        "ALL",
        "TRUCK 1",
        "TRUCK 2",
        "TRUCK 3",
        "TRUCK 4",
        "TRUCK 5",
        "TRUCK 6",
        "TRUCK 7",
        "TRUCK 8",
        "TRUCK 9",
        "TRUCK 10"
    ];

    public static readonly string[] CartonOrSkidOptions =
    [   
        "SKID",
        "BOX",
        "PACKED WITH LINE ",
        "SINGLE PACK",
    ];

    public static readonly string[] PackingUnitCategories =
    [
        "TABLE TOPS",
        "TABLE LEGS, TUBULAR STL EXC 1 QTR DIAMETER, N-G-T 2 INCH DIAMETER",
        "CHAIRS",
        "CURVARE",
        "IMMIX",
        "OH!",
        "OLLIE",
        "ROVERS, TABLES ALUMINUM STEEL",
        "ELECTRICAL WIRING PLUG",
        "TRANSPORTS",
        "HARDWARE",
        "STRETCHERS",
        "RAILS",
        "KEYBOARDS/CPU HOLDERS",
        "WIRE BASKETS/WIRE RUNNERS",
        "STORAGE CABINET/LIGHT BAR",

    ];

    public static readonly string DITTO_LOGO = "pack://application:,,,/ditto_logo.jpg";
    public static readonly string VERSTEEL_LOGO = "pack://application:,,,/blackoutlinelogo.png";
    public static readonly string PACKING_LIST_FOLDER = @"\\store2\volume1\Packing List\";
    public static readonly string BOL_FOLDER = @"\\store2\volume1\BillofLading\";
    public static readonly string SYNCFUSION_LICENSE_KEY = @"Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdfdHRcRmdfVkJ3X0dWYEk=";

    public static readonly string GHOSTSCRIPT_PATH = @"\\store2\c$\Program Files\gs\gs10.03.1\bin\gswin64c.exe";
    public static readonly string LOG_FILE_PATH = @"\\store2\software\software\ShippingFormsCreator\Logs\DbLog.txt";

    public static readonly string CONNECTION_STRING = "Server=store2,1433;Database=ShippingFormsDb;Integrated Security=SSPI;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;";
}