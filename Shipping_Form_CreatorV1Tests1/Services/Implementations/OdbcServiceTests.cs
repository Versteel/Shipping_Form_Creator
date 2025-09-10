using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shipping_Form_CreatorV1.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shipping_Form_CreatorV1.Services.Implementations.Tests
{
    [TestClass()]
    public class OdbcServiceTests
    {
        [TestMethod()]
        public async Task GetAllReportsForSeedingAsyncTest()
        {
            var odbcService = new OdbcService();
            var reports = await odbcService.GetAllReportsForSeedingAsync();
            // Assert
            // 1. Verify that the result is not null.
            Assert.IsNotNull(reports, "The returned list of reports should not be null.");

            // 2. Verify that the list contains at least one report.
            // This assumes your database has data for this query.
            Assert.IsTrue(reports.Any(), "The returned list of reports should not be empty.");

            // 3. Optional: Add more specific assertions to validate the data structure.
            // This is a good practice to ensure the data is being mapped correctly.
            var firstReport = reports.FirstOrDefault();
            Assert.IsNotNull(firstReport, "The first report in the list should not be null.");
            Assert.IsNotNull(firstReport.Header, "The report header should not be null.");
            Assert.IsTrue(firstReport.Header.OrderNumber > 0, "The OrderNumber should be a valid, positive number.");
            Assert.IsNotNull(firstReport.LineItems, "The list of line items should not be null.");

            // For more thorough testing, you could check that line items are correctly populated.
            // This example checks if any report has line items.
            Assert.IsTrue(reports.Any(r => r.LineItems.Any()), "At least one report should have line items.");
        }
    }
}