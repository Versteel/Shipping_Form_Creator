using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Odbc;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class OdbcService : IOdbcService
{
    private const string CONNECTION_STRING =
        "Driver={iSeries Access ODBC Driver};System=192.168.1.2;Uid=FRN032;Pwd=FRN032;";

    // Query for the main header information
    private const string GET_HEADER_QUERY = """
                                             SELECT
                                                 spp.spname,
                                                 ohp.ohord#,
                                                 ohp.ohlsuf,
                                                 ohp.ohstky,
                                                 ohp.ohspo#,
                                                 ohp.ohentm,
                                                 ohp.ohenty,
                                                 ohp.ohreqm,
                                                 ohp.ohreqy,
                                                 o4p.odinst,
                                                 o4p.o4name,
                                                 o4p.o4adr2,
                                                 o4p.o4adr3,
                                                 o4p.o4adr4,
                                                 o4p.o4city,
                                                 o4p.o4st,
                                                 o4p.o4zip,
                                                 o4p.odshpm,
                                                 o4p.odshpy,
                                                 cm1p.c1stky,
                                                 cm1p.cmname,
                                                 cm1p.cmlne1,
                                                 cm1p.cmlne2,
                                                 cm1p.cmlne3,
                                                 cm1p.cmcity,
                                                 cm1p.cmst,
                                                 cm1p.cmzip,
                                                 Lpad(SUBSTRING(Lpad(ohp.ohentm, 4, '0'), 1, 2), 2, '0')
                                                     || '/' || Lpad(SUBSTRING(Lpad(ohp.ohentm, 4, '0'), 3, 2), 2, '0')
                                                     || '/' || '20' || Lpad(ohp.ohenty, 2, '0') AS OrderEnteredDate,
                                                 Lpad(SUBSTRING(Lpad(o4p.odshpm, 4, '0'), 1, 2), 2, '0')
                                                     || '/' || Lpad(SUBSTRING(Lpad(o4p.odshpm, 4, '0'), 3, 2), 2, '0')
                                                     || '/' || '20' || Lpad(o4p.odshpy, 2, '0') AS ShipDate,
                                                 Lpad(SUBSTRING(Lpad(ohp.ohreqm, 4, '0'), 1, 2), 2, '0')
                                                     || '/' || Lpad(SUBSTRING(Lpad(ohp.ohreqm, 4, '0'), 3, 2), 2, '0')
                                                     || '/' || '20' || Lpad(ohp.ohreqy, 2, '0') AS DueDate,
                                                 nfterms.nfdesc AS FreightTermsDesc,
                                                 nfcarrier.nfdesc AS CarrierDesc
                                             FROM s107ce82.frndta032.ohp AS ohp
                                             JOIN s107ce82.frndta032.spp AS spp
                                               ON ohp.ohsm = spp.spsm
                                             LEFT JOIN s107ce82.frndta032.o4p AS o4p
                                               ON o4p.o4ord# = ohp.ohord#
                                              AND o4p.o4sufx = ohp.ohlsuf
                                             LEFT JOIN s107ce82.frndta032.cm1p AS cm1p
                                               ON cm1p.c1stky = ohp.ohstky
                                             LEFT JOIN s107ce82.frndta032.nfp AS nfterms
                                               ON nfterms.nftyp = 'FT' AND nfterms.nfnumb = o4p.odfrtt
                                             LEFT JOIN s107ce82.frndta032.nfp AS nfcarrier
                                               ON nfcarrier.nftyp = 'CC' AND nfcarrier.nfnumb = o4p.odcarr
                                             WHERE ohp.ohord# = ?
                                               AND ohp.ohlsuf = ?
                                             FOR FETCH ONLY
                                             
                                             """;

    // Query for line item details
    private const string GET_LINE_ITEMS_QUERY = """
                                                 SELECT
                                                     o6p.o6item,
                                                     o6p.odpn,
                                                     o6p.odorgq,
                                                     o6p.odshpq,
                                                     o6p.odbalq,
                                                     pmp.pmdesc AS PartDescription
                                                 FROM s107ce82.frndta032.o6p AS o6p
                                                 LEFT JOIN s107ce82.frndta032.pmp AS pmp
                                                   ON pmp.pmpart = o6p.odpn
                                                 WHERE o6p.o6ord# = ?
                                                   AND o6p.o6sufx = ?
                                                 ORDER BY o6p.o6item
                                                 FOR FETCH ONLY
                                                 
                                                 """;

    // Query for notes without a specific line item
    private const string GET_ORDER_NOTES_QUERY = """
                                                SELECT 
                                                    CAST(FLOOR(o5p.o5item) AS INTEGER) AS ModelItem,  
                                                    o5p.o5item,                                      
                                                    o5p.o5op        AS NoteSeq,
                                                    o5p.odtext,
                                                    o5p.odprt2,
                                                    o5p.odprt3
                                                FROM s107ce82.frndta032.o5p AS o5p
                                                WHERE o5p.o5ord# = ?
                                                  AND o5p.o5sufx = ?
                                                  AND o5p.odtext IS NOT NULL
                                                  AND UPPER(o5p.odtext) NOT LIKE 'OPTIONS BEGIN%'
                                                  AND UPPER(o5p.odtext) NOT LIKE 'OPTIONS END%'
                                                ORDER BY o5p.o5item, o5p.o5op
                                                FOR FETCH ONLY
                                                                
                                                """;

    // Query to get all order headers by ship date
    private const string GET_ALL_HEADERS_BY_SHIPDATE_QUERY = """
                                             SELECT DISTINCT
                                                    ohp.ohord#,
                                                    ohp.ohlsuf
                                             FROM      s107ce82.frndta032.ohp AS ohp
                                             JOIN      s107ce82.frndta032.o4p AS o4p
                                             ON        o4p.o4ord# = ohp.ohord#
                                             AND       o4p.o4sufx = ohp.ohlsuf
                                             WHERE     o4p.odshpm = ?
                                             AND       o4p.odshpy = ?
                                             FOR FETCH ONLY
                                             """;



    // ===== Safe readers =====
    private static string GetSafeString(DbDataReader r, string col)
    {
        var o = r.GetOrdinal(col);
        return r.IsDBNull(o) ? string.Empty : r.GetString(o).Trim();
    }

    private static int GetSafeInt(DbDataReader r, string col, int @default = 0)
    {
        var o = r.GetOrdinal(col);
        if (r.IsDBNull(o)) return @default;

        var v = r.GetValue(o);
        return v switch
        {
            int i => i,
            long l => checked((int)l),
            decimal d => decimal.ToInt32(d),
            short s => s,
            byte b => b,
            double d2 => (int)d2,
            _ => Convert.ToInt32(v)
        };
    }

    private static decimal? GetNullableDecimal(DbDataReader r, string col)
    {
        var o = r.GetOrdinal(col);
        if (r.IsDBNull(o)) return null;
        var v = r.GetValue(o);
        return v is decimal d ? d : Convert.ToDecimal(v);
    }

    private static decimal GetSafeDecimal(DbDataReader r, string col, decimal @default = 0m)
        => GetNullableDecimal(r, col) ?? @default;

    // ===== Public API =====
    public async Task<List<ReportModel>> GetShippedOrdersByDate(DateTime shipDate, CancellationToken ct = default)
    {
        var results = new List<ReportModel>();

        await using var connection = new OdbcConnection(CONNECTION_STRING);
        await connection.OpenAsync(ct);

        await using var cmd = new OdbcCommand(GET_ALL_HEADERS_BY_SHIPDATE_QUERY, connection);
        cmd.Parameters.Add("@ODSHPM", OdbcType.Int).Value = shipDate.Month * 100 + shipDate.Day;
        cmd.Parameters.Add("@ODSHPY", OdbcType.Int).Value = shipDate.Year % 100;

        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            int orderNum = GetSafeInt(rdr, "OHORD#");
            int suffix = GetSafeInt(rdr, "OHLSUF");

            var rpt = await GetReportAsync(orderNum, suffix, ct);
            if (rpt != null)
                results.Add(rpt);
        }

        return results;
    }


   public async Task<ReportModel?> GetReportAsync(int orderNumber, int suffix, CancellationToken ct = default)
{
    var rpt = new ReportModel
    {
        LineItems = new ObservableCollection<LineItem>()
    };

    await using var connection = new OdbcConnection(CONNECTION_STRING);
    await connection.OpenAsync(ct);

    // Step 1: Get the single header record
    await using var headerCommand = new OdbcCommand(GET_HEADER_QUERY, connection);
    headerCommand.Parameters.Add("@OHORD#", OdbcType.Int).Value = orderNumber;
    headerCommand.Parameters.Add("@OHLSUF", OdbcType.Int).Value = suffix;

    await using var headerReader = await headerCommand.ExecuteReaderAsync(ct);
    if (await headerReader.ReadAsync(ct))
    {
        rpt.Header = new ReportHeader
        {
            OrderNumber = GetSafeInt(headerReader, "OHORD#"),
            Suffix = GetSafeInt(headerReader, "OHLSUF"),
            OrdEnterDate = GetSafeString(headerReader, "OrderEnteredDate"),
            ShipDate = GetSafeString(headerReader, "ShipDate"),
            SoldToCustNumber = GetSafeString(headerReader, "OHSTKY"),
            SoldToName = GetSafeString(headerReader, "O4NAME"),
            SoldToCustAddressLine1 = GetSafeString(headerReader, "O4ADR2"),
            SoldToCustAddressLine2 = GetSafeString(headerReader, "O4ADR3"),
            SoldToCustAddressLine3 = GetSafeString(headerReader, "O4ADR4"),
            SoldToCity = GetSafeString(headerReader, "O4CITY"),
            SoldToSt = GetSafeString(headerReader, "O4ST"),
            SoldToZipCode = GetSafeString(headerReader, "O4ZIP"),
            ShipToCustNumber = GetSafeString(headerReader, "C1STKY"),
            ShipToName = GetSafeString(headerReader, "CMNAME"),
            ShipToCustAddressLine1 = GetSafeString(headerReader, "CMLNE1"),
            ShipToCustAddressLine2 = GetSafeString(headerReader, "CMLNE2"),
            ShipToCustAddressLine3 = GetSafeString(headerReader, "CMLNE3"),
            ShipToCity = GetSafeString(headerReader, "CMCITY"),
            ShipToSt = GetSafeString(headerReader, "CMST"),
            ShipToZipCode = GetSafeString(headerReader, "CMZIP"),
            CustomerPONumber = GetSafeString(headerReader, "OHSPO#"),
            DueDate = GetSafeString(headerReader, "DueDate"),
            SalesPerson = GetSafeString(headerReader, "SPNAME"),
            CarrierName = GetSafeString(headerReader, "CarrierDesc"),
            FreightTerms = GetSafeString(headerReader, "FreightTermsDesc"),
            TrackingNumber = GetSafeString(headerReader, "ODINST")
        };
    }
    else
    {
        return null; // Header not found
    }

    // Step 2: Get line items
    await using var lineItemCommand = new OdbcCommand(GET_LINE_ITEMS_QUERY, connection);
    lineItemCommand.Parameters.Add("@O6ORD#", OdbcType.Int).Value = orderNumber;
    lineItemCommand.Parameters.Add("@O6SUFX", OdbcType.Int).Value = suffix;

    await using var lineItemReader = await lineItemCommand.ExecuteReaderAsync(ct);

    var lineItemMap = new Dictionary<decimal, LineItem>();

    while (await lineItemReader.ReadAsync(ct))
    {
        var itemNumber = GetSafeDecimal(lineItemReader, "O6ITEM");

        if (!lineItemMap.ContainsKey(itemNumber))
        {
            var lih = new LineItemHeader
            {
                LineItemNumber = itemNumber,
                ProductNumber = GetSafeString(lineItemReader, "ODPN"),
                ProductDescription = GetSafeString(lineItemReader, "PartDescription"),
                OrderedQuantity = GetSafeDecimal(lineItemReader, "ODORGQ"),
                PickOrShipQuantity = GetSafeDecimal(lineItemReader, "ODSHPQ"),
                BackOrderQuantity = GetSafeDecimal(lineItemReader, "ODBALQ"),
            };
            var li = new LineItem
            {
                LineItemHeader = lih,
                LineItemDetails = new ObservableCollection<LineItemDetail>()
            };
            lineItemMap[itemNumber] = li;
            rpt.LineItems.Add(li);
        }
    }

    // Step 3: Get "floating" notes
    await using var notesCommand = new OdbcCommand(GET_ORDER_NOTES_QUERY, connection);
    notesCommand.Parameters.Add("@O5ORD#", OdbcType.Int).Value = orderNumber;
    notesCommand.Parameters.Add("@O5SUFX", OdbcType.Int).Value = suffix;

    await using var notesReader = await notesCommand.ExecuteReaderAsync(ct);

    var floatingNotesMap = new Dictionary<decimal, LineItem>();

    while (await notesReader.ReadAsync(ct))
    {
        var modelItem = GetSafeInt(notesReader, "ModelItem");
        var subItem = GetSafeDecimal(notesReader, "O5ITEM");

        var detail = new LineItemDetail
        {
            ModelItem = modelItem,
            NoteSequenceNumber = GetSafeDecimal(notesReader, "NoteSeq"),
            NoteText = GetSafeString(notesReader, "ODTEXT"),
            PackingListFlag = GetSafeString(notesReader, "ODPRT2"),
            BolFlag = GetSafeString(notesReader, "ODPRT3")
        };

        if (lineItemMap.TryGetValue(modelItem, out var li))
        {
            li.LineItemDetails.Add(detail);
        }
        else
        {
            if (!floatingNotesMap.ContainsKey(modelItem))
            {
                var lih = new LineItemHeader
                {
                    LineItemNumber = modelItem,
                    ProductNumber = string.Empty,
                    ProductDescription = modelItem switch
                    {
                        0 => "ORDER NOTES",
                        950 => "SHIPPING / BOL NOTES",
                        _ => "NOTES"
                    },
                    OrderedQuantity = 0m,
                    PickOrShipQuantity = 0m,
                    BackOrderQuantity = 0m,
                };
                var liNew = new LineItem
                {
                    LineItemHeader = lih,
                    LineItemDetails = new ObservableCollection<LineItemDetail>()
                };
                floatingNotesMap[modelItem] = liNew;
                rpt.LineItems.Add(liNew);
            }
            floatingNotesMap[modelItem].LineItemDetails.Add(detail);
        }
    }

    rpt.LineItems = new ObservableCollection<LineItem>(rpt.LineItems.OrderBy(li => li.LineItemHeader.LineItemNumber));

    return rpt;
}

}