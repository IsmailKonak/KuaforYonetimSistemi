using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebProjeDeneme1.Models.Kullanicilar;
using WebProjeDeneme1.Models.Personeller;
using WebProjeDeneme1.Models.Randevular;
using WebProjeDeneme1.Models.Salonlar;
using WebProjeDeneme1.Models.Uzmanlik;
using System.Linq;

namespace WebProjeDeneme1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Tabloya karşılık gelen DbSet'ler
        public DbSet<Uye> Uyeler { get; set; }
        public DbSet<Personel> Personeller { get; set; }
        public DbSet<Randevu> Randevular { get; set; }
        public DbSet<Konum> Konumlar { get; set; }
        public DbSet<Salon> Salonlar { get; set; }
        public DbSet<UzmanlikAlani> UzmanlikAlanlari { get; set; }
        public DbSet<YapilabilenIslem> YapilabilenIslemler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Özel ilişki ve kurallar burada tanımlanabilir
            modelBuilder.Entity<Personel>()
                .Property(p => p.UzmanlikAlanlari)
                .HasConversion(
                    v => string.Join(",", v),  // Listeyi string'e çevir
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
                )
                .Metadata
                .SetValueComparer(new ValueComparer<List<int>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));
        }
    }
}