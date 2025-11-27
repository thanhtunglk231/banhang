#pragma warning disable CS8618
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace webBanThucPham.Models
{
    public partial class DbBanThucPhamContext : DbContext
    {
        public DbBanThucPhamContext() { }

        public DbBanThucPhamContext(DbContextOptions<DbBanThucPhamContext> options)
            : base(options) { }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Attribute> Attributes { get; set; }
        public virtual DbSet<Attributesprice> Attributesprices { get; set; }
        public virtual DbSet<Cart> Carts { get; set; }
        public virtual DbSet<Cartitem> Cartitems { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Deliveryaddress> Deliveryaddresses { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Orderdetail> Orderdetails { get; set; }
        public virtual DbSet<Page> Pages { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Shipper> Shippers { get; set; }
        public virtual DbSet<Tintuc> Tintucs { get; set; }
        public virtual DbSet<Transactstatuss> Transactstatusses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning TODO: Di chuyển connection string sang appsettings.json và cấu hình qua DI.
                optionsBuilder.UseSqlServer(
                    "Server=localhost;Database=webBanThucPham;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Collation cho SQL Server (tùy chọn)
            modelBuilder.UseCollation("Vietnamese_100_CI_AS");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.ToTable("accounts");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.RoleId);

                entity.Property(e => e.AccountId).HasColumnName("AccountID");
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreateDate).HasColumnType("datetime");
                entity.Property(e => e.Email).HasMaxLength(50);
                entity.Property(e => e.FullName).HasMaxLength(150);
                entity.Property(e => e.LastLogin).HasColumnType("datetime");
                entity.Property(e => e.Password).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(12);
                entity.Property(e => e.RoleId).HasColumnName("RoleID");
                entity.Property(e => e.Salt).HasMaxLength(10).IsFixedLength();

                entity.HasOne(d => d.Role)
                      .WithMany(p => p.Accounts)
                      .HasForeignKey(d => d.RoleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Attribute>(entity =>
            {
                entity.HasKey(e => e.AttributeId);
                entity.ToTable("attributes");

                entity.Property(e => e.AttributeId).HasColumnName("AttributeID");
                entity.Property(e => e.Name).HasMaxLength(250);
            });

            modelBuilder.Entity<Attributesprice>(entity =>
            {
                entity.HasKey(e => e.AttributesPriceId);
                entity.ToTable("attributesprices");

                entity.HasIndex(e => e.AttributeId);
                entity.HasIndex(e => e.ProductId);

                entity.Property(e => e.AttributesPriceId).HasColumnName("AttributesPriceID");
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.AttributeId).HasColumnName("AttributeID");
                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.HasOne(d => d.Attribute)
                      .WithMany(p => p.Attributesprices)
                      .HasForeignKey(d => d.AttributeId);

                entity.HasOne(d => d.Product)
                      .WithMany(p => p.Attributesprices)
                      .HasForeignKey(d => d.ProductId);
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.CartId);
                entity.ToTable("cart");

                entity.HasIndex(e => e.CustomerId);

                entity.Property(e => e.CartId).HasColumnName("CartID");
                entity.Property(e => e.CreatedDate)
                      .HasColumnType("datetime")
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.HasOne(d => d.Customer)
                      .WithMany(p => p.Carts)
                      .HasForeignKey(d => d.CustomerId);
            });

            modelBuilder.Entity<Cartitem>(entity =>
            {
                entity.HasKey(e => e.CartItemId);
                entity.ToTable("cartitem");

                entity.HasIndex(e => e.CartId);
                entity.HasIndex(e => e.ProductId);

                entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
                entity.Property(e => e.CartId).HasColumnName("CartID");
                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.Quantity).HasDefaultValue(1);

                entity.HasOne(d => d.Cart)
                      .WithMany(p => p.Cartitems)
                      .HasForeignKey(d => d.CartId);

                entity.HasOne(d => d.Product)
                      .WithMany(p => p.Cartitems)
                      .HasForeignKey(d => d.ProductId);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CatId);
                entity.ToTable("categories");

                entity.HasIndex(e => e.ParentId);

                entity.Property(e => e.CatId).HasColumnName("CatID");
                entity.Property(e => e.Alias).HasMaxLength(250);
                entity.Property(e => e.CatName).HasMaxLength(50);
                entity.Property(e => e.Cover).HasMaxLength(250);
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
                entity.Property(e => e.MetaDesc).HasMaxLength(250);
                entity.Property(e => e.MetaKey).HasMaxLength(250);
                entity.Property(e => e.ParentId).HasColumnName("ParentID");
                entity.Property(e => e.Published).HasDefaultValue(true);
                entity.Property(e => e.SchemalMarkup).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Thumb).HasMaxLength(250);
                entity.Property(e => e.Title).HasMaxLength(250);

                // Quan hệ tự tham chiếu -> NO ACTION để tránh multiple cascade paths
                entity.HasOne(d => d.Parent)
                      .WithMany(p => p.InverseParent)
                      .HasForeignKey(d => d.ParentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                entity.ToTable("customers");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.LocationId);
                entity.HasIndex(e => e.Phone).IsUnique();

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.Avatar).HasMaxLength(255);
                entity.Property(e => e.Birthday).HasColumnType("datetime");
                entity.Property(e => e.CreateDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.FullName).HasMaxLength(255);
                entity.Property(e => e.LastLogin).HasColumnType("datetime");
                entity.Property(e => e.LocationId).HasColumnName("LocationID");
                entity.Property(e => e.Password).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(12);
                entity.Property(e => e.Salt).HasMaxLength(50).IsFixedLength();

                entity.HasOne(d => d.Location)
                      .WithMany(p => p.Customers)
                      .HasForeignKey(d => d.LocationId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Deliveryaddress>(entity =>
            {
                entity.HasKey(e => e.DeliveryAddressId);
                entity.ToTable("deliveryaddresses");

                entity.HasIndex(e => e.CustomerId);

                entity.Property(e => e.DeliveryAddressId).HasColumnName("DeliveryAddressID");
                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
                entity.Property(e => e.NameAddress).HasColumnType("nvarchar(max)");
                entity.Property(e => e.PhoneNumber).HasMaxLength(12);

                entity.HasOne(d => d.Customer)
                      .WithMany(p => p.Deliveryaddresses)
                      .HasForeignKey(d => d.CustomerId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(e => e.LocationId);
                entity.ToTable("locations");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.NameWithType).HasMaxLength(255);
                entity.Property(e => e.PathWithType).HasMaxLength(255);
                entity.Property(e => e.Slug).HasMaxLength(100);
                entity.Property(e => e.Type).HasMaxLength(20);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.ToTable("orders");

                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.DeliveryAddressId);
                entity.HasIndex(e => e.PaymentMethodId);

                entity.Property(e => e.OrderId).HasColumnName("OrderID");
                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
                entity.Property(e => e.DeliveryAddressId).HasColumnName("DeliveryAddressID");
                entity.Property(e => e.Note).HasColumnType("nvarchar(max)");
                entity.Property(e => e.OrderDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.PaymentDate).HasColumnType("datetime");
                entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
                entity.Property(e => e.PaymentMethodId).HasColumnName("PaymentMethodID");
                entity.Property(e => e.ShipDate).HasColumnType("datetime");
                entity.Property(e => e.TransactStatusId).HasColumnName("TransactStatusID");

                entity.HasOne(d => d.Customer)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.CustomerId);

                entity.HasOne(d => d.DeliveryAddress)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.DeliveryAddressId);

                entity.HasOne(d => d.PaymentMethod)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.PaymentMethodId);
            });

            modelBuilder.Entity<Orderdetail>(entity =>
            {
                entity.HasKey(e => e.OrderDetailId);
                entity.ToTable("orderdetails");

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);

                entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
                entity.Property(e => e.OrderId).HasColumnName("OrderID");
                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.ShipDate).HasColumnType("datetime");

                entity.HasOne(d => d.Order)
                      .WithMany(p => p.Orderdetails)
                      .HasForeignKey(d => d.OrderId);

                entity.HasOne(d => d.Product)
                      .WithMany(p => p.Orderdetails)
                      .HasForeignKey(d => d.ProductId);
            });

            modelBuilder.Entity<Page>(entity =>
            {
                entity.HasKey(e => e.PageId);
                entity.ToTable("pages");

                entity.HasIndex(e => e.Alias).IsUnique();

                entity.Property(e => e.PageId).HasColumnName("PageID");
                entity.Property(e => e.Alias).HasMaxLength(250);
                entity.Property(e => e.Contents).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CreateDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.MetaDesc).HasMaxLength(250);
                entity.Property(e => e.MetaKey).HasMaxLength(250);
                entity.Property(e => e.PageName).HasMaxLength(250);
                entity.Property(e => e.Published).HasDefaultValue(true);
                entity.Property(e => e.Thumb).HasMaxLength(250);
                entity.Property(e => e.Title).HasMaxLength(250);
            });

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(e => e.PaymentMethodId);
                entity.ToTable("payment_methods");

                entity.Property(e => e.PaymentMethodId).HasColumnName("PaymentMethodID");
                entity.Property(e => e.Description)
                      .HasColumnType("nvarchar(max)")
                      .HasColumnName("description");
                entity.Property(e => e.MethodName)
                      .HasColumnType("nvarchar(max)")
                      .HasColumnName("method_name");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.ToTable("products");

                entity.HasIndex(e => e.CatId);

                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.Alias).HasMaxLength(255);
                entity.Property(e => e.CatId).HasColumnName("CatID");
                entity.Property(e => e.DateCreated).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.DateModified).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
                entity.Property(e => e.MetaDesc).HasMaxLength(255);
                entity.Property(e => e.MetaKey).HasMaxLength(255);
                entity.Property(e => e.ProductName).HasMaxLength(255);
                entity.Property(e => e.ShortDesc).HasMaxLength(255);
                entity.Property(e => e.Tags).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Thumb).HasMaxLength(255);
                entity.Property(e => e.Thumbnail).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Title).HasMaxLength(255);
                entity.Property(e => e.Video).HasMaxLength(255);

                entity.HasOne(d => d.Cat)
                      .WithMany(p => p.Products)
                      .HasForeignKey(d => d.CatId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.ToTable("roles");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.RoleName).HasMaxLength(50);
            });

            modelBuilder.Entity<Shipper>(entity =>
            {
                entity.HasKey(e => e.ShipperId);
                entity.ToTable("shippers");

                entity.Property(e => e.ShipperId).HasColumnName("ShipperID");
                entity.Property(e => e.Company).HasMaxLength(150);
                entity.Property(e => e.Phone).HasMaxLength(10).IsFixedLength();
                entity.Property(e => e.ShipDate).HasColumnType("datetime");
                entity.Property(e => e.ShipperName).HasMaxLength(150);
            });

            modelBuilder.Entity<Tintuc>(entity =>
            {
                entity.HasKey(e => e.PostId);
                entity.ToTable("tintucs");

                entity.HasIndex(e => e.AccountId);
                entity.HasIndex(e => e.CatId);

                entity.Property(e => e.PostId).HasColumnName("PostID");
                entity.Property(e => e.AccountId).HasColumnName("AccountID");
                entity.Property(e => e.Alias).HasMaxLength(255);
                entity.Property(e => e.CatId).HasColumnName("CatID");
                entity.Property(e => e.Contents).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
                entity.Property(e => e.IsHot).HasColumnType("bit").HasDefaultValue(false);
                entity.Property(e => e.IsNewfeed).HasColumnType("bit").HasDefaultValue(false);
                entity.Property(e => e.MetaDesc).HasMaxLength(255);
                entity.Property(e => e.MetaKey).HasMaxLength(255);
                entity.Property(e => e.Scontents).HasMaxLength(255).HasColumnName("SContents");
                entity.Property(e => e.Tags).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Thum).HasMaxLength(255);
                entity.Property(e => e.Title).HasMaxLength(255);

                entity.HasOne(d => d.Account)
                      .WithMany(p => p.Tintucs)
                      .HasForeignKey(d => d.AccountId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Cat)
                      .WithMany(p => p.Tintucs)
                      .HasForeignKey(d => d.CatId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Transactstatuss>(entity =>
            {
                entity.HasKey(e => e.TracsactStatusId);
                entity.ToTable("transactstatuss");

                entity.Property(e => e.TracsactStatusId).HasColumnName("TracsactStatusID");
                entity.Property(e => e.Descripstion).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Status).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
