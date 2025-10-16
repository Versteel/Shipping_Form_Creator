using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shipping_Form_CreatorV1.Data;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;

namespace Shipping_Form_CreatorV1.Services.Implementations;

public class SqliteService(IDbContextFactory<AppDbContext> dbContext) : ISqliteService
{
    public async Task<ReportModel?> GetReportAsync(int orderNumber, int suffixNumber, CancellationToken ct = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(ct);

        var reportModelId = await db.ReportHeaders
            .Where(h => h.OrderNumber == orderNumber)
            .Where(h => h.Suffix == suffixNumber)
            .Select(h => h.ReportModelId)
            .FirstOrDefaultAsync(ct);

        if (reportModelId == 0)
            return null;

        var report = await db.ReportModels
            .Where(r => r.Header.OrderNumber == orderNumber)
            .Include(r => r.Header)
            .Include(r => r.HandlingUnits)
            .ThenInclude(handlingUnit => handlingUnit.ContainedUnits)
                .ThenInclude(packingUnit => packingUnit.LineItem)
                    .ThenInclude(lineItem => lineItem.LineItemHeader)
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
            Log.Information("User: {User} is adding a new ReportModel with OrderNumber: {OrderNumber} and Suffix: {Suffix}",
                Environment.UserName, report.Header.OrderNumber, report.Header.Suffix);
            AddNewReport(db, report);
        }
        else
        {
            Log.Information("User: {User} is updating ReportModel ID: {ReportId} with OrderNumber: {OrderNumber} and Suffix: {Suffix}",
                Environment.UserName, report.Id, report.Header.OrderNumber, report.Header.Suffix);
            await UpdateExistingReportAsync(db, report, ct);
        }

