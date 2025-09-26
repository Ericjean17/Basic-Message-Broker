using MessageBroker.Models; // import data models
using Microsoft.EntityFrameworkCore; // import EF Core functionality

namespace MessageBroker.Data
{
  // AppDbContext inherits from EF Core's base database context class
  // DbContext acts as a bridge between C# objects and database
  public class AppDbContext : DbContext
  {
    // Takes DbContextOptions<AppDbContext> and passes it to the base class
    // to configure database connection settings. DbContextOptions tells EF Core
    // how to connect to the database with a connection string, db provider
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    // Creates a "Topics", "Subsscriptions", and "Messages" table mapped to their model
    // Each DbSet<T> is a database table. EF Core uses these to generate SQL commands for
    // CRUD operations. => Set<T>() returns the corresponding database table set
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Message> Messages => Set<Message>();
  }
}