using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Data;
using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Services.Interfaces;

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

    public async Task SaveReportAsync(ReportModel inputReport, CancellationToken ct = default)
    {
        await using var db = await _dbContext.CreateDbContextAsync(ct);

        // 1. Retrieve the existing report and its entire graph from the database.
        var existingReport = await db.ReportModels
            .Include(r => r.Header)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemHeader)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemDetails)
            .Include(r => r.LineItems)
                .ThenInclude(li => li.LineItemPackingUnits)
            .FirstOrDefaultAsync(r => r.Id == inputReport.Id, ct);

        if (existingReport == null)
        {
            // If the report doesn't exist, add it as a new entity graph.
            foreach (var lineItem in inputReport.LineItems)
            {
                lineItem.ReportModel = inputReport;
                if (lineItem.LineItemHeader != null)
                {
                    lineItem.LineItemHeaderId = lineItem.LineItemHeader.Id;
                }
                foreach (var detail in lineItem.LineItemDetails)
                {
                    detail.LineItem = lineItem;
                }
                foreach (var pu in lineItem.LineItemPackingUnits)
                {
                    pu.LineItem = lineItem;
                }
            }

            db.ReportModels.Add(inputReport);
        }
        else
        {

            // 2. Update the properties of the main entity and its one-to-one relationship.
            db.Entry(existingReport).CurrentValues.SetValues(inputReport);
            db.Entry(existingReport.Header).CurrentValues.SetValues(inputReport.Header);

            // 3. Synchronize the LineItems collection (one-to-many).

            // Get the IDs of the updated line items to quickly check for deletions.
            var updatedLineItemIds = inputReport.LineItems.Select(li => li.Id).ToList();

            // Remove LineItems that are no longer in the updated graph.
            var lineItemsToRemove = existingReport.LineItems
                .Where(li => !updatedLineItemIds.Contains(li.Id))
                .ToList();
            db.LineItems.RemoveRange(lineItemsToRemove);

            // Add or update the remaining line items.
            foreach (var updatedLineItem in inputReport.LineItems)
            {
                var existingLineItem = existingReport.LineItems
                    .FirstOrDefault(li => li.Id == updatedLineItem.Id);

                if (existingLineItem == null)
                {
                    // It's a new line item, add it to the collection.
                    existingReport.LineItems.Add(updatedLineItem);
                }
                else
                {
                    // It's an existing line item, update its properties.
                    db.Entry(existingLineItem).CurrentValues.SetValues(updatedLineItem);
                    if (updatedLineItem.LineItemHeader != null)
                        db.Entry(existingLineItem.LineItemHeader).CurrentValues
                            .SetValues(updatedLineItem.LineItemHeader);

                    // 4. Synchronize the child collections of LineItem.

                    // Synchronize LineItemDetails
                    var updatedDetailIds = updatedLineItem.LineItemDetails.Select(d => d.Id).ToList();
                    var detailsToRemove = existingLineItem.LineItemDetails
                        .Where(d => !updatedDetailIds.Contains(d.Id))
                        .ToList();
                    db.LineItemDetails.RemoveRange(detailsToRemove);
                    foreach (var updatedDetail in updatedLineItem.LineItemDetails)
                    {
                        var existingDetail = existingLineItem.LineItemDetails
                            .FirstOrDefault(d => d.Id == updatedDetail.Id);
                        if (existingDetail == null)
                            existingLineItem.LineItemDetails.Add(updatedDetail);
                        else
                            db.Entry(existingDetail).CurrentValues.SetValues(updatedDetail);
                    }

                    // Synchronize LineItemPackingUnits
                    var updatedUnitIds = updatedLineItem.LineItemPackingUnits.Select(u => u.Id).ToList();
                    var unitsToRemove = existingLineItem.LineItemPackingUnits
                        .Where(u => !updatedUnitIds.Contains(u.Id))
                        .ToList();
                    db.LineItemPackingUnits.RemoveRange(unitsToRemove);
                    foreach (var updatedUnit in updatedLineItem.LineItemPackingUnits)
                    {
                        var existingUnit = existingLineItem.LineItemPackingUnits
                            .FirstOrDefault(u => u.Id == updatedUnit.Id);
                        if (existingUnit == null)
                            existingLineItem.LineItemPackingUnits.Add(updatedUnit);
                        else
                            db.Entry(existingUnit).CurrentValues.SetValues(updatedUnit);
                    }
                }
            }
        }

        // 5. Save all changes to the database.
        await db.SaveChangesAsync(ct);
    }

}
