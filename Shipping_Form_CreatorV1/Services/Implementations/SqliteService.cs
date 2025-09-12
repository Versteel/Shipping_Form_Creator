using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Data;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class SqliteService(IDbContextFactory<AppDbContext> dbContext) : ISqliteService
{
    public async Task<ReportModel?> GetReportAsync(int orderNumber, CancellationToken ct = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(ct);

        var reportModelId = await db.ReportHeaders
            .Where(h => h.OrderNumber == orderNumber)
            .Select(h => h.ReportModelId)
            .FirstOrDefaultAsync(ct);

        if (reportModelId == 0)
            return null;

        var report = await db.ReportModels
            .Where(r => r.Header.OrderNumber == orderNumber)
            .Include(r => r.Header)
            .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemHeader)
            .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemDetails)
            .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemPackingUnits)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);


        return report;
    }
    
    public async Task<List<ReportModel>> GetAllReportsByDateAsync(DateTime date, CancellationToken ct = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(ct);

        var search = date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        // Start from headers so we only get rows that actually have a header
        var query = await db.ReportModels
            .Include(r => r.Header)
            .Where(r => r.Header.ShipDate == search)
            .ToListAsync(ct);

        return query;
    }

    public async Task SaveReportAsync(ReportModel report, CancellationToken ct = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(ct);

        if (report.Id == 0)
        {
            AddNewReport(db, report);
        }
        else
        {
            await UpdateExistingReportAsync(db, report, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static void AddNewReport(DbContext db, ReportModel report)
    {
        db.Set<ReportModel>().Add(report);
    }

    private async Task UpdateExistingReportAsync(DbContext db, ReportModel report, CancellationToken ct)
    {
        var existingReport = await db.Set<ReportModel>()
            .Include(r => r.Header)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemHeader)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemDetails)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemPackingUnits)
            .FirstOrDefaultAsync(r => r.Id == report.Id, ct) 
            ?? throw new InvalidOperationException($"Report with ID {report.Id} not found.");

        UpdateReportHeader(existingReport.Header, report.Header);

        UpdateLineItems(db, existingReport, report.LineItems);
    }

    private static void UpdateReportHeader(ReportHeader existing, ReportHeader updated)
    {
        existing.LogoImagePath = updated.LogoImagePath;
        existing.OrderNumber = updated.OrderNumber;
        existing.PageCount = updated.PageCount;
        existing.OrdEnterDate = updated.OrdEnterDate;
        existing.ShipDate = updated.ShipDate;
        existing.SoldToCustNumber = updated.SoldToCustNumber;
        existing.ShipToCustNumber = updated.ShipToCustNumber;
        existing.SoldToName = updated.SoldToName;
        existing.SoldToCustAddressLine1 = updated.SoldToCustAddressLine1;
        existing.SoldToCustAddressLine2 = updated.SoldToCustAddressLine2;
        existing.SoldToCustAddressLine3 = updated.SoldToCustAddressLine3;
        existing.SoldToCity = updated.SoldToCity;
        existing.SoldToSt = updated.SoldToSt;
        existing.SoldToZipCode = updated.SoldToZipCode;
        existing.ShipToName = updated.ShipToName;
        existing.ShipToCustAddressLine1 = updated.ShipToCustAddressLine1;
        existing.ShipToCustAddressLine2 = updated.ShipToCustAddressLine2;
        existing.ShipToCustAddressLine3 = updated.ShipToCustAddressLine3;
        existing.ShipToCity = updated.ShipToCity;
        existing.ShipToSt = updated.ShipToSt;
        existing.ShipToZipCode = updated.ShipToZipCode;
        existing.CustomerPONumber = updated.CustomerPONumber;
        existing.DueDate = updated.DueDate;
        existing.SalesPerson = updated.SalesPerson;
        existing.CarrierName = updated.CarrierName;
        existing.TrackingNumber = updated.TrackingNumber;
        existing.FreightTerms = updated.FreightTerms;
    }

    private void UpdateLineItems(DbContext db, ReportModel existingReport, ICollection<LineItem> updatedLineItems)
    {
        var updatedLineItemIds = updatedLineItems.Where(li => li.Id != 0).Select(li => li.Id).ToHashSet();

        var lineItemsToRemove = existingReport.LineItems.Where(li => !updatedLineItemIds.Contains(li.Id)).ToList();
        foreach (var lineItem in lineItemsToRemove)
        {
            existingReport.LineItems.Remove(lineItem);
            db.Set<LineItem>().Remove(lineItem);
        }

        foreach (var updatedLineItem in updatedLineItems)
        {
            if (updatedLineItem.Id == 0)
            {
                updatedLineItem.ReportModelId = existingReport.Id;
                existingReport.LineItems.Add(updatedLineItem);
            }
            else
            {
                var existingLineItem = existingReport.LineItems.FirstOrDefault(li => li.Id == updatedLineItem.Id);
                if (existingLineItem != null)
                {
                    UpdateLineItem(db, existingLineItem, updatedLineItem);
                }
            }
        }
    }

    private void UpdateLineItem(DbContext db, LineItem existingLineItem, LineItem updatedLineItem)
    {
        if (updatedLineItem.LineItemHeader != null)
        {
            if (existingLineItem.LineItemHeader == null)
            {
                existingLineItem.LineItemHeader = updatedLineItem.LineItemHeader;
                existingLineItem.LineItemHeaderId = updatedLineItem.LineItemHeader.Id;
            }
            else
            {
                UpdateLineItemHeader(existingLineItem.LineItemHeader, updatedLineItem.LineItemHeader);
            }
        }

        UpdateLineItemDetails(db, existingLineItem, updatedLineItem.LineItemDetails);

        UpdateLineItemPackingUnits(db, existingLineItem, updatedLineItem.LineItemPackingUnits);
    }

    private static void UpdateLineItemHeader(LineItemHeader existing, LineItemHeader updated)
    {
        existing.LineItemNumber = updated.LineItemNumber;
        existing.ProductNumber = updated.ProductNumber;
        existing.ProductDescription = updated.ProductDescription;
        existing.OrderedQuantity = updated.OrderedQuantity;
        existing.PickOrShipQuantity = updated.PickOrShipQuantity;
        existing.BackOrderQuantity = updated.BackOrderQuantity;
    }

    private void UpdateLineItemDetails(DbContext db, LineItem existingLineItem, ICollection<LineItemDetail> updatedDetails)
    {
        var updatedDetailIds = updatedDetails.Where(d => d.Id != 0).Select(d => d.Id).ToHashSet();

        var detailsToRemove = existingLineItem.LineItemDetails.Where(d => !updatedDetailIds.Contains(d.Id)).ToList();
        foreach (var detail in detailsToRemove)
        {
            existingLineItem.LineItemDetails.Remove(detail);
            db.Set<LineItemDetail>().Remove(detail);
        }

        foreach (var updatedDetail in updatedDetails)
        {
            if (updatedDetail.Id == 0)
            {
                updatedDetail.LineItemId = existingLineItem.Id;
                existingLineItem.LineItemDetails.Add(updatedDetail);
            }
            else
            {
                var existingDetail = existingLineItem.LineItemDetails.FirstOrDefault(d => d.Id == updatedDetail.Id);
                if (existingDetail != null)
                {
                    UpdateLineItemDetail(existingDetail, updatedDetail);
                }
            }
        }
    }

    private static void UpdateLineItemDetail(LineItemDetail existing, LineItemDetail updated)
    {
        existing.ModelItem = updated.ModelItem;
        existing.NoteSequenceNumber = updated.NoteSequenceNumber;
        existing.NoteText = updated.NoteText;
        existing.PackingListFlag = updated.PackingListFlag;
        existing.BolFlag = updated.BolFlag;
    }

    private void UpdateLineItemPackingUnits(DbContext db, LineItem existingLineItem, ICollection<LineItemPackingUnit> updatedPackingUnits)
    {
        var updatedPackingUnitIds = updatedPackingUnits.Where(pu => pu.Id != 0).Select(pu => pu.Id).ToHashSet();

        var packingUnitsToRemove = existingLineItem.LineItemPackingUnits.Where(pu => !updatedPackingUnitIds.Contains(pu.Id)).ToList();
        foreach (var packingUnit in packingUnitsToRemove)
        {
            existingLineItem.LineItemPackingUnits.Remove(packingUnit);
            db.Set<LineItemPackingUnit>().Remove(packingUnit);
        }

        foreach (var updatedPackingUnit in updatedPackingUnits)
        {
            if (updatedPackingUnit.Id == 0)
            {
                updatedPackingUnit.LineItemId = existingLineItem.Id;
                existingLineItem.LineItemPackingUnits.Add(updatedPackingUnit);
            }
            else
            {
                var existingPackingUnit = existingLineItem.LineItemPackingUnits.FirstOrDefault(pu => pu.Id == updatedPackingUnit.Id);
                if (existingPackingUnit != null)
                {
                    UpdateLineItemPackingUnit(existingPackingUnit, updatedPackingUnit);
                }
            }
        }
    }

    private static void UpdateLineItemPackingUnit(LineItemPackingUnit existing, LineItemPackingUnit updated)
    {
        existing.Quantity = updated.Quantity;
        existing.CartonOrSkid = updated.CartonOrSkid;
        existing.LineNumber = updated.LineNumber;
        existing.TypeOfUnit = updated.TypeOfUnit;
        existing.Weight = updated.Weight;
    }

}
