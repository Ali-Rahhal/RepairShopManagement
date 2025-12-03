using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RepairShop.Models;

namespace RepairShop.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<TransactionHeader> TransactionHeaders { get; set; }
        public DbSet<TransactionBody> TransactionBodies { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<SerialNumber> SerialNumbers { get; set; }
        public DbSet<Warranty> Warranties { get; set; }
        public DbSet<MaintenanceContract> MaintenanceContracts { get; set; }
        public DbSet<DefectiveUnit> DefectiveUnits { get; set; }
        public DbSet<PreventiveMaintenanceRecord> PreventiveMaintenanceRecords { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
