using Microsoft.EntityFrameworkCore;
using Shipping_Form_CreatorV1.Models;
using System.Reflection.Emit;

namespace Shipping_Form_CreatorV1.Data;

public class AppDbContext : DbContext
{
    public DbSet<ReportModel> ReportModels => Set<ReportModel>();
    public DbSet<ReportHeader> ReportHeaders => Set<ReportHeader>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<LineItemHeader> LineItemHeaders => Set<LineItemHeader>();
    public DbSet<LineItemDetail> LineItemDetails => Set<LineItemDetail>();
    public DbSet<LineItemPackingUnit> LineItemPackingUnits => Set<LineItemPackingUnit>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ReportModel ⟷ ReportHeader (1:1)
        b.Entity<ReportModel>()
            .HasOne(r => r.Header)
            .WithOne(h => h.ReportModel)
            .HasForeignKey<ReportHeader>(h => h.ReportModelId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReportModel → LineItems (1:many)
        b.Entity<ReportModel>()
            .HasMany(r => r.LineItems)
            .WithOne(li => li.ReportModel)
            .HasForeignKey(li => li.ReportModelId);

        // LineItem ⟷ LineItemHeader (1:1)
        b.Entity<LineItem>()
            .HasOne(li => li.LineItemHeader)
            .WithOne(h => h.LineItem)
            .HasForeignKey<LineItem>(li => li.LineItemHeaderId);

        // LineItem → LineItemDetails (1:many)
        b.Entity<LineItemDetail>()
            .HasOne(d => d.LineItem)
            .WithMany(li => li.LineItemDetails)
            .HasForeignKey(d => d.LineItemId);

        // LineItem → LineItemPackingUnits (1:many)
        b.Entity<LineItemPackingUnit>()
            .HasOne(pu => pu.LineItem)
            .WithMany(li => li.LineItemPackingUnits)
            .HasForeignKey(pu => pu.LineItemId);


        b.Entity<LineItemPackingUnit>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
        b.Entity<ReportModel>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();

        b.Entity<ReportHeader>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();

        b.Entity<LineItem>()
            .Property(li => li.Id)
            .ValueGeneratedOnAdd();

        b.Entity<LineItemHeader>()
            .Property(lih => lih.Id)
            .ValueGeneratedOnAdd();

        b.Entity<LineItemDetail>()
            .Property(d => d.Id)
            .ValueGeneratedOnAdd();

        b.Entity<LineItemPackingUnit>()
            .Property(pu => pu.Id)
            .ValueGeneratedOnAdd();


    }

}