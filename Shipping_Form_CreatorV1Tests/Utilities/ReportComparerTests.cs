using Shipping_Form_CreatorV1.Models;
using Shipping_Form_CreatorV1.Utilities;

namespace Shipping_Form_CreatorV1Tests.Utilities;

[TestClass]
public class ReportComparerTests
{
    [TestMethod]
    public void EquivalentReports_ShouldBeEqual()
    {
        var r1 = new ReportModel
        {
            Header = new ReportHeader { OrderNumber = 123, ShipToName = "Alice" }
        };
        var r2 = new ReportModel
        {
            Header = new ReportHeader { OrderNumber = 123, ShipToName = " Alice " } 
        };

        var equal = ReportComparer.AreEquivalent(r1, r2);

        Assert.IsTrue(equal);
    }

    [TestMethod]
    public void DifferentReports_ShouldNotBeEqual()
    {
        var r1 = new ReportModel { Header = new ReportHeader { OrderNumber = 123 } };
        var r2 = new ReportModel { Header = new ReportHeader { OrderNumber = 999 } };

        var equal = ReportComparer.AreEquivalent(r1, r2);

        Assert.IsFalse(equal);
    }
}
