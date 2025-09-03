using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shipping_Form_CreatorV1.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>();

            // Use your actual SQL Server connection string here
            options.UseSqlServer(
                "Server=reportingpc,1433;Database=ShippingFormsDb;Integrated Security=SSPI;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;",
                sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    sql.CommandTimeout(60);
                })
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine);
            return new AppDbContext(options.Options);
        }
    }

}
