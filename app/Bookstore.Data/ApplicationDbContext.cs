using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        // PORT-TODO
        // {"was": "System.Data.Entity.DbContext(string)", "note": "The constructor now takes DbContextOptions<ApplicationDbContext> instead of a connection string. The host must register the context, e.g. services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString)), and call the seeding routine (replacement for BookstoreDbInitializer) at startup.", "files": ["Bookstore.Data/BookstoreDbInitializer.cs"]}
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Address> Address { get; set; }

        public DbSet<Book> Book { get; set; }

        public DbSet<Customer> Customer { get; set; }

        public DbSet<Order> Order { get; set; }

        public DbSet<ShoppingCart> ShoppingCart { get; set; }

        public DbSet<OrderItem> OrderItem { get; set; }

        public DbSet<Offer> Offer { get; set; }

        public DbSet<ReferenceDataItem> ReferenceData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // EF Core does not pluralize table names: tables take the DbSet property names
            // (already singular), matching the EF6 schema without PluralizingTableNameConvention.

            modelBuilder.Entity<Customer>().Property(x => x.Sub).HasMaxLength(450);
            modelBuilder.Entity<Customer>().HasIndex(x => x.Sub).IsUnique();

            modelBuilder.Entity<Book>().HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>().HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).IsRequired().OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Offer>().HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Offer>().HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).IsRequired().OnDelete(DeleteBehavior.Restrict);

            // EF6 configured this relationship without HasForeignKey, which produced an independent
            // association stored in a "Customer_Id" column (Order.CustomerId remained a plain column).
            // Preserve that schema with an explicit shadow FK mapped to "Customer_Id".
            // PORT-TODO
            // {"was": "System.Data.Entity.ModelConfiguration.Configuration.RequiredNavigationPropertyConfiguration.WithMany", "note": "Order->Customer uses shadow FK column Customer_Id to keep the EF6 schema; Order.CustomerId is still a separate plain column and does not drive the relationship (same as EF6). Consider consolidating onto CustomerId in a later schema migration."}
            modelBuilder.Entity<Order>().Property<int>("Customer_Id").HasColumnName("Customer_Id");
            modelBuilder.Entity<Order>().HasOne(x => x.Customer).WithMany().HasForeignKey("Customer_Id").IsRequired().OnDelete(DeleteBehavior.Restrict);

            // Update the Refernce Data Table to Match the modern version
            modelBuilder.Entity<ReferenceDataItem>().ToTable("ReferenceData");

            modelBuilder.Entity<ShoppingCartItem>().ToTable("ShoppingCartItem");
            modelBuilder.Entity<ShoppingCartItem>().HasKey(x => new { x.Id, x.ShoppingCartId });
            modelBuilder.Entity<ShoppingCartItem>().Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();

            // Database.SetInitializer(new BookstoreDbInitializer()) was removed: EF Core has no initializers.
            // Seeding is performed by the routine in BookstoreDbInitializer that the host calls at startup.
        }
    }
}