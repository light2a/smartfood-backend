using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public partial class SmartFoodContext : DbContext
    {
        public SmartFoodContext(DbContextOptions<SmartFoodContext> options) : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA5A646603AB1");

                entity.ToTable("Account");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.Property(e => e.Email)
                    .HasMaxLength(255)   // ✅ FIXED
                    .IsRequired();

                entity.Property(e => e.FullName)
                    .HasMaxLength(255);  // ✅ FIXED

                entity.Property(e => e.Password)
                    .HasMaxLength(255)   // ✅ Recommended
                    .IsRequired();

                entity.Property(e => e.UpdateAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK__Account__Roleid__3B75D760");

                entity.Property(e => e.ExternalProviderKey).HasMaxLength(100);
                entity.Property(e => e.ExternalProvider).HasMaxLength(100);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1ADC36AA35");

                entity.ToTable("Role");

                entity.Property(e => e.RoleName).HasMaxLength(20);
                entity.HasData(
                    new Role { RoleId = 1, RoleName = "Customer" },
                    new Role { RoleId = 2, RoleName = "Seller" },
                    new Role { RoleId = 3, RoleName = "Admin" }
                );
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
