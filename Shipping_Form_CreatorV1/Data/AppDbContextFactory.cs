using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shipping_Form_CreatorV1.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Use your actual SQL Server connection string here
            optionsBuilder.UseSqlServer("Server=reportingpc\\SQLEXPRESS;Database=ShippingFormsDb;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");

            return new AppDbContext(optionsBuilder.Options);
        }
    }

}
