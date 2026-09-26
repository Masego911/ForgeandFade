using ForgeAndFade.Api.Models; // Imports the model classes so this database context can expose them as database tables.

using Microsoft.EntityFrameworkCore; // Imports Entity Framework Core types such as DbContext and DbSet.

namespace ForgeAndFade.Api.Data // Places this class inside the Data namespace because it is responsible for database access configuration.
{
    public class ApplicationDbContext : DbContext // Inherits from DbContext, which is Entity Framework Core's main class for communicating with a database.
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) // Receives database configuration from dependency injection and passes it to the base DbContext class.
        {
        }

        public DbSet<Customer> Customers { get; set; } // Represents the Customers table and allows Entity Framework to query and save Customer objects.

        public DbSet<Barber> Barbers { get; set; } // Represents the Barbers table and allows Entity Framework to query and save Barber objects.

        public DbSet<Service> Services { get; set; } // Represents the Services table and allows Entity Framework to query and save Service objects.

        public DbSet<Booking> Bookings { get; set; } // Represents the Bookings table and allows Entity Framework to query and save Booking objects.

        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; } // Represents the LoyaltyAccounts table and allows Entity Framework to query and save loyalty account objects.

        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; } // Represents the LoyaltyTransactions table and allows Entity Framework to query and save loyalty transaction objects.

        protected override void OnModelCreating(ModelBuilder modelBuilder) // Overrides Entity Framework's model-building method so we can configure relationships and database rules explicitly.
        {
            base.OnModelCreating(modelBuilder); // Runs Entity Framework Core's normal model configuration before applying our custom rules.

            modelBuilder.Entity<Booking>()
                .Property(booking => booking.Status)
                .HasConversion<string>(); // Stores enum names in the nvarchar(max) column created by InitialCreate.

            modelBuilder.Entity<Customer>() // Begins configuration for the Customer entity.
                .HasIndex(customer => customer.Email) // Creates a database index on the customer's email address.
                .IsUnique(); // Prevents two customer records from having the same email address.

            modelBuilder.Entity<Customer>() // Begins configuration of the relationship between Customer and PreferredBarber.
                .HasOne(customer => customer.PreferredBarber) // States that a customer may have one preferred barber.
                .WithMany() // States that we are not currently creating a collection of preferred customers on the Barber class.
                .HasForeignKey(customer => customer.PreferredBarberId) // Uses PreferredBarberId as the foreign key for this relationship.
                .OnDelete(DeleteBehavior.SetNull); // Sets PreferredBarberId to null if that barber is deleted instead of deleting the customer.

            modelBuilder.Entity<Booking>() // Begins configuration of the relationship between Booking and Customer.
                .HasOne(booking => booking.Customer) // States that every booking belongs to one customer.
                .WithMany(customer => customer.Bookings) // States that one customer may have many bookings.
                .HasForeignKey(booking => booking.CustomerId) // Uses CustomerId as the foreign key stored in the Booking table.
                .OnDelete(DeleteBehavior.Restrict); // Prevents a customer from being deleted automatically when bookings depend on that customer.

            modelBuilder.Entity<Booking>() // Begins configuration of the relationship between Booking and Barber.
                .HasOne(booking => booking.Barber) // States that every booking belongs to one barber.
                .WithMany() // States that the Barber class does not currently contain a Bookings collection.
                .HasForeignKey(booking => booking.BarberId) // Uses BarberId as the foreign key in the Booking table.
                .OnDelete(DeleteBehavior.Restrict); // Prevents deleting a barber when bookings still reference that barber.

            modelBuilder.Entity<Booking>() // Begins configuration of the relationship between Booking and Service.
                .HasOne(booking => booking.Service) // States that every booking belongs to one service.
                .WithMany() // States that the Service class does not currently contain a Bookings collection.
                .HasForeignKey(booking => booking.ServiceId) // Uses ServiceId as the foreign key stored in the Booking table.
                .OnDelete(DeleteBehavior.Restrict); // Prevents deleting a service if bookings still reference it.

            modelBuilder.Entity<LoyaltyAccount>() // Begins configuration of the loyalty account entity.
                .HasOne(loyaltyAccount => loyaltyAccount.Customer) // States that every loyalty account belongs to one customer.
                .WithOne(customer => customer.LoyaltyAccount) // States that every customer may have one loyalty account.
                .HasForeignKey<LoyaltyAccount>(loyaltyAccount => loyaltyAccount.CustomerId) // Uses CustomerId as the foreign key in LoyaltyAccount.
                .OnDelete(DeleteBehavior.Cascade); // Deletes the loyalty account automatically if its customer is deleted.

            modelBuilder.Entity<LoyaltyTransaction>() // Begins configuration of the relationship between LoyaltyTransaction and LoyaltyAccount.
                .HasOne(transaction => transaction.LoyaltyAccount) // States that every loyalty transaction belongs to one loyalty account.
                .WithMany(account => account.Transactions) // States that one loyalty account can contain many transactions.
                .HasForeignKey(transaction => transaction.LoyaltyAccountId) // Uses LoyaltyAccountId as the foreign key.
                .OnDelete(DeleteBehavior.Cascade); // Deletes loyalty transactions automatically if their loyalty account is deleted.

            modelBuilder.Entity<LoyaltyTransaction>() // Begins configuration of the optional relationship between LoyaltyTransaction and Booking.
                .HasOne(transaction => transaction.Booking) // States that a loyalty transaction may be connected to one booking.
                .WithMany() // States that Booking does not currently contain a loyalty transaction collection.
                .HasForeignKey(transaction => transaction.BookingId) // Uses BookingId as the optional foreign key.
                .OnDelete(DeleteBehavior.SetNull); // Keeps the loyalty transaction but clears BookingId if the related booking is deleted.

            modelBuilder.Entity<Service>() // Begins configuration for the Service entity.
                .Property(service => service.Price) // Selects the Price property for database configuration.
                .HasColumnType("decimal(10,2)"); // Stores prices with up to ten digits in total and two digits after the decimal point.
        }
    }
}
