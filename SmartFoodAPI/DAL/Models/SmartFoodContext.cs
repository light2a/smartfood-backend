using Microsoft.EntityFrameworkCore;
using System;

namespace DAL.Models
{
    public partial class SmartFoodContext : DbContext
    {
        public SmartFoodContext(DbContextOptions<SmartFoodContext> options) : base(options) { }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Area> Areas { get; set; }
        public virtual DbSet<Seller> Sellers { get; set; }
        public virtual DbSet<Restaurant> Restaurants { get; set; }
        public virtual DbSet<MenuItem> MenuItems { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public virtual DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }
        public virtual DbSet<Feedback> Feedbacks { get; set; }
        public virtual DbSet<Category> Categories { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Account ===
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.Property(e => e.AccountId).ValueGeneratedOnAdd();
                entity.ToTable("Account");

                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(255);
                entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                entity.Property(e => e.UpdateAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                entity.Property(e => e.ExternalProviderKey).HasMaxLength(100);
                entity.Property(e => e.ExternalProvider).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(15);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_Account_Role");
            });

            // === Role ===
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).ValueGeneratedOnAdd();
                entity.ToTable("Role");

                entity.Property(e => e.RoleName).HasMaxLength(20);
                entity.HasData(
                    new Role { RoleId = 1, RoleName = "Customer" },
                    new Role { RoleId = 2, RoleName = "Seller" },
                    new Role { RoleId = 3, RoleName = "Admin" }
                );
            });

            // === Area ===
            modelBuilder.Entity<Area>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.City).HasMaxLength(200);
            });

            // === Seller ===
            modelBuilder.Entity<Seller>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

                entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasDefaultValue(SellerStatus.Unavailable)
                  .IsRequired();

                entity.Property(e => e.BankCode)
                    .HasConversion<string>();

                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // === Restaurant ===
            modelBuilder.Entity<Restaurant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(250).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(r => r.Seller)
                    .WithMany(s => s.Restaurants)
                    .HasForeignKey(r => r.SellerId);

                entity.HasOne(r => r.Area)
                    .WithMany(a => a.Restaurants)
                    .HasForeignKey(r => r.AreaId);

                entity.HasMany(r => r.Categories)
                    .WithOne(c => c.Restaurant)
                    .HasForeignKey(c => c.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // === MenuItem ===
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(250).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .HasDefaultValue(MenuItemStatus.DangBan);
                entity.Property(e => e.LogoUrl).HasMaxLength(500);

                entity.HasOne(m => m.Category)
                    .WithMany(c => c.MenuItems)
                    .HasForeignKey(m => m.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Restaurant)
                    .WithMany(r => r.MenuItems)
                    .HasForeignKey(m => m.RestaurantId);
            });

            // === Order ===
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.CommissionPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(e => e.FinalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.OrderType).HasMaxLength(50).IsRequired();

                entity.HasOne(o => o.Customer)
                    .WithMany()
                    .HasForeignKey(o => o.CustomerAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Restaurant)
                    .WithMany(r => r.Orders)
                    .HasForeignKey(o => o.RestaurantId);
            });

            // === OrderItem ===
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Qty).HasDefaultValue(1);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.MenuItem)
                    .WithMany(m => m.OrderItems)
                    .HasForeignKey(oi => oi.MenuItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // === OrderStatusHistory ===
            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Status).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(os => os.Order)
                    .WithMany(o => o.StatusHistory)
                    .HasForeignKey(os => os.OrderId);
            });

            // === LoyaltyPoint ===
            modelBuilder.Entity<LoyaltyPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Points).HasDefaultValue(0);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(lp => lp.User)
                    .WithMany()
                    .HasForeignKey(lp => lp.UserAccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === Feedback ===
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasOne(f => f.Order)
                    .WithMany(o => o.Feedbacks)
                    .HasForeignKey(f => f.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Customer)
                    .WithMany()
                    .HasForeignKey(f => f.CustomerAccountId);
            });


            // === Category ===
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasMany(c => c.MenuItems)
                    .WithOne(m => m.Category)
                    .HasForeignKey(m => m.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
