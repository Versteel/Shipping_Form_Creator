using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class OdbcService : IOdbcService
{
    private const string CONNECTION_STRING =
        "Driver={iSeries Access ODBC Driver};System=192.168.1.2;Uid=FRN032;Pwd=FRN032;";

    // Combined query: FRENOT (notes) and FREDTL (detail lines) are LEFT JOINs
    private const string COMBINED_QUERY = """
                                              SELECT
                                                  SPP.SPNAME,
                                                  FREHDR.*,
                                                  FREDTL.*,
                                                  FRENOT.NTMITM,
                                                  FRENOT.NTSEQ#,
                                                  FRENOT.NTTEXT,
                                                  FRENOT.NTPRT1,
                                                  FRENOT.NTPRT2,
                                                  FRENOT.NTPRT3,
                                                  FRENOT.NTPRT4,
                                                  FRENOT.NTCMNT,
                                                  FRENOT.NTGRP5
                                              FROM S107CE82.FRNDTA032.OHP OHP
                                              JOIN S107CE82.FRNDTA032.SPP SPP
                                                  ON OHP.OHSM = SPP.SPSM
                                              JOIN S107CE82.FRNDTA032.FREHDR FREHDR
                                                  ON OHP.OHORD# = FREHDR.HDORD#
                                              LEFT JOIN S107CE82.FRNDTA032.FRENOT FRENOT
                                                  ON FREHDR.HDORD# = FRENOT.NTORD#
                                              LEFT JOIN S107CE82.FRNDTA032.FREDTL FREDTL
                                                  ON FREDTL.DTORD# = FREHDR.HDORD#
                                                 AND FREDTL.DTLSUF = FREHDR.HDLSUF
                                                 AND FREDTL.DTITEM = FRENOT.NTMITM
                                              WHERE FREHDR.HDORD# = ?
                                                AND FREHDR.HDLSUF = ?
                                              ORDER BY FRENOT.NTMITM, FRENOT.NTSEQ#
                                              FOR FETCH ONLY
                                          """;

    private const string GET_ALL_HEADERS_QUERY = """
                                              SELECT 
                                              OHP.OHORD#,
                                              O4P.O4SUFX,                                              

                                              -- Formatted Ship Date: MM/dd/yyyy
                                              LPAD(SUBSTRING(LPAD(O4P.ODSHPM, 4, '0'), 1, 2), 2, '0') || '/' ||
                                              LPAD(SUBSTRING(LPAD(O4P.ODSHPM, 4, '0'), 3, 2), 2, '0') || '/' ||
                                              '20' || LPAD(O4P.ODSHPY, 2, '0') AS FormattedShipDate

                                          FROM 
                                              FRNDTA032.CM1P AS CM1P
                                              INNER JOIN FRNDTA032.OHP AS OHP 
                                                  ON CM1P.C1STKY = OHP.OHSTKY
                                              INNER JOIN FRNDTA032.O4P AS O4P 
                                                  ON OHP.OHORD# = O4P.O4ORD#
                                              INNER JOIN FRNDTA032.NFP AS NFP1
                                                  ON TRIM(O4P.ODCARR) = TRIM(NFP1.NFNUMB)
                                                  AND NFP1.NFTYP = 'CC'
                                              INNER JOIN FRNDTA032.NFP AS NFP2
                                                  ON TRIM(O4P.ODFRTT) = TRIM(NFP2.NFNUMB)
                                                  AND NFP2.NFTYP = 'FT'
                                              WHERE 1=1
                                              AND LPAD(SUBSTRING(LPAD(O4P.ODSHPM, 4, '0'), 1, 2), 2, '0') || '/' ||
                                              LPAD(SUBSTRING(LPAD(O4P.ODSHPM, 4, '0'), 3, 2), 2, '0') || '/' ||
                                              '20' || LPAD(O4P.ODSHPY, 2, '0') = ?                                          
                                          """;

    // ===== Safe readers =====

    private static string GetSafeString(DbDataReader r, string col)
    {
        var o = r.GetOrdinal(col);
        return r.IsDBNull(o) ? string.Empty : r.GetString(o).Trim();
    }

    private static int GetSafeInt(DbDataReader r, string col, int @default = 0)
    {
        int o = r.GetOrdinal(col);
        if (r.IsDBNull(o)) return @default;

        object v = r.GetValue(o);
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

    private string FormatDate(int? rawDate)
    {
        if (rawDate is null or <= 0)
        {
            return string.Empty;
        }

        var dateString = rawDate.Value.ToString();

        if (dateString.Length >= 7 && DateTime.TryParseExact(dateString[1..], "yyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate.ToString("MM/dd/yyyy");
        }

        return dateString;
    }

    // ===== Public API =====

    public async Task<List<ReportModel>> GetShippedOrdersByDate(DateTime shipDate, CancellationToken ct = default)
    {
        var results = new List<ReportModel>();
        var formattedDate = shipDate.ToString("MM/dd/yyyy");
        await using var connection = new OdbcConnection(CONNECTION_STRING);
        await connection.OpenAsync(ct);
        await using var command = new OdbcCommand(GET_ALL_HEADERS_QUERY, connection);
        command.Parameters.Add("", OdbcType.VarChar, 10).Value = formattedDate;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var orderNumber = GetSafeInt(reader, "OHORD#");
            var suffix = GetSafeInt(reader, "O4SUFX");

            var rpt = await GetReportAsync(orderNumber, suffix, ct);

            if(rpt != null)
                results.Add(rpt);
        }
        return results;
    }

    public async Task<ReportModel?> GetReportAsync(int orderNumber, int suffix, CancellationToken ct = default)
    {
        var rpt = new ReportModel();

        // Key: DTITEM (line item number); value: header
        var headersByItemNo = new Dictionary<decimal, LineItemHeader>();
        var allDetails = new List<LineItemDetail>();

        await using var connection = new OdbcConnection(CONNECTION_STRING);
        await connection.OpenAsync(ct);
        await using var command = new OdbcCommand(COMBINED_QUERY, connection);

        // Bind as INT (not Decimal)
        command.Parameters.Add("@HDORD#", OdbcType.Int).Value = orderNumber;
        command.Parameters.Add("@HDLSUF", OdbcType.Int).Value = suffix;

        await using var reader = await command.ExecuteReaderAsync(ct);

        var anyRows = false;
        while (await reader.ReadAsync(ct))
        {
            anyRows = true;

            // Header once
            rpt.Header = new ReportHeader
            {
                OrderNumber = GetSafeInt(reader, "HDORD#"),
                Suffix = GetSafeInt(reader, "HDLSUF"),
                OrdEnterDate = FormatDate(GetSafeInt(reader, "HDORDD")),
                ShipDate = FormatDate(GetSafeInt(reader, "HDRDTE")),
                SoldToCustNumber = GetSafeString(reader, "HDBTKY"),
                SoldToName = GetSafeString(reader, "BTNAME"),
                SoldToCustAddressLine1 = GetSafeString(reader, "BTLNE1"),
                SoldToCustAddressLine2 = GetSafeString(reader, "BTLNE2"),
                SoldToCustAddressLine3 = GetSafeString(reader, "BTLNE3"),
                SoldToCity = GetSafeString(reader, "BTCITY"),
                SoldToSt = GetSafeString(reader, "BTST"),
                SoldToZipCode = GetSafeString(reader, "BTZIP"),
                ShipToCustNumber = GetSafeString(reader, "HDSTKY"),
                ShipToName = GetSafeString(reader, "OSNAME"),
                ShipToCustAddressLine1 = GetSafeString(reader, "OSADR2"),
                ShipToCustAddressLine2 = GetSafeString(reader, "OSADR3"),
                ShipToCustAddressLine3 = GetSafeString(reader, "OSADR4"),
                ShipToCity = GetSafeString(reader, "STCITY"),
                ShipToSt = GetSafeString(reader, "STST"),
                ShipToZipCode = GetSafeString(reader, "STZIP"),
                CustomerPONumber = GetSafeString(reader, "HDSPO#"),
                DueDate = FormatDate(GetSafeInt(reader, "HDPRMD")),
                SalesPerson = GetSafeString(reader, "SPNAME"),
                CarrierName = GetSafeString(reader, "HDCARN"),
                FreightTerms = GetSafeString(reader, "HDFRTT"),
                TrackingNumber = GetSafeString(reader, "HDINST")
            };

            // FREDTL: may be NULL if note doesn't match a line
            var dtItem = GetNullableDecimal(reader, "DTITEM");
            if (dtItem.HasValue && !headersByItemNo.ContainsKey(dtItem.Value))
            {
                headersByItemNo[dtItem.Value] = new LineItemHeader
                {
                    // Id is EF-generated in SQLite; do not set from ERP
                    LineItemNumber = dtItem.Value,
                    ProductNumber = GetSafeString(reader, "DTPN"),
                    ProductDescription = GetSafeString(reader, "DTDESC"),
                    OrderedQuantity = GetSafeDecimal(reader, "DTOQS"),
                    PickOrShipQuantity = GetSafeDecimal(reader, "DTSHPQ"),
                    BackOrderQuantity = GetSafeDecimal(reader, "DTBOQ"),
                };
            }

            // FRENOT: may contain order-level (0.00) and shipping/BOL (950.00) notes
            var ntMitm = GetNullableDecimal(reader, "NTMITM");
            if (ntMitm.HasValue)
            {
                allDetails.Add(new LineItemDetail
                {
                    ModelItem = ntMitm.Value,
                    NoteSequenceNumber = GetSafeDecimal(reader, "NTSEQ#"),
                    NoteText = GetSafeString(reader, "NTTEXT"),
                    PackingListFlag = GetSafeString(reader, "NTPRT2"),
                    BolFlag = GetSafeString(reader, "NTPRT3")
                });
            }
        }

        if (!anyRows)
            return null!; // not found

        // Build the final set of item numbers: headers ∪ notes
        var itemNumbers =
            headersByItemNo.Keys
            .Union(allDetails.Select(d => d.ModelItem))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var itemNo in itemNumbers)
        {
            // Use real header if present; otherwise create a synthetic header
            if (!headersByItemNo.TryGetValue(itemNo, out var lih))
            {
                lih = new LineItemHeader
                {
                    LineItemNumber = itemNo,
                    ProductNumber = string.Empty,
                    ProductDescription = itemNo switch
                    {
                        0m => "ORDER NOTES",
                        950m => "SHIPPING / BOL NOTES",
                        _ => "NOTES"
                    },
                    OrderedQuantity = 0m,
                    PickOrShipQuantity = 0m,
                    BackOrderQuantity = 0m,
                };
            }

            var detailsForItem = allDetails
                .Where(d => d.ModelItem == itemNo)
                .Where(d => d.NoteText != null && !d.NoteText.Contains("OPTIONS BEGIN"))
                .Where(d => d.NoteText != null && !d.NoteText.Contains("OPTIONS END"))
                .OrderBy(d => d.NoteSequenceNumber)
                .ToList();

            var li = new LineItem
            {
                LineItemHeader = lih,
                LineItemDetails = new ObservableCollection<LineItemDetail>(detailsForItem)
            };

            rpt.LineItems.Add(li);
        }

        return rpt;
    }
}
