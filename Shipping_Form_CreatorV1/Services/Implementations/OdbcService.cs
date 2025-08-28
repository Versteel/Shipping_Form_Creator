using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class OdbcService : IOdbcService
{
    private const string CONNECTION_STRING =
        "Driver={IBM i Access ODBC Driver};System=192.168.1.2;Uid=FRN032;Pwd=FRN032;";

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
                                                 AND FREDTL.DTLSUF = 0
                                                 AND FREDTL.DTITEM = FRENOT.NTMITM
                                              WHERE FREHDR.HDORD# = ?
                                                AND FREHDR.HDLSUF = 0
                                              ORDER BY FRENOT.NTMITM, FRENOT.NTSEQ#
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
            _ => System.Convert.ToInt32(v)
        };
    }

    private static decimal? GetNullableDecimal(DbDataReader r, string col)
    {
        int o = r.GetOrdinal(col);
        if (r.IsDBNull(o)) return null;
        object v = r.GetValue(o);
        return v is decimal d ? d : System.Convert.ToDecimal(v);
    }

    private static decimal GetSafeDecimal(DbDataReader r, string col, decimal @default = 0m)
        => GetNullableDecimal(r, col) ?? @default;

    private string FormatDate(int? rawDate)
    {
        if (rawDate is null || rawDate <= 0)
        {
            return string.Empty;
        }

        var dateString = rawDate.Value.ToString();

        if (dateString.Length >= 7 && DateTime.TryParseExact(dateString[1..], "yyMMdd", null,
                System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            return parsedDate.ToString("MM/dd/yyyy");
        }

        return dateString;
    }

    // ===== Public API =====

    public async Task<ReportModel?> GetReportAsync(int orderNumber, CancellationToken ct = default)
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

        await using var reader = await command.ExecuteReaderAsync(ct);

        var anyRows = false;
        while (await reader.ReadAsync(ct))
        {
            anyRows = true;

            // Header once
            rpt.Header = new ReportHeader
            {
                OrderNumber = GetSafeInt(reader, "HDORD#"),
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
                ShipToName = GetSafeString(reader, "STNAME"),
                ShipToCustAddressLine1 = GetSafeString(reader, "STLNE1"),
                ShipToCustAddressLine2 = GetSafeString(reader, "STLNE2"),
                ShipToCustAddressLine3 = GetSafeString(reader, "STLNE3"),
                ShipToCity = GetSafeString(reader, "STCITY"),
                ShipToSt = GetSafeString(reader, "STST"),
                ShipToZipCode = GetSafeString(reader, "STZIP"),
                CustomerPONumber = GetSafeString(reader, "HDSPO#"),
                DueDate = FormatDate(GetSafeInt(reader, "HDPRMD")),
                SalesPerson = GetSafeString(reader, "SPNAME"),
                CarrierName = GetSafeString(reader, "HDCARN"),
                FreightTerms = GetSafeString(reader, "HDFRTT"),
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
                    ProductDescription = itemNo == 0m
                        ? "ORDER NOTES"
                        : (itemNo == 950m ? "SHIPPING / BOL NOTES" : "NOTES"),
                    OrderedQuantity = 0m,
                    PickOrShipQuantity = 0m,
                    BackOrderQuantity = 0m,
                };
            }

            var detailsForItem = allDetails
                .Where(d => d.ModelItem == itemNo)
                .Where(d => !d.NoteText.Contains("OPTIONS BEGIN"))
                .Where(d => !d.NoteText.Contains("OPTIONS END"))
                .OrderBy(d => d.NoteSequenceNumber)
                .ToList();

            var li = new LineItem
            {
                LineItemHeader = lih,
                // LineItemHeaderId stays 0; EF will set when saved to SQLite
                LineItemDetails = new ObservableCollection<LineItemDetail>(detailsForItem)
            };

            rpt.LineItems.Add(li);
        }

        return rpt;
    }
}
