using WasselniAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WasselniAPI.Services.Interfaces
{
    // User Service Interface
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByPhoneAsync(string phone);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetOnlineDriversAsync();
        Task<IEnumerable<User>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ValidatePasswordAsync(string email, string password);
        Task<string> HashPasswordAsync(string password);
        Task<bool> UpdateLocationAsync(int userId, double lat, double lng);
        Task<bool> UpdateDriverStatusAsync(int driverId, DriverStatus status);
        Task<bool> SetLoginStatusAsync(int userId, bool isLoggedIn);
        Task<double?> GetDriverRatingAsync(int driverId);
        Task<bool> UpdateDriverRatingAsync(int driverId);
    }

    // Car Service Interface
    public interface ICarService
    {
        Task<Car?> GetCarByIdAsync(int id);
        Task<Car?> GetCarByDriverIdAsync(int driverId);
        Task<IEnumerable<Car>> GetAllCarsAsync();
        Task<Car> CreateCarAsync(Car car);
        Task<Car> UpdateCarAsync(Car car);
        Task<bool> DeleteCarAsync(int id);
        Task<bool> IsPlateNumberExistsAsync(string plateNumber);
    }

    // Ride Service Interface
    public interface IRideService
    {
        Task<Ride?> GetRideByIdAsync(int id);
        Task<IEnumerable<Ride>> GetUserRidesAsync(int userId, UserType userType);
        Task<IEnumerable<Ride>> GetActiveRidesAsync();
        Task<Ride?> GetActiveRideByCustomerIdAsync(int customerId);
        Task<Ride?> GetActiveRideByDriverIdAsync(int driverId);
        Task<Ride> CreateRideAsync(Ride ride);
        Task<Ride> UpdateRideAsync(Ride ride);
        Task<bool> CancelRideAsync(int rideId, string reason);
        Task<bool> AcceptRideAsync(int rideId, int driverId);
        Task<bool> StartRideAsync(int rideId);
        Task<bool> CompleteRideAsync(int rideId, double distanceKm, int durationMinutes);
        Task<bool> DriverArrivedAsync(int rideId);
        Task<decimal> CalculateFareAsync(double distanceKm, int durationMinutes, DateTime requestTime);
        Task<IEnumerable<Ride>> GetRideHistoryAsync(int userId, int pageSize = 20, int pageNumber = 1);
    }

    // Payment Service Interface
    public interface IPaymentService
    {
        Task<Payment?> GetPaymentByIdAsync(int id);
        Task<Payment?> GetPaymentByRideIdAsync(int rideId);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<bool> ProcessPaymentAsync(int paymentId);
        Task<bool> RefundPaymentAsync(int paymentId, string reason);
        Task<decimal> GetTotalEarningsAsync(int driverId, DateTime? from = null, DateTime? to = null);
    }

    // Rating Service Interface
    public interface IRatingService
    {
        Task<Rating?> GetRatingByIdAsync(int id);
        Task<IEnumerable<Rating>> GetUserRatingsAsync(int userId);
        Task<Rating?> GetRideRatingAsync(int rideId, int ratedUserId);
        Task<Rating> CreateRatingAsync(Rating rating);
        Task<Rating> UpdateRatingAsync(Rating rating);
        Task<double> GetAverageRatingAsync(int userId);
        Task<bool> HasUserRatedRideAsync(int rideId, int userId);
    }

    // Pricing Service Interface
    public interface IPricingService
    {
        Task<Pricing?> GetActivePricingAsync();
        Task<IEnumerable<Pricing>> GetAllPricingAsync();
        Task<Pricing> CreatePricingAsync(Pricing pricing);
        Task<Pricing> UpdatePricingAsync(Pricing pricing);
        Task<decimal> CalculateRideFareAsync(double distanceKm, int durationMinutes, DateTime requestTime);
        Task<bool> IsPeakHourAsync(DateTime dateTime);
    }

    // Notification Service Interface
    public interface INotificationService
    {
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> SendRideNotificationAsync(int userId, NotificationType type, int rideId, string title, string message);
    }

    // Driver Location Service Interface
    public interface IDriverLocationService
    {
        Task<DriverLocation?> GetLatestLocationAsync(int driverId);
        Task<IEnumerable<DriverLocation>> GetNearbyDriversAsync(double lat, double lng, double radiusKm);
        Task<DriverLocation> UpdateLocationAsync(int driverId, double lat, double lng, double? speed = null, double? heading = null);
        Task<IEnumerable<DriverLocation>> GetLocationHistoryAsync(int driverId, DateTime from, DateTime to);
        Task<bool> DeleteOldLocationsAsync(DateTime before);
    }

    // Ride Tracking Service Interface
    public interface IRideTrackingService
    {
        Task<IEnumerable<RideTracking>> GetRideTrackingAsync(int rideId);
        Task<RideTracking> AddTrackingPointAsync(int rideId, double lat, double lng, double? speed = null, double? heading = null);
        Task<double> CalculateDistanceAsync(int rideId);
        Task<RideTracking?> GetLatestTrackingPointAsync(int rideId);
    }

    // Ride Request Service Interface
    public interface IRideRequestService
    {
        Task<IEnumerable<RideRequest>> GetPendingRequestsAsync(int driverId);
        Task<RideRequest> SendRideRequestAsync(int rideId, int driverId);
        Task<bool> RespondToRequestAsync(int requestId, bool accepted);
        Task<bool> ExpireOldRequestsAsync();
        Task<IEnumerable<RideRequest>> GetRideRequestsAsync(int rideId);
    }

    // WebSocket Service Interface
    public interface IWebSocketService
    {
        Task SendToUserAsync(int userId, object message);
        Task SendToDriversAsync(IEnumerable<int> driverIds, object message);
        Task BroadcastToAllAsync(object message);
        Task NotifyRideStatusChange(int rideId, RideStatus status);
        Task NotifyLocationUpdate(int driverId, double lat, double lng);
        Task NotifyNewRideRequest(int driverId, int rideId);
    }
}

namespace WasselniAPI.Services.Implementations
{
    using WasselniAPI.Services.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using WasselniAPI.Data;

    // User Service Implementation
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Car)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetOnlineDriversAsync()
        {
            return await _context.Users
                .Where(u => u.UserType == UserType.Driver &&
                           u.DriverStatus == DriverStatus.Online &&
                           u.IsLoggedIn)
                .Include(u => u.Car)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5)
        {
            var drivers = await GetOnlineDriversAsync();

            return drivers.Where(d => d.CurrentLat.HasValue && d.CurrentLng.HasValue &&
                CalculateDistance(lat, lng, d.CurrentLat.Value, d.CurrentLng.Value) <= radiusKm);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.PasswordHash = await HashPasswordAsync(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidatePasswordAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            return VerifyPassword(password, user.PasswordHash);
        }

        public async Task<string> HashPasswordAsync(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "WasselSalt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPasswordAsync(password).Result;
            return passwordHash == hash;
        }

        public async Task<bool> UpdateLocationAsync(int userId, double lat, double lng)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.CurrentLat = lat;
            user.CurrentLng = lng;
            await UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> UpdateDriverStatusAsync(int driverId, DriverStatus status)
        {
            var driver = await GetUserByIdAsync(driverId);
            if (driver?.UserType != UserType.Driver) return false;

            driver.DriverStatus = status;
            await UpdateUserAsync(driver);
            return true;
        }

        public async Task<bool> SetLoginStatusAsync(int userId, bool isLoggedIn)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsLoggedIn = isLoggedIn;
            if (isLoggedIn) user.LastLoginAt = DateTime.UtcNow;
            await UpdateUserAsync(user);
            return true;
        }

        public async Task<double?> GetDriverRatingAsync(int driverId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.RatedUserId == driverId)
                .Select(r => r.Score)
                .ToListAsync();

            return ratings.Any() ? ratings.Average() : null;
        }

        public async Task<bool> UpdateDriverRatingAsync(int driverId)
        {
            var rating = await GetDriverRatingAsync(driverId);
            var driver = await GetUserByIdAsync(driverId);

            if (driver == null) return false;

            driver.Rating = rating;
            await UpdateUserAsync(driver);
            return true;
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            var R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }

    // Car Service Implementation
    public class CarService : ICarService
    {
        private readonly ApplicationDbContext _context;

        public CarService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Car?> GetCarByIdAsync(int id)
        {
            return await _context.Cars
                .Include(c => c.Driver)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Car?> GetCarByDriverIdAsync(int driverId)
        {
            return await _context.Cars
                .FirstOrDefaultAsync(c => c.DriverId == driverId && c.IsActive);
        }

        public async Task<IEnumerable<Car>> GetAllCarsAsync()
        {
            return await _context.Cars
                .Include(c => c.Driver)
                .Where(c => c.IsActive)
                .ToListAsync();
        }

        public async Task<Car> CreateCarAsync(Car car)
        {
            car.CreatedAt = DateTime.UtcNow;
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return car;
        }

        public async Task<Car> UpdateCarAsync(Car car)
        {
            _context.Cars.Update(car);
            await _context.SaveChangesAsync();
            return car;
        }

        public async Task<bool> DeleteCarAsync(int id)
        {
            var car = await GetCarByIdAsync(id);
            if (car == null) return false;

            car.IsActive = false;
            await UpdateCarAsync(car);
            return true;
        }

        public async Task<bool> IsPlateNumberExistsAsync(string plateNumber)
        {
            return await _context.Cars
                .AnyAsync(c => c.PlateNumber == plateNumber && c.IsActive);
        }
    }

    // Ride Service Implementation
    public class RideService : IRideService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricingService _pricingService;

        public RideService(ApplicationDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<Ride?> GetRideByIdAsync(int id)
        {
            return await _context.Rides
                .Include(r => r.Customer)
                .Include(r => r.Driver)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Ride>> GetUserRidesAsync(int userId, UserType userType)
        {
            var query = _context.Rides.Include(r => r.Customer).Include(r => r.Driver);

            return userType == UserType.Customer
                ? await query.Where(r => r.CustomerId == userId).ToListAsync()
                : await query.Where(r => r.DriverId == userId).ToListAsync();
        }

        public async Task<IEnumerable<Ride>> GetActiveRidesAsync()
        {
            return await _context.Rides
                .Where(r => r.Status != RideStatus.Completed && r.Status != RideStatus.Cancelled)
                .Include(r => r.Customer)
                .Include(r => r.Driver)
                .ToListAsync();
        }

        public async Task<Ride?> GetActiveRideByCustomerIdAsync(int customerId)
        {
            return await _context.Rides
                .Where(r => r.CustomerId == customerId &&
                           r.Status != RideStatus.Completed &&
                           r.Status != RideStatus.Cancelled)
                .Include(r => r.Driver)
                .FirstOrDefaultAsync();
        }

        public async Task<Ride?> GetActiveRideByDriverIdAsync(int driverId)
        {
            return await _context.Rides
                .Where(r => r.DriverId == driverId &&
                           r.Status != RideStatus.Completed &&
                           r.Status != RideStatus.Cancelled)
                .Include(r => r.Customer)
                .FirstOrDefaultAsync();
        }

        public async Task<Ride> CreateRideAsync(Ride ride)
        {
            ride.CreatedAt = DateTime.UtcNow;
            ride.Status = RideStatus.Requested;

            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();
            return ride;
        }

        public async Task<Ride> UpdateRideAsync(Ride ride)
        {
            _context.Rides.Update(ride);
            await _context.SaveChangesAsync();
            return ride;
        }

        public async Task<bool> CancelRideAsync(int rideId, string reason)
        {
            var ride = await GetRideByIdAsync(rideId);
            if (ride == null) return false;

            ride.Status = RideStatus.Cancelled;
            ride.CancelledAt = DateTime.UtcNow;
            ride.CancellationReason = reason;

            await UpdateRideAsync(ride);
            return true;
        }

        public async Task<bool> AcceptRideAsync(int rideId, int driverId)
        {
            var ride = await GetRideByIdAsync(rideId);
            if (ride?.Status != RideStatus.Requested) return false;

            ride.DriverId = driverId;
            ride.Status = RideStatus.Accepted;
            ride.AcceptedAt = DateTime.UtcNow;

            await UpdateRideAsync(ride);
            return true;
        }

        public async Task<bool> StartRideAsync(int rideId)
        {
            var ride = await GetRideByIdAsync(rideId);
            if (ride?.Status != RideStatus.Arrived) return false;

            ride.Status = RideStatus.InProgress;
            ride.StartedAt = DateTime.UtcNow;

            await UpdateRideAsync(ride);
            return true;
        }

        public async Task<bool> CompleteRideAsync(int rideId, double distanceKm, int durationMinutes)
        {
            var ride = await GetRideByIdAsync(rideId);
            if (ride?.Status != RideStatus.InProgress) return false;

            ride.Status = RideStatus.Completed;
            ride.CompletedAt = DateTime.UtcNow;
            ride.DistanceKm = distanceKm;
            ride.DurationMinutes = durationMinutes;
            ride.ActualFare = await CalculateFareAsync(distanceKm, durationMinutes, ride.CreatedAt);

            await UpdateRideAsync(ride);
            return true;
        }

        public async Task<bool> DriverArrivedAsync(int rideId)
        {
            var ride = await GetRideByIdAsync(rideId);
            if (ride?.Status != RideStatus.Accepted) return false;

            ride.Status = RideStatus.Arrived;
            ride.ArrivedAt = DateTime.UtcNow;

            await UpdateRideAsync(ride);
            return true;
        }

        public async Task<decimal> CalculateFareAsync(double distanceKm, int durationMinutes, DateTime requestTime)
        {
            return await _pricingService.CalculateRideFareAsync(distanceKm, durationMinutes, requestTime);
        }

        public async Task<IEnumerable<Ride>> GetRideHistoryAsync(int userId, int pageSize = 20, int pageNumber = 1)
        {
            return await _context.Rides
                .Where(r => r.CustomerId == userId || r.DriverId == userId)
                .Where(r => r.Status == RideStatus.Completed)
                .OrderByDescending(r => r.CompletedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Customer)
                .Include(r => r.Driver)
                .ToListAsync();
        }
    }

    // Payment Service Implementation
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Ride)
                .ThenInclude(r => r.Customer)
                .Include(p => p.Ride)
                .ThenInclude(r => r.Driver)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetPaymentByRideIdAsync(int rideId)
        {
            return await _context.Payments
                .Include(p => p.Ride)
                .FirstOrDefaultAsync(p => p.RideId == rideId);
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId)
        {
            return await _context.Payments
                .Include(p => p.Ride)
                .Where(p => p.Ride.CustomerId == userId || p.Ride.DriverId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Pending;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> ProcessPaymentAsync(int paymentId)
        {
            var payment = await GetPaymentByIdAsync(paymentId);
            if (payment == null || payment.Status != PaymentStatus.Pending)
                return false;

            try
            {
                // Simulate payment processing logic
                // In real implementation, integrate with payment gateway
                await Task.Delay(100); // Simulate processing time

                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.TransactionId = Guid.NewGuid().ToString("N")[..16].ToUpper();

                await UpdatePaymentAsync(payment);

                _logger.LogInformation("Payment {PaymentId} processed successfully", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment {PaymentId}", paymentId);
                payment.Status = PaymentStatus.Failed;
                payment.Notes = ex.Message;
                await UpdatePaymentAsync(payment);
                return false;
            }
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, string reason)
        {
            var payment = await GetPaymentByIdAsync(paymentId);
            if (payment == null || payment.Status != PaymentStatus.Completed)
                return false;

            try
            {
                // Simulate refund processing
                payment.Status = PaymentStatus.Refunded;
                payment.Notes = $"Refunded: {reason}";
                payment.ProcessedAt = DateTime.UtcNow;

                await UpdatePaymentAsync(payment);

                _logger.LogInformation("Payment {PaymentId} refunded successfully", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refund payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<decimal> GetTotalEarningsAsync(int driverId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Payments
                .Include(p => p.Ride)
                .Where(p => p.Ride.DriverId == driverId && p.Status == PaymentStatus.Completed);

            if (from.HasValue)
                query = query.Where(p => p.ProcessedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.ProcessedAt <= to.Value);

            return await query.SumAsync(p => p.Amount);
        }
    }

    // Rating Service Implementation
    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public RatingService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<Rating?> GetRatingByIdAsync(int id)
        {
            return await _context.Ratings
                .Include(r => r.Ride)
                .Include(r => r.RatedUser)
                .Include(r => r.RatingGivenByUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Rating>> GetUserRatingsAsync(int userId)
        {
            return await _context.Ratings
                .Include(r => r.Ride)
                .Include(r => r.RatingGivenByUser)
                .Where(r => r.RatedUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Rating?> GetRideRatingAsync(int rideId, int ratedUserId)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.RideId == rideId && r.RatedUserId == ratedUserId);
        }

        public async Task<Rating> CreateRatingAsync(Rating rating)
        {
            // Check if rating already exists
            var existingRating = await GetRideRatingAsync(rating.RideId, rating.RatedUserId);
            if (existingRating != null)
                throw new InvalidOperationException("Rating already exists for this ride and user");

            rating.CreatedAt = DateTime.UtcNow;

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            // Update user's overall rating
            await _userService.UpdateDriverRatingAsync(rating.RatedUserId);

            return rating;
        }

        public async Task<Rating> UpdateRatingAsync(Rating rating)
        {
            _context.Ratings.Update(rating);
            await _context.SaveChangesAsync();

            // Update user's overall rating
            await _userService.UpdateDriverRatingAsync(rating.RatedUserId);

            return rating;
        }

        public async Task<double> GetAverageRatingAsync(int userId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.RatedUserId == userId)
                .Select(r => r.Score)
                .ToListAsync();

            return ratings.Any() ? ratings.Average() : 0.0;
        }

        public async Task<bool> HasUserRatedRideAsync(int rideId, int userId)
        {
            return await _context.Ratings
                .AnyAsync(r => r.RideId == rideId && r.RatingGivenByUserId == userId);
        }
    }

    // Pricing Service Implementation
    public class PricingService : IPricingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PricingService> _logger;

        public PricingService(ApplicationDbContext context, ILogger<PricingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Pricing?> GetActivePricingAsync()
        {
            return await _context.Pricings
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Pricing>> GetAllPricingAsync()
        {
            return await _context.Pricings
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Pricing> CreatePricingAsync(Pricing pricing)
        {
            pricing.CreatedAt = DateTime.UtcNow;
            pricing.IsActive = true;

            // Deactivate other pricing models
            var existingPricings = await _context.Pricings
                .Where(p => p.IsActive)
                .ToListAsync();

            foreach (var existing in existingPricings)
            {
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            _context.Pricings.Add(pricing);
            await _context.SaveChangesAsync();
            return pricing;
        }

        public async Task<Pricing> UpdatePricingAsync(Pricing pricing)
        {
            pricing.UpdatedAt = DateTime.UtcNow;
            _context.Pricings.Update(pricing);
            await _context.SaveChangesAsync();
            return pricing;
        }

        public async Task<decimal> CalculateRideFareAsync(double distanceKm, int durationMinutes, DateTime requestTime)
        {
            var pricing = await GetActivePricingAsync();
            if (pricing == null)
            {
                _logger.LogWarning("No active pricing model found, using default values");
                // Default Jordan pricing
                pricing = new Pricing
                {
                    BaseFare = 0.50m,
                    PerKmRate = 0.28m,
                    PerMinuteRate = 0.05m,
                    MinimumFare = 1.10m,
                    PeakHourMultiplier = 1.20m
                };
            }

            // Calculate base fare
            var distanceFare = (decimal)distanceKm * pricing.PerKmRate;
            var timeFare = durationMinutes * pricing.PerMinuteRate;
            var totalFare = pricing.BaseFare + distanceFare + timeFare;

            // Apply peak hour multiplier
            if (await IsPeakHourAsync(requestTime))
            {
                totalFare *= pricing.PeakHourMultiplier;
                _logger.LogInformation("Peak hour multiplier applied: {Multiplier}", pricing.PeakHourMultiplier);
            }

            // Ensure minimum fare
            if (totalFare < pricing.MinimumFare)
            {
                totalFare = pricing.MinimumFare;
            }

            // Round to 2 decimal places
            return Math.Round(totalFare, 2);
        }

        public async Task<bool> IsPeakHourAsync(DateTime dateTime)
        {
            var pricing = await GetActivePricingAsync();
            if (pricing == null) return false;

            var timeOfDay = dateTime.TimeOfDay;

            // Check morning peak hours
            if (timeOfDay >= pricing.MorningPeakStart && timeOfDay <= pricing.MorningPeakEnd)
                return true;

            // Check evening peak hours
            if (timeOfDay >= pricing.EveningPeakStart && timeOfDay <= pricing.EveningPeakEnd)
                return true;

            return false;
        }
    }

    // Notification Service Implementation
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Ride)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to last 50 notifications
                .ToListAsync();
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification created for user {UserId}: {Title}",
                notification.UserId, notification.Title);

            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await GetNotificationByIdAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> SendRideNotificationAsync(int userId, NotificationType type, int rideId, string title, string message)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RideId = rideId
                };

                await CreateNotificationAsync(notification);

                // Here you would integrate with push notification service
                // For example: Firebase, Apple Push Notification service, etc.
                await SendPushNotificationAsync(userId, title, message);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
                return false;
            }
        }

        private async Task SendPushNotificationAsync(int userId, string title, string message)
        {
            // Simulate push notification sending
            // In real implementation, integrate with FCM, APNS, etc.
            _logger.LogInformation("Push notification sent to user {UserId}: {Title} - {Message}",
                userId, title, message);

            await Task.CompletedTask;
        }
    }

    // Driver Location Service Implementation
    public class DriverLocationService : IDriverLocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DriverLocationService> _logger;

        public DriverLocationService(ApplicationDbContext context, ILogger<DriverLocationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DriverLocation?> GetLatestLocationAsync(int driverId)
        {
            return await _context.DriverLocations
                .Where(dl => dl.DriverId == driverId)
                .OrderByDescending(dl => dl.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DriverLocation>> GetNearbyDriversAsync(double lat, double lng, double radiusKm)
        {
            // Get recent locations (within last 5 minutes)
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            var recentLocations = await _context.DriverLocations
                .Where(dl => dl.Timestamp >= cutoffTime)
                .Include(dl => dl.Driver)
                .Where(dl => dl.Driver.DriverStatus == DriverStatus.Online)
                .ToListAsync();

            // Filter by distance (simple implementation)
            return recentLocations.Where(dl =>
                CalculateDistance(lat, lng, dl.Latitude, dl.Longitude) <= radiusKm);
        }

        public async Task<DriverLocation> UpdateLocationAsync(int driverId, double lat, double lng, double? speed = null, double? heading = null)
        {
            var location = new DriverLocation
            {
                DriverId = driverId,
                Latitude = lat,
                Longitude = lng,
                Speed = speed,
                Heading = heading,
                Timestamp = DateTime.UtcNow
            };

            _context.DriverLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }

        public async Task<IEnumerable<DriverLocation>> GetLocationHistoryAsync(int driverId, DateTime from, DateTime to)
        {
            return await _context.DriverLocations
                .Where(dl => dl.DriverId == driverId &&
                            dl.Timestamp >= from &&
                            dl.Timestamp <= to)
                .OrderBy(dl => dl.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> DeleteOldLocationsAsync(DateTime before)
        {
            try
            {
                var oldLocations = await _context.DriverLocations
                    .Where(dl => dl.Timestamp < before)
                    .ToListAsync();

                _context.DriverLocations.RemoveRange(oldLocations);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} old driver locations before {Date}",
                    oldLocations.Count, before);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old driver locations");
                return false;
            }
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            var R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }

    // Ride Tracking Service Implementation
    public class RideTrackingService : IRideTrackingService
    {
        private readonly ApplicationDbContext _context;

        public RideTrackingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RideTracking>> GetRideTrackingAsync(int rideId)
        {
            return await _context.RideTrackings
                .Where(rt => rt.RideId == rideId)
                .OrderBy(rt => rt.Timestamp)
                .ToListAsync();
        }

        public async Task<RideTracking> AddTrackingPointAsync(int rideId, double lat, double lng, double? speed = null, double? heading = null)
        {
            var trackingPoint = new RideTracking
            {
                RideId = rideId,
                Latitude = lat,
                Longitude = lng,
                Speed = speed,
                Heading = heading,
                Timestamp = DateTime.UtcNow
            };

            _context.RideTrackings.Add(trackingPoint);
            await _context.SaveChangesAsync();

            return trackingPoint;
        }

        public async Task<double> CalculateDistanceAsync(int rideId)
        {
            var trackingPoints = await GetRideTrackingAsync(rideId);
            var points = trackingPoints.ToList();

            if (points.Count < 2) return 0;

            double totalDistance = 0;
            for (int i = 1; i < points.Count; i++)
            {
                totalDistance += CalculateDistance(
                    points[i - 1].Latitude, points[i - 1].Longitude,
                    points[i].Latitude, points[i].Longitude);
            }

            return totalDistance;
        }

        public async Task<RideTracking?> GetLatestTrackingPointAsync(int rideId)
        {
            return await _context.RideTrackings
                .Where(rt => rt.RideId == rideId)
                .OrderByDescending(rt => rt.Timestamp)
                .FirstOrDefaultAsync();
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            var R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }

    // Ride Request Service Implementation
    public class RideRequestService : IRideRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RideRequestService> _logger;

        public RideRequestService(ApplicationDbContext context, ILogger<RideRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<RideRequest>> GetPendingRequestsAsync(int driverId)
        {
            return await _context.RideRequests
                .Where(rr => rr.DriverId == driverId &&
                            !rr.Accepted.HasValue &&
                            rr.ExpiresAt > DateTime.UtcNow)
                .Include(rr => rr.Ride)
                .ThenInclude(r => r.Customer)
                .OrderBy(rr => rr.SentAt)
                .ToListAsync();
        }

        public async Task<RideRequest> SendRideRequestAsync(int rideId, int driverId)
        {
            var request = new RideRequest
            {
                RideId = rideId,
                DriverId = driverId,
                SentAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2) // 2 minutes to respond
            };

            _context.RideRequests.Add(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ride request sent to driver {DriverId} for ride {RideId}", driverId, rideId);

            return request;
        }

        public async Task<bool> RespondToRequestAsync(int requestId, bool accepted)
        {
            var request = await _context.RideRequests
                .Include(rr => rr.Ride)
                .FirstOrDefaultAsync(rr => rr.Id == requestId);

            if (request == null || request.RespondedAt.HasValue || request.ExpiresAt < DateTime.UtcNow)
                return false;

            request.Accepted = accepted;
            request.RespondedAt = DateTime.UtcNow;

            _context.RideRequests.Update(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Driver {DriverId} {Response} ride request {RequestId}",
                request.DriverId, accepted ? "accepted" : "declined", requestId);

            return true;
        }

        public async Task<bool> ExpireOldRequestsAsync()
        {
            try
            {
                var expiredRequests = await _context.RideRequests
                    .Where(rr => !rr.RespondedAt.HasValue && rr.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var request in expiredRequests)
                {
                    request.Accepted = false;
                    request.RespondedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                if (expiredRequests.Any())
                {
                    _logger.LogInformation("Expired {Count} old ride requests", expiredRequests.Count);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire old ride requests");
                return false;
            }
        }

        public async Task<IEnumerable<RideRequest>> GetRideRequestsAsync(int rideId)
        {
            return await _context.RideRequests
                .Where(rr => rr.RideId == rideId)
                .Include(rr => rr.Driver)
                .OrderBy(rr => rr.SentAt)
                .ToListAsync();
        }
    }
}