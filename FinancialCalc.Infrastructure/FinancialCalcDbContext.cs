using FinancialCalc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialCalc.Infrastructure
{
    public class FinancialCalcDbContext : DbContext
    {
        public FinancialCalcDbContext(DbContextOptions<FinancialCalcDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBomLine> ProductBomLines { get; set; }
        public DbSet<ProductBopLine> ProductBopLines { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<ProductMaterial> ProductMaterials { get; set; }
        public DbSet<Workstation> Workstations { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }  

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialCalcDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}