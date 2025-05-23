using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WasselniAPI.DTOs;
using WasselniAPI.Models;
using WasselniAPI.Services.Interfaces;


namespace WasselniAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("Email already exists");

            var existingPhone = await _userService.GetUserByPhoneAsync(dto.PhoneNumber);
            if (existingPhone != null)
                return BadRequest("Phone number already exists");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                UserType = dto.UserType,
                PasswordHash = dto.Password
            };

            var createdUser = await _userService.CreateUserAsync(user);
            return Ok(new { Message = "User registered successfully", UserId = createdUser.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var isValid = await _userService.ValidatePasswordAsync(dto.Email, dto.Password);
            if (!isValid)
                return Unauthorized("Invalid credentials");

            var user = await _userService.GetUserByEmailAsync(dto.Email);
            await _userService.SetLoginStatusAsync(user.Id, true);

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token, User = user });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            await _userService.SetLoginStatusAsync(userId, false);
            return Ok(new { Message = "Logged out successfully" });
        }

        private string GenerateJwtToken(User user)
        {
            return $"jwt_token_for_user_{user.Id}";
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.Address = dto.Address ?? user.Address;

            var updatedUser = await _userService.UpdateUserAsync(user);
            return Ok(updatedUser);
        }

        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationDto dto)
        {
            var userId = GetCurrentUserId();
            var success = await _userService.UpdateLocationAsync(userId, dto.Latitude, dto.Longitude);
            return success ? Ok() : BadRequest();
        }

        [HttpGet("drivers/nearby")]
        public async Task<IActionResult> GetNearbyDrivers([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 5)
        {
            var drivers = await _userService.GetNearbyDriversAsync(lat, lng, radius);
            return Ok(drivers);
        }

        [HttpGet("drivers/online")]
        public async Task<IActionResult> GetOnlineDrivers()
        {
            var drivers = await _userService.GetOnlineDriversAsync();
            return Ok(drivers);
        }

        [HttpPost("driver/status")]
        public async Task<IActionResult> UpdateDriverStatus([FromBody] DriverStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var success = await _userService.UpdateDriverStatusAsync(userId, dto.Status);
            return success ? Ok() : BadRequest();
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CarsController : ControllerBase
    {
        private readonly ICarService _carService;

        public CarsController(ICarService carService)
        {
            _carService = carService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCars()
        {
            var cars = await _carService.GetAllCarsAsync();
            return Ok(cars);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCar(int id)
        {
            var car = await _carService.GetCarByIdAsync(id);
            return car != null ? Ok(car) : NotFound();
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetCarByDriver(int driverId)
        {
            var car = await _carService.GetCarByDriverIdAsync(driverId);
            return car != null ? Ok(car) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCar([FromBody] CreateCarDto dto)
        {
            var exists = await _carService.IsPlateNumberExistsAsync(dto.PlateNumber);
            if (exists)
                return BadRequest("Plate number already exists");

            var car = new Car
            {
                DriverId = dto.DriverId,
                PlateNumber = dto.PlateNumber,
                Make = dto.Make,
                Model = dto.Model,
                Year = dto.Year,
                Color = dto.Color,
                ImageUrl = dto.ImageUrl
            };

            var createdCar = await _carService.CreateCarAsync(car);
            return CreatedAtAction(nameof(GetCar), new { id = createdCar.Id }, createdCar);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCar(int id, [FromBody] UpdateCarDto dto)
        {
            var car = await _carService.GetCarByIdAsync(id);
            if (car == null) return NotFound();

            car.PlateNumber = dto.PlateNumber ?? car.PlateNumber;
            car.Make = dto.Make ?? car.Make;
            car.Model = dto.Model ?? car.Model;
            car.Year = dto.Year ?? car.Year;
            car.Color = dto.Color ?? car.Color;
            car.ImageUrl = dto.ImageUrl ?? car.ImageUrl;

            var updatedCar = await _carService.UpdateCarAsync(car);
            return Ok(updatedCar);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var success = await _carService.DeleteCarAsync(id);
            return success ? NoContent() : NotFound();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RidesController : ControllerBase
    {
        private readonly IRideService _rideService;
        private readonly IRideRequestService _rideRequestService;
        private readonly IUserService _userService;
        private readonly IWebSocketService _webSocketService;

        public RidesController(IRideService rideService, IRideRequestService rideRequestService, IUserService userService, IWebSocketService webSocketService)
        {
            _rideService = rideService;
            _rideRequestService = rideRequestService;
            _userService = userService;
            _webSocketService = webSocketService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRides([FromQuery] UserType? userType)
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            var rides = await _rideService.GetUserRidesAsync(userId, userType ?? user.UserType);
            return Ok(rides);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRide(int id)
        {
            var ride = await _rideService.GetRideByIdAsync(id);
            return ride != null ? Ok(ride) : NotFound();
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveRide()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            var ride = user.UserType == UserType.Customer
                ? await _rideService.GetActiveRideByCustomerIdAsync(userId)
                : await _rideService.GetActiveRideByDriverIdAsync(userId);

            return ride != null ? Ok(ride) : NotFound();
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetRideHistory([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
        {
            var userId = GetCurrentUserId();
            var rides = await _rideService.GetRideHistoryAsync(userId, pageSize, pageNumber);
            return Ok(rides);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRide([FromBody] CreateRideDto dto)
        {
            var userId = GetCurrentUserId();
            var activeRide = await _rideService.GetActiveRideByCustomerIdAsync(userId);
            if (activeRide != null)
                return BadRequest("You already have an active ride");

            var ride = new Ride
            {
                CustomerId = userId,
                PickupLat = dto.PickupLat,
                PickupLng = dto.PickupLng,
                PickupAddress = dto.PickupAddress,
                DropoffLat = dto.DropoffLat,
                DropoffLng = dto.DropoffLng,
                DropoffAddress = dto.DropoffAddress,
                EstimatedFare = dto.EstimatedFare,
                Notes = dto.Notes
            };

            var createdRide = await _rideService.CreateRideAsync(ride);

            var nearbyDrivers = await _userService.GetNearbyDriversAsync(dto.PickupLat, dto.PickupLng);
            foreach (var driver in nearbyDrivers)
            {
                await _rideRequestService.SendRideRequestAsync(createdRide.Id, driver.Id);
                await _webSocketService.NotifyNewRideRequest(driver.Id, createdRide.Id);
            }

            return CreatedAtAction(nameof(GetRide), new { id = createdRide.Id }, createdRide);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptRide(int id)
        {
            var userId = GetCurrentUserId();
            var activeRide = await _rideService.GetActiveRideByDriverIdAsync(userId);
            if (activeRide != null)
                return BadRequest("You already have an active ride");

            var success = await _rideService.AcceptRideAsync(id, userId);
            if (!success)
                return BadRequest("Cannot accept this ride");

            await _webSocketService.NotifyRideStatusChange(id, RideStatus.Accepted);
            return Ok();
        }

        [HttpPost("{id}/arrived")]
        public async Task<IActionResult> DriverArrived(int id)
        {
            var success = await _rideService.DriverArrivedAsync(id);
            if (!success)
                return BadRequest("Cannot mark as arrived");

            await _webSocketService.NotifyRideStatusChange(id, RideStatus.Arrived);
            return Ok();
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRide(int id)
        {
            var success = await _rideService.StartRideAsync(id);
            if (!success)
                return BadRequest("Cannot start this ride");

            await _webSocketService.NotifyRideStatusChange(id, RideStatus.InProgress);
            return Ok();
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteRide(int id, [FromBody] CompleteRideDto dto)
        {
            var success = await _rideService.CompleteRideAsync(id, dto.DistanceKm, dto.DurationMinutes);
            if (!success)
                return BadRequest("Cannot complete this ride");

            await _webSocketService.NotifyRideStatusChange(id, RideStatus.Completed);
            return Ok();
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelRide(int id, [FromBody] CancelRideDto dto)
        {
            var success = await _rideService.CancelRideAsync(id, dto.Reason);
            if (!success)
                return BadRequest("Cannot cancel this ride");

            await _webSocketService.NotifyRideStatusChange(id, RideStatus.Cancelled);
            return Ok();
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            return payment != null ? Ok(payment) : NotFound();
        }

        [HttpGet("ride/{rideId}")]
        public async Task<IActionResult> GetPaymentByRide(int rideId)
        {
            var payment = await _paymentService.GetPaymentByRideIdAsync(rideId);
            return payment != null ? Ok(payment) : NotFound();
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserPayments()
        {
            var userId = GetCurrentUserId();
            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            return Ok(payments);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var payment = new Payment
            {
                RideId = dto.RideId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                Notes = dto.Notes
            };

            var createdPayment = await _paymentService.CreatePaymentAsync(payment);
            return CreatedAtAction(nameof(GetPayment), new { id = createdPayment.Id }, createdPayment);
        }

        [HttpPost("{id}/process")]
        public async Task<IActionResult> ProcessPayment(int id)
        {
            var success = await _paymentService.ProcessPaymentAsync(id);
            return success ? Ok() : BadRequest("Payment processing failed");
        }

        [HttpPost("{id}/refund")]
        public async Task<IActionResult> RefundPayment(int id, [FromBody] RefundPaymentDto dto)
        {
            var success = await _paymentService.RefundPaymentAsync(id, dto.Reason);
            return success ? Ok() : BadRequest("Refund failed");
        }

        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = GetCurrentUserId();
            var earnings = await _paymentService.GetTotalEarningsAsync(userId, from, to);
            return Ok(new { TotalEarnings = earnings });
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRating(int id)
        {
            var rating = await _ratingService.GetRatingByIdAsync(id);
            return rating != null ? Ok(rating) : NotFound();
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserRatings(int userId)
        {
            var ratings = await _ratingService.GetUserRatingsAsync(userId);
            return Ok(ratings);
        }

        [HttpGet("user/{userId}/average")]
        public async Task<IActionResult> GetAverageRating(int userId)
        {
            var average = await _ratingService.GetAverageRatingAsync(userId);
            return Ok(new { AverageRating = average });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto dto)
        {
            var userId = GetCurrentUserId();
            var hasRated = await _ratingService.HasUserRatedRideAsync(dto.RideId, userId);
            if (hasRated)
                return BadRequest("You have already rated this ride");

            var rating = new Rating
            {
                RideId = dto.RideId,
                RatedUserId = dto.RatedUserId,
                RatingGivenByUserId = userId,
                Score = dto.Score,
                Comment = dto.Comment
            };

            var createdRating = await _ratingService.CreateRatingAsync(rating);
            return CreatedAtAction(nameof(GetRating), new { id = createdRating.Id }, createdRating);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRating(int id, [FromBody] UpdateRatingDto dto)
        {
            var rating = await _ratingService.GetRatingByIdAsync(id);
            if (rating == null) return NotFound();

            rating.Score = dto.Score ?? rating.Score;
            rating.Comment = dto.Comment ?? rating.Comment;

            var updatedRating = await _ratingService.UpdateRatingAsync(rating);
            return Ok(updatedRating);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActivePricing()
        {
            var pricing = await _pricingService.GetActivePricingAsync();
            return pricing != null ? Ok(pricing) : NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPricing()
        {
            var pricings = await _pricingService.GetAllPricingAsync();
            return Ok(pricings);
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateFare([FromBody] CalculateFareDto dto)
        {
            var fare = await _pricingService.CalculateRideFareAsync(dto.DistanceKm, dto.DurationMinutes, dto.RequestTime);
            var isPeakHour = await _pricingService.IsPeakHourAsync(dto.RequestTime);
            return Ok(new { Fare = fare, IsPeakHour = isPeakHour });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePricing([FromBody] CreatePricingDto dto)
        {
            var pricing = new Pricing
            {
                Name = dto.Name,
                BaseFare = dto.BaseFare,
                PerKmRate = dto.PerKmRate,
                PerMinuteRate = dto.PerMinuteRate,
                MinimumFare = dto.MinimumFare,
                PeakHourMultiplier = dto.PeakHourMultiplier,
                MorningPeakStart = dto.MorningPeakStart,
                MorningPeakEnd = dto.MorningPeakEnd,
                EveningPeakStart = dto.EveningPeakStart,
                EveningPeakEnd = dto.EveningPeakEnd
            };

            var createdPricing = await _pricingService.CreatePricingAsync(pricing);
            return Ok(createdPricing);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePricing(int id, [FromBody] UpdatePricingDto dto)
        {
            var pricings = await _pricingService.GetAllPricingAsync();
            var pricing = pricings.FirstOrDefault(p => p.Id == id);
            if (pricing == null) return NotFound();

            pricing.Name = dto.Name ?? pricing.Name;
            pricing.BaseFare = dto.BaseFare ?? pricing.BaseFare;
            pricing.PerKmRate = dto.PerKmRate ?? pricing.PerKmRate;
            pricing.PerMinuteRate = dto.PerMinuteRate ?? pricing.PerMinuteRate;
            pricing.MinimumFare = dto.MinimumFare ?? pricing.MinimumFare;
            pricing.PeakHourMultiplier = dto.PeakHourMultiplier ?? pricing.PeakHourMultiplier;
            pricing.IsActive = dto.IsActive ?? pricing.IsActive;

            var updatedPricing = await _pricingService.UpdatePricingAsync(pricing);
            return Ok(updatedPricing);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id);
            return notification != null ? Ok(notification) : NotFound();
        }

        [HttpGet("unread/count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { UnreadCount = count });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            return success ? Ok() : NotFound();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);
            return success ? Ok() : BadRequest();
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly IDriverLocationService _driverLocationService;
        private readonly IRideTrackingService _rideTrackingService;
        private readonly IWebSocketService _webSocketService;

        public LocationController(IDriverLocationService driverLocationService, IRideTrackingService rideTrackingService, IWebSocketService webSocketService)
        {
            _driverLocationService = driverLocationService;
            _rideTrackingService = rideTrackingService;
            _webSocketService = webSocketService;
        }

        [HttpPost("driver/update")]
        public async Task<IActionResult> UpdateDriverLocation([FromBody] UpdateLocationDto dto)
        {
            var userId = GetCurrentUserId();
            var location = await _driverLocationService.UpdateLocationAsync(userId, dto.Latitude, dto.Longitude, dto.Speed, dto.Heading);
            await _webSocketService.NotifyLocationUpdate(userId, dto.Latitude, dto.Longitude);
            return Ok(location);
        }

        [HttpGet("driver/{driverId}/latest")]
        public async Task<IActionResult> GetLatestDriverLocation(int driverId)
        {
            var location = await _driverLocationService.GetLatestLocationAsync(driverId);
            return location != null ? Ok(location) : NotFound();
        }

        [HttpGet("drivers/nearby")]
        public async Task<IActionResult> GetNearbyDrivers([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 5)
        {
            var drivers = await _driverLocationService.GetNearbyDriversAsync(lat, lng, radius);
            return Ok(drivers);
        }

        [HttpGet("driver/{driverId}/history")]
        public async Task<IActionResult> GetDriverLocationHistory(int driverId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var history = await _driverLocationService.GetLocationHistoryAsync(driverId, from, to);
            return Ok(history);
        }

        [HttpPost("ride/{rideId}/track")]
        public async Task<IActionResult> AddRideTrackingPoint(int rideId, [FromBody] UpdateLocationDto dto)
        {
            var trackingPoint = await _rideTrackingService.AddTrackingPointAsync(rideId, dto.Latitude, dto.Longitude, dto.Speed, dto.Heading);
            return Ok(trackingPoint);
        }

        [HttpGet("ride/{rideId}/tracking")]
        public async Task<IActionResult> GetRideTracking(int rideId)
        {
            var tracking = await _rideTrackingService.GetRideTrackingAsync(rideId);
            return Ok(tracking);
        }

        [HttpGet("ride/{rideId}/distance")]
        public async Task<IActionResult> GetRideDistance(int rideId)
        {
            var distance = await _rideTrackingService.CalculateDistanceAsync(rideId);
            return Ok(new { DistanceKm = distance });
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RideRequestsController : ControllerBase
    {
        private readonly IRideRequestService _rideRequestService;

        public RideRequestsController(IRideRequestService rideRequestService)
        {
            _rideRequestService = rideRequestService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            var requests = await _rideRequestService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpGet("ride/{rideId}")]
        public async Task<IActionResult> GetRideRequests(int rideId)
        {
            var requests = await _rideRequestService.GetRideRequestsAsync(rideId);
            return Ok(requests);
        }

        [HttpPost("{requestId}/respond")]
        public async Task<IActionResult> RespondToRequest(int requestId, [FromBody] RespondToRequestDto dto)
        {
            var success = await _rideRequestService.RespondToRequestAsync(requestId, dto.Accepted);
            return success ? Ok() : BadRequest("Cannot respond to this request");
        }

        [HttpPost("expire-old")]
        public async Task<IActionResult> ExpireOldRequests()
        {
            var success = await _rideRequestService.ExpireOldRequestsAsync();
            return success ? Ok() : BadRequest();
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}

namespace WasselniAPI.DTOs
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public UserType UserType { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DriverStatusDto
    {
        public DriverStatus Status { get; set; }
    }

    public class CreateCarDto
    {
        public int DriverId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class UpdateCarDto
    {
        public string? PlateNumber { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? Color { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateRideDto
    {
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public double DropoffLat { get; set; }
        public double DropoffLng { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public decimal? EstimatedFare { get; set; }
        public string? Notes { get; set; }
    }

    public class CompleteRideDto
    {
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class CancelRideDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreatePaymentDto
    {
        public int RideId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class RefundPaymentDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateRatingDto
    {
        public int RideId { get; set; }
        public int RatedUserId { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateRatingDto
    {
        public int? Score { get; set; }
        public string? Comment { get; set; }
    }

    public class CreatePricingDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public decimal PerKmRate { get; set; }
        public decimal PerMinuteRate { get; set; }
        public decimal MinimumFare { get; set; }
        public decimal PeakHourMultiplier { get; set; } = 1.0m;
        public TimeSpan MorningPeakStart { get; set; }
        public TimeSpan MorningPeakEnd { get; set; }
        public TimeSpan EveningPeakStart { get; set; }
        public TimeSpan EveningPeakEnd { get; set; }
    }

    public class UpdatePricingDto
    {
        public string? Name { get; set; }
        public decimal? BaseFare { get; set; }
        public decimal? PerKmRate { get; set; }
        public decimal? PerMinuteRate { get; set; }
        public decimal? MinimumFare { get; set; }
        public decimal? PeakHourMultiplier { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CalculateFareDto
    {
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }

    public class UpdateLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
    }

    public class RespondToRequestDto
    {
        public bool Accepted { get; set; }
    }
}