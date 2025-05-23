
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WasselniAPI.Models
{
    // Enums
    public enum UserType
    {
        Customer,
        Driver
    }

    public enum RideStatus
    {
        Requested,
        Accepted,
        Arrived,
        InProgress,
        Completed,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        DigitalWallet
    }

    public enum DriverStatus
    {
        Offline,
        Online,
        Busy,
        OnBreak
    }

    public enum NotificationType
    {
        RideRequest,
        RideAccepted,
        DriverArrived,
        TripStarted,
        TripCompleted,
        RideCancelled,
        PaymentReceived,
        General
    }

    // User Model
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserType UserType { get; set; }

        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\d{9,15}$", ErrorMessage = "Phone number must be 9-15 digits")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        public double? CurrentLat { get; set; }
        public double? CurrentLng { get; set; }

        public bool IsLoggedIn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Driver-specific properties
        public DriverStatus? DriverStatus { get; set; }
        public double? Rating { get; set; }
        public int? TotalTrips { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public Car? Car { get; set; }

        [JsonIgnore]
        public List<Ride> CustomerRides { get; set; } = new();

        [JsonIgnore]
        public List<Ride> DriverRides { get; set; } = new();

        [JsonIgnore]
        public List<Rating> GivenRatings { get; set; } = new();

        [JsonIgnore]
        public List<Rating> ReceivedRatings { get; set; } = new();

        [JsonIgnore]
        public List<Notification> Notifications { get; set; } = new();
    }

    // Car Model
    public class Car
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(30)]
        public string Color { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("DriverId")]
        public User Driver { get; set; } = null!;
    }

    // Ride Model
    public class Ride
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? DriverId { get; set; }

        [Required]
        public double PickupLat { get; set; }

        [Required]
        public double PickupLng { get; set; }

        [StringLength(200)]
        public string PickupAddress { get; set; } = string.Empty;

        [Required]
        public double DropoffLat { get; set; }

        [Required]
        public double DropoffLng { get; set; }

        [StringLength(200)]
        public string DropoffAddress { get; set; } = string.Empty;

        [Required]
        public RideStatus Status { get; set; } = RideStatus.Requested;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public double? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal? EstimatedFare { get; set; }
        public decimal? ActualFare { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public User Customer { get; set; } = null!;

        [ForeignKey("DriverId")]
        public User? Driver { get; set; }

        [JsonIgnore]
        public Payment? Payment { get; set; }

        [JsonIgnore]
        public List<Rating> Ratings { get; set; } = new();

        [JsonIgnore]
        public List<RideTracking> TrackingPoints { get; set; } = new();
    }

    // Payment Model
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("RideId")]
        public Ride Ride { get; set; } = null!;
    }

    // Rating Model
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        public int RatedUserId { get; set; }

        [Required]
        public int RatingGivenByUserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("RideId")]
        public Ride Ride { get; set; } = null!;

        [ForeignKey("RatedUserId")]
        public User RatedUser { get; set; } = null!;

        [ForeignKey("RatingGivenByUserId")]
        public User RatingGivenByUser { get; set; } = null!;
    }

    // Pricing Model
    public class Pricing
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BaseFare { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PerKmRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PerMinuteRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MinimumFare { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PeakHourMultiplier { get; set; } = 1.0m;

        public TimeSpan MorningPeakStart { get; set; } = new TimeSpan(7, 0, 0);
        public TimeSpan MorningPeakEnd { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan EveningPeakStart { get; set; } = new TimeSpan(15, 0, 0);
        public TimeSpan EveningPeakEnd { get; set; } = new TimeSpan(19, 0, 0);

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // Notification Model
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public int? RideId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        [StringLength(1000)]
        public string? Data { get; set; } // JSON data for additional info

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("RideId")]
        public Ride? Ride { get; set; }
    }

    // RideTracking Model (for real-time location tracking during ride)
    public class RideTracking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("RideId")]
        public Ride Ride { get; set; } = null!;
    }

    // Driver Location Model (for real-time driver tracking when online)
    public class DriverLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DriverId")]
        public User Driver { get; set; } = null!;
    }

    // RideRequest Model (for managing ride requests to multiple drivers)
    public class RideRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        public int DriverId { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public bool? Accepted { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Navigation Properties
        [ForeignKey("RideId")]
        public Ride Ride { get; set; } = null!;

        [ForeignKey("DriverId")]
        public User Driver { get; set; } = null!;
    }
}