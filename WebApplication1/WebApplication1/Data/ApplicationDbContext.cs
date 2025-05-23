using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WasselniAPI.Models;

namespace WasselniAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DriverLocation> DriverLocations { get; set; }
        public DbSet<RideTracking> RideTrackings { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.HasIndex(u => u.PhoneNumber)
                    .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(u => u.Address)
                    .HasMaxLength(200);

                entity.Property(u => u.UserType)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(u => u.DriverStatus)
                    .HasConversion<string>();

                entity.Property(u => u.CurrentLat)
                    .HasPrecision(10, 7);

                entity.Property(u => u.CurrentLng)
                    .HasPrecision(10, 7);

                entity.Property(u => u.Rating)
                    .HasPrecision(3, 2);

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasMany(u => u.CustomerRides)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.DriverRides)
                    .WithOne(r => r.Driver)
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(u => u.GivenRatings)
                    .WithOne(r => r.RatingGivenByUser)
                    .HasForeignKey(r => r.RatingGivenByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.ReceivedRatings)
                    .WithOne(r => r.RatedUser)
                    .HasForeignKey(r => r.RatedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.Notifications)
                    .WithOne(n => n.User)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Car Configuration
            modelBuilder.Entity<Car>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.PlateNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(c => c.PlateNumber)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");

                entity.Property(c => c.Make)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.Model)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.Color)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(c => c.ImageUrl)
                    .HasMaxLength(200);

                entity.Property(c => c.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                // Relationships
                entity.HasOne(c => c.Driver)
                    .WithOne(u => u.Car)
                    .HasForeignKey<Car>(c => c.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ride Configuration
            modelBuilder.Entity<Ride>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.PickupLat)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(r => r.PickupLng)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(r => r.DropoffLat)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(r => r.DropoffLng)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(r => r.PickupAddress)
                    .HasMaxLength(200);

                entity.Property(r => r.DropoffAddress)
                    .HasMaxLength(200);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(r => r.DistanceKm)
                    .HasPrecision(10, 2);

                entity.Property(r => r.EstimatedFare)
                    .HasPrecision(10, 2);

                entity.Property(r => r.ActualFare)
                    .HasPrecision(10, 2);

                entity.Property(r => r.CancellationReason)
                    .HasMaxLength(500);

                entity.Property(r => r.Notes)
                    .HasMaxLength(1000);

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(r => r.CustomerId);
                entity.HasIndex(r => r.DriverId);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);

                // Relationships
                entity.HasOne(r => r.Customer)
                    .WithMany(u => u.CustomerRides)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Driver)
                    .WithMany(u => u.DriverRides)
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(r => r.Ratings)
                    .WithOne(rt => rt.Ride)
                    .HasForeignKey(rt => rt.RideId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(r => r.TrackingPoints)
                    .WithOne(rt => rt.Ride)
                    .HasForeignKey(rt => rt.RideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Payment Configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Amount)
                    .IsRequired()
                    .HasPrecision(10, 2);

                entity.Property(p => p.PaymentMethod)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(p => p.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(p => p.TransactionId)
                    .HasMaxLength(100);

                entity.HasIndex(p => p.TransactionId)
                    .IsUnique()
                    .HasFilter("[TransactionId] IS NOT NULL");

                entity.Property(p => p.Notes)
                    .HasMaxLength(500);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(p => p.RideId)
                    .IsUnique();
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.CreatedAt);

                // Relationships
                entity.HasOne(p => p.Ride)
                    .WithOne(r => r.Payment)
                    .HasForeignKey<Payment>(p => p.RideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Rating Configuration
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Score)
                    .IsRequired()
                    .HasAnnotation("Range", new[] { 1, 5 });

                entity.Property(r => r.Comment)
                    .HasMaxLength(500);

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Composite unique constraint
                entity.HasIndex(r => new { r.RideId, r.RatedUserId, r.RatingGivenByUserId })
                    .IsUnique();

                // Indexes
                entity.HasIndex(r => r.RatedUserId);
                entity.HasIndex(r => r.RatingGivenByUserId);
                entity.HasIndex(r => r.CreatedAt);

                // Relationships configured in User entity
            });

            // Pricing Configuration
            modelBuilder.Entity<Pricing>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.BaseFare)
                    .IsRequired()
                    .HasPrecision(10, 2);

                entity.Property(p => p.PerKmRate)
                    .IsRequired()
                    .HasPrecision(10, 2);

                entity.Property(p => p.PerMinuteRate)
                    .IsRequired()
                    .HasPrecision(10, 2);

                entity.Property(p => p.MinimumFare)
                    .IsRequired()
                    .HasPrecision(10, 2);

                entity.Property(p => p.PeakHourMultiplier)
                    .HasPrecision(5, 2)
                    .HasDefaultValue(1.0m);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(p => p.IsActive)
                    .HasDefaultValue(false);

                // Indexes
                entity.HasIndex(p => p.IsActive);
                entity.HasIndex(p => p.CreatedAt);
            });

            // Notification Configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Type)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(n => n.Data)
                    .HasMaxLength(1000);

                entity.Property(n => n.IsRead)
                    .HasDefaultValue(false);

                entity.Property(n => n.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.CreatedAt);

                // Relationships
                entity.HasOne(n => n.Ride)
                    .WithMany()
                    .HasForeignKey(n => n.RideId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // DriverLocation Configuration
            modelBuilder.Entity<DriverLocation>(entity =>
            {
                entity.HasKey(dl => dl.Id);

                entity.Property(dl => dl.Latitude)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(dl => dl.Longitude)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(dl => dl.Speed)
                    .HasPrecision(5, 2);

                entity.Property(dl => dl.Heading)
                    .HasPrecision(5, 2);

                entity.Property(dl => dl.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(dl => dl.DriverId);
                entity.HasIndex(dl => dl.Timestamp);
                entity.HasIndex(dl => new { dl.DriverId, dl.Timestamp });

                // Relationships
                entity.HasOne(dl => dl.Driver)
                    .WithMany()
                    .HasForeignKey(dl => dl.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RideTracking Configuration
            modelBuilder.Entity<RideTracking>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.Latitude)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(rt => rt.Longitude)
                    .IsRequired()
                    .HasPrecision(10, 7);

                entity.Property(rt => rt.Speed)
                    .HasPrecision(5, 2);

                entity.Property(rt => rt.Heading)
                    .HasPrecision(5, 2);

                entity.Property(rt => rt.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(rt => rt.RideId);
                entity.HasIndex(rt => rt.Timestamp);
                entity.HasIndex(rt => new { rt.RideId, rt.Timestamp });
            });

            // RideRequest Configuration
            modelBuilder.Entity<RideRequest>(entity =>
            {
                entity.HasKey(rr => rr.Id);

                entity.Property(rr => rr.SentAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(rr => rr.ExpiresAt)
                    .IsRequired();

                // Indexes
                entity.HasIndex(rr => rr.RideId);
                entity.HasIndex(rr => rr.DriverId);
                entity.HasIndex(rr => rr.ExpiresAt);
                entity.HasIndex(rr => new { rr.DriverId, rr.ExpiresAt });

                // Relationships
                entity.HasOne(rr => rr.Ride)
                    .WithMany()
                    .HasForeignKey(rr => rr.RideId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rr => rr.Driver)
                    .WithMany()
                    .HasForeignKey(rr => rr.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Default Pricing
            modelBuilder.Entity<Pricing>().HasData(
                new Pricing
                {
                    Id = 1,
                    Name = "Jordan Standard Pricing",
                    BaseFare = 0.50m,
                    PerKmRate = 0.28m,
                    PerMinuteRate = 0.05m,
                    MinimumFare = 1.10m,
                    PeakHourMultiplier = 1.20m,
                    MorningPeakStart = new TimeSpan(7, 0, 0),
                    MorningPeakEnd = new TimeSpan(9, 0, 0),
                    EveningPeakStart = new TimeSpan(15, 0, 0),
                    EveningPeakEnd = new TimeSpan(19, 0, 0),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "Wassel Admin",
                    Email = "admin@wassel.jo",
                    PhoneNumber = "962777123456",
                    UserType = UserType.Customer,
                    PasswordHash = "AQAAAAEAACcQAAAAEJ3Rz0J5w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8", // "Admin123!"
                    Address = "Amman, Jordan",
                    IsLoggedIn = false,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        // Override SaveChanges to handle automatic timestamps and audit fields
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            HandleAuditFields();
            return base.SaveChanges();
        }

        private void HandleAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Property("CreatedAt") != null)
                    {
                        entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    if (entry.Property("UpdatedAt") != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    }

                    // Prevent modification of CreatedAt
                    if (entry.Property("CreatedAt") != null)
                    {
                        entry.Property("CreatedAt").IsModified = false;
                    }
                }
            }
        }

        // Add method to create database and seed initial data
        public async Task EnsureDatabaseCreatedAsync()
        {
            await Database.EnsureCreatedAsync();
        }

        // Method to apply pending migrations
        public async Task MigrateAsync()
        {
            await Database.MigrateAsync();
        }
    }

    // Extension methods for DbContext
    public static class ApplicationDbContextExtensions
    {
        public static async Task<bool> CanConnectAsync(this ApplicationDbContext context)
        {
            try
            {
                return await context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public static async Task SeedTestDataAsync(this ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.UserType == UserType.Driver))
            {
                // Add test driver
                var testDriver = new User
                {
                    FullName = "Ahmed Al-Driver",
                    Email = "driver@wassel.jo",
                    PhoneNumber = "962777654321",
                    UserType = UserType.Driver,
                    PasswordHash = "AQAAAAEAACcQAAAAEJ3Rz0J5w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8",
                    Address = "Amman, Jordan",
                    DriverStatus = DriverStatus.Offline,
                    Rating = 4.8,
                    TotalTrips = 0,
                    IsLoggedIn = false,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(testDriver);
                await context.SaveChangesAsync();

                // Add test car for driver
                var testCar = new Car
                {
                    DriverId = testDriver.Id,
                    PlateNumber = "12345",
                    Make = "Toyota",
                    Model = "Camry",
                    Year = 2020,
                    Color = "White",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Cars.Add(testCar);
                await context.SaveChangesAsync();
            }

            if (!context.Users.Any(u => u.UserType == UserType.Customer && u.Email != "admin@wassel.jo"))
            {
                // Add test customer
                var testCustomer = new User
                {
                    FullName = "Sara Al-Customer",
                    Email = "customer@wassel.jo",
                    PhoneNumber = "962777987654",
                    UserType = UserType.Customer,
                    PasswordHash = "AQAAAAEAACcQAAAAEJ3Rz0J5w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8o8O7c2Q8w5Q9v5K8",
                    Address = "Amman, Jordan",
                    IsLoggedIn = false,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(testCustomer);
                await context.SaveChangesAsync();
            }
        }
    }
}
