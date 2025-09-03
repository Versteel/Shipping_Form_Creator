using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Data;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;
using System.Diagnostics;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class SqliteService(IDbContextFactory<AppDbContext> dbContext) : ISqliteService
{
    private readonly IDbContextFactory<AppDbContext> _dbContext = dbContext;

    public bool ExistsInERP(int orderNumber)
    {
        using var db = _dbContext.CreateDbContext();
        return db.ReportHeaders.Any(h => h.OrderNumber == orderNumber);
    }

    public async Task<ReportModel?> GetReportAsync(int orderNumber, CancellationToken ct = default)
    {
        await using var db = await _dbContext.CreateDbContextAsync(ct);

        var report = await db.ReportModels
          .AsNoTracking()
          .AsSplitQuery()
          .Include(r => r.Header)
          .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemHeader)
          .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemDetails)
          .Include(r => r.LineItems)
            .ThenInclude(li => li.LineItemPackingUnits)
          .SingleOrDefaultAsync(r => r.Header.OrderNumber == orderNumber, ct);

        return report;
    }

    public async Task SaveReportAsync(ReportModel report, CancellationToken ct = default)
    {
        await using var db = await _dbContext.CreateDbContextAsync(ct);

        if (report.Id == 0)
        {
            // Add new report with all children
            AddNewReport(db, report, ct);
        }
        else
        {
            // Update existing report and children
            await UpdateExistingReportAsync(db, report, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private void AddNewReport(DbContext db, ReportModel report, CancellationToken ct)
    {
        // Add the report - EF will cascade to children
        db.Set<ReportModel>().Add(report);
    }

    private async Task UpdateExistingReportAsync(DbContext db, ReportModel report, CancellationToken ct)
    {
        // Load existing report with all related data
        var existingReport = await db.Set<ReportModel>()
            .Include(r => r.Header)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemHeader)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemDetails)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemPackingUnits)
            .FirstOrDefaultAsync(r => r.Id == report.Id, ct);

        if (existingReport == null)
        {
            throw new InvalidOperationException($"Report with ID {report.Id} not found.");
        }

        // Update header properties
        if (report.Header != null)
        {
            UpdateReportHeader(existingReport.Header, report.Header);
        }

        // Update line items
        UpdateLineItems(db, existingReport, report.LineItems, ct);
    }

    private void UpdateReportHeader(ReportHeader existing, ReportHeader updated)
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
        existing.ShippingInstructions = updated.ShippingInstructions;
        existing.FreightTerms = updated.FreightTerms;
    }

    private void UpdateLineItems(DbContext db, ReportModel existingReport, ICollection<LineItem> updatedLineItems, CancellationToken ct)
    {
        var existingLineItemIds = existingReport.LineItems.Select(li => li.Id).ToHashSet();
        var updatedLineItemIds = updatedLineItems.Where(li => li.Id != 0).Select(li => li.Id).ToHashSet();

        // Remove line items that are no longer present
        var lineItemsToRemove = existingReport.LineItems.Where(li => !updatedLineItemIds.Contains(li.Id)).ToList();
        foreach (var lineItem in lineItemsToRemove)
        {
            existingReport.LineItems.Remove(lineItem);
            db.Set<LineItem>().Remove(lineItem);
        }

        // Process each updated line item
        foreach (var updatedLineItem in updatedLineItems)
        {
            if (updatedLineItem.Id == 0)
            {
                // Add new line item
                updatedLineItem.ReportModelId = existingReport.Id;
                existingReport.LineItems.Add(updatedLineItem);
            }
            else
            {
                // Update existing line item
                var existingLineItem = existingReport.LineItems.FirstOrDefault(li => li.Id == updatedLineItem.Id);
                if (existingLineItem != null)
                {
                    UpdateLineItem(db, existingLineItem, updatedLineItem, ct);
                }
            }
        }
    }

    private void UpdateLineItem(DbContext db, LineItem existingLineItem, LineItem updatedLineItem, CancellationToken ct)
    {
        // Update line item header
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

        // Update line item details
        UpdateLineItemDetails(db, existingLineItem, updatedLineItem.LineItemDetails);

        // Update line item packing units
        UpdateLineItemPackingUnits(db, existingLineItem, updatedLineItem.LineItemPackingUnits);
    }

    private void UpdateLineItemHeader(LineItemHeader existing, LineItemHeader updated)
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
        var existingDetailIds = existingLineItem.LineItemDetails.Select(d => d.Id).ToHashSet();
        var updatedDetailIds = updatedDetails.Where(d => d.Id != 0).Select(d => d.Id).ToHashSet();

        // Remove details that are no longer present
        var detailsToRemove = existingLineItem.LineItemDetails.Where(d => !updatedDetailIds.Contains(d.Id)).ToList();
        foreach (var detail in detailsToRemove)
        {
            existingLineItem.LineItemDetails.Remove(detail);
            db.Set<LineItemDetail>().Remove(detail);
        }

        // Process each updated detail
        foreach (var updatedDetail in updatedDetails)
        {
            if (updatedDetail.Id == 0)
            {
                // Add new detail
                updatedDetail.LineItemId = existingLineItem.Id;
                existingLineItem.LineItemDetails.Add(updatedDetail);
            }
            else
            {
                // Update existing detail
                var existingDetail = existingLineItem.LineItemDetails.FirstOrDefault(d => d.Id == updatedDetail.Id);
                if (existingDetail != null)
                {
                    UpdateLineItemDetail(existingDetail, updatedDetail);
                }
            }
        }
    }

    private void UpdateLineItemDetail(LineItemDetail existing, LineItemDetail updated)
    {
        existing.ModelItem = updated.ModelItem;
        existing.NoteSequenceNumber = updated.NoteSequenceNumber;
        existing.NoteText = updated.NoteText;
        existing.PackingListFlag = updated.PackingListFlag;
        existing.BolFlag = updated.BolFlag;
    }

    private void UpdateLineItemPackingUnits(DbContext db, LineItem existingLineItem, ICollection<LineItemPackingUnit> updatedPackingUnits)
    {
        var existingPackingUnitIds = existingLineItem.LineItemPackingUnits.Select(pu => pu.Id).ToHashSet();
        var updatedPackingUnitIds = updatedPackingUnits.Where(pu => pu.Id != 0).Select(pu => pu.Id).ToHashSet();

        // Remove packing units that are no longer present
        var packingUnitsToRemove = existingLineItem.LineItemPackingUnits.Where(pu => !updatedPackingUnitIds.Contains(pu.Id)).ToList();
        foreach (var packingUnit in packingUnitsToRemove)
        {
            existingLineItem.LineItemPackingUnits.Remove(packingUnit);
            db.Set<LineItemPackingUnit>().Remove(packingUnit);
        }

        // Process each updated packing unit
        foreach (var updatedPackingUnit in updatedPackingUnits)
        {
            if (updatedPackingUnit.Id == 0)
            {
                // Add new packing unit
                updatedPackingUnit.LineItemId = existingLineItem.Id;
                existingLineItem.LineItemPackingUnits.Add(updatedPackingUnit);
            }
            else
            {
                // Update existing packing unit
                var existingPackingUnit = existingLineItem.LineItemPackingUnits.FirstOrDefault(pu => pu.Id == updatedPackingUnit.Id);
                if (existingPackingUnit != null)
                {
                    UpdateLineItemPackingUnit(existingPackingUnit, updatedPackingUnit);
                }
            }
        }
    }

    private void UpdateLineItemPackingUnit(LineItemPackingUnit existing, LineItemPackingUnit updated)
    {
        existing.Quantity = updated.Quantity;
        existing.CartonOrSkid = updated.CartonOrSkid;
        existing.LineNumber = updated.LineNumber;
        existing.TypeOfUnit = updated.TypeOfUnit;
        existing.Weight = updated.Weight;
    }

}