        await db.SaveChangesAsync(ct);
        Log.Information("User: {User} successfully saved/updated ReportModel ID: {ReportId}",
            Environment.UserName, report.Id);
    }

    private static void AddNewReport(DbContext db, ReportModel report)
    {
        db.Set<ReportModel>().Add(report);
    }

    private async Task UpdateExistingReportAsync(DbContext db, ReportModel report, CancellationToken ct)
    {
        var existingReport = await db.Set<ReportModel>()
            .Include(r => r.Header)
            .Include(r => r.HandlingUnits)
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
        UpdateHandlingUnits(db, existingReport, report.HandlingUnits);
    }
    private static void UpdateHandlingUnits(DbContext db, ReportModel existingReport, ICollection<HandlingUnit> updatedHandlingUnits)
    {
        var updatedIds = updatedHandlingUnits.Select(h => h.Id).ToHashSet();

        // 1. Handle Deletions: Find units in the DB that are not in the UI's list and remove them.
        var unitsToRemove = existingReport.HandlingUnits.Where(h => !updatedIds.Contains(h.Id)).ToList();
        foreach (var unit in unitsToRemove)
        {
            // Un-assign all children before deleting the parent pallet.
            foreach (var contained in unit.ContainedUnits.ToList())
            {
                unit.ContainedUnits.Remove(contained);
            }
            existingReport.HandlingUnits.Remove(unit);
            db.Set<HandlingUnit>().Remove(unit);
        }

        // Create a lookup for all packing units currently tracked by Entity Framework for this report.
        var allTrackedPackingUnits = existingReport.LineItems
            .SelectMany(li => li.LineItemPackingUnits)
            .Where(pu => pu.Id > 0)
            .ToDictionary(pu => pu.Id);


        // 2. Handle Additions and Updates for each pallet from the UI.
        foreach (var updatedUnit in updatedHandlingUnits)
        {
            // Try to find the corresponding unit that EF is tracking.
            var existingUnit = existingReport.HandlingUnits.FirstOrDefault(h => h.Id == updatedUnit.Id);

            if (existingUnit == null)
            {
                // This is a NEW handling unit. Create it and add it to the report.
                existingUnit = new HandlingUnit { ReportModelId = existingReport.Id };
                existingReport.HandlingUnits.Add(existingUnit);
            }

            // Update properties from the UI object to the tracked entity.
            existingUnit.Description = updatedUnit.Description;

            // Now, reconcile the children (the items dragged onto the pallet).
            var uiContainedUnitIds = updatedUnit.ContainedUnits.Select(cu => cu.Id).ToHashSet();

            // Remove children that are no longer on this pallet.
            var childrenToRemove = existingUnit.ContainedUnits.Where(cu => !uiContainedUnitIds.Contains(cu.Id)).ToList();
            foreach (var child in childrenToRemove)
            {
                existingUnit.ContainedUnits.Remove(child);
            }

            // Add children that were newly dragged onto this pallet.
            foreach (var uiChild in updatedUnit.ContainedUnits)
            {
                // Check if the existing pallet already contains this child.
                if (!existingUnit.ContainedUnits.Any(c => c.Id == uiChild.Id))
                {
                    // Find the EF-tracked version of this child from our lookup and add it.
                    if (allTrackedPackingUnits.TryGetValue(uiChild.Id, out var trackedChild))
                    {
                        existingUnit.ContainedUnits.Add(trackedChild);
                    }
                }
            }
        }
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
        var updatedLineItemIds = updatedLineItems.Where(li => li.Id > 0).Select(li => li.Id).ToHashSet();

        var lineItemsToRemove = existingReport.LineItems.Where(li => li.Id > 0 && !updatedLineItemIds.Contains(li.Id)).ToList();
        foreach (var lineItem in lineItemsToRemove)
        {
            existingReport.LineItems.Remove(lineItem);
            db.Set<LineItem>().Remove(lineItem);
        }

        foreach (var updatedLineItem in updatedLineItems)
        {
            if (updatedLineItem.Id <= 0) // <-- CHANGE IS HERE
            {
                var newLineItem = new LineItem
                {
                    ReportModelId = existingReport.Id,
                    LineItemHeader = updatedLineItem.LineItemHeader,
                    LineItemDetails = new ObservableCollection<LineItemDetail>(updatedLineItem.LineItemDetails),
                    LineItemPackingUnits = new ObservableCollection<LineItemPackingUnit>(updatedLineItem.LineItemPackingUnits)
                };
                existingReport.LineItems.Add(newLineItem);
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

    private static void UpdateLineItem(DbContext db, LineItem existingLineItem, LineItem updatedLineItem)
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

    private static void UpdateLineItemDetails(DbContext db, LineItem existingLineItem, ICollection<LineItemDetail> updatedDetails)
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

    private static void UpdateLineItemPackingUnits(DbContext db, LineItem existingLineItem, ICollection<LineItemPackingUnit> updatedPackingUnits)
    {
        var updatedPackingUnitIds = updatedPackingUnits.Where(pu => pu.Id > 0).Select(pu => pu.Id).ToHashSet();

        var packingUnitsToRemove = existingLineItem.LineItemPackingUnits.Where(pu => pu.Id > 0 && !updatedPackingUnitIds.Contains(pu.Id)).ToList();
        foreach (var packingUnit in packingUnitsToRemove)
        {
            Log.Information("User {User} is deleting packing unit {Id}.", Environment.UserName, packingUnit.Id);
            existingLineItem.LineItemPackingUnits.Remove(packingUnit);
            db.Set<LineItemPackingUnit>().Remove(packingUnit);
        }

        foreach (var updatedPackingUnit in updatedPackingUnits)
        {
            if (updatedPackingUnit.Id <= 0) // This is a new unit from the UI
            {
                var newDbUnit = new LineItemPackingUnit
                {
                    Quantity = updatedPackingUnit.Quantity,
                    CartonOrSkid = updatedPackingUnit.CartonOrSkid,
                    TypeOfUnit = updatedPackingUnit.TypeOfUnit,
                    Weight = updatedPackingUnit.Weight,
                    TruckNumber = updatedPackingUnit.TruckNumber,
                    LineNumber = updatedPackingUnit.LineNumber,
                    CartonOrSkidContents = updatedPackingUnit.CartonOrSkidContents,
                };

                // FIX: Add the new unit to the existing line item's collection
                existingLineItem.LineItemPackingUnits.Add(newDbUnit);

                // AND explicitly tell the DbContext to track it as a new entity.
                db.Add(newDbUnit);
            }
            else
            {
                var existingPackingUnit = existingLineItem.LineItemPackingUnits.FirstOrDefault(pu => pu.Id == updatedPackingUnit.Id);
                if (existingPackingUnit != null)
                {
                    UpdateLineItemPackingUnit(db, existingPackingUnit, updatedPackingUnit);
                }
            }
        }
    }

    private static void UpdateLineItemPackingUnit(DbContext db, LineItemPackingUnit existing, LineItemPackingUnit updated)
    {
        // Use EF's built-in method to copy the scalar properties.
        // This is safer and avoids navigation property conflicts.
        db.Entry(existing).CurrentValues.SetValues(updated);
    }

}
