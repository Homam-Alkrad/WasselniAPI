using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WasselniAPI.Models;
using WasselniAPI.Services.Interfaces;

namespace WasselniAPI.WebSocket
{
    // WebSocket Message Types
    public static class WebSocketMessageTypes
    {
        public const string LOCATION_UPDATE = "location_update";
        public const string RIDE_REQUEST = "ride_request";
        public const string RIDE_ACCEPTED = "ride_accepted";
        public const string RIDE_CANCELLED = "ride_cancelled";
        public const string DRIVER_ARRIVED = "driver_arrived";
        public const string TRIP_STARTED = "trip_started";
        public const string TRIP_COMPLETED = "trip_completed";
        public const string DRIVER_STATUS_CHANGED = "driver_status_changed";
        public const string NEW_MESSAGE = "new_message";
        public const string CONNECTION_STATUS = "connection_status";
        public const string ERROR = "error";
    }

    // WebSocket Message Model
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public int? RideId { get; set; }
    }

    // Location Update Model
    public class LocationUpdateData
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Ride Request Data Model
    public class RideRequestData
    {
        public int RideId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public double DropoffLat { get; set; }
        public double DropoffLng { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public decimal EstimatedFare { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedDurationMinutes { get; set; }
    }

    // Connection Info
    public class WebSocketConnection
    {
        public int UserId { get; set; }
        public UserType UserType { get; set; }
        public System.Net.WebSockets.WebSocket Socket { get; set; } = null!;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastPingAt { get; set; } = DateTime.UtcNow;
        public bool IsAlive { get; set; } = true;
    }

    // WebSocket Handler
    public class WebSocketHandler
    {
        private readonly ConcurrentDictionary<string, WebSocketConnection> _connections;
        private readonly IUserService _userService;
        private readonly IRideService _rideService;
        private readonly IDriverLocationService _driverLocationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WebSocketHandler> _logger;

        public WebSocketHandler(
            IUserService userService,
            IRideService rideService,
            IDriverLocationService driverLocationService,
            INotificationService notificationService,
            ILogger<WebSocketHandler> logger)
        {
            _connections = new ConcurrentDictionary<string, WebSocketConnection>();
            _userService = userService;
            _rideService = rideService;
            _driverLocationService = driverLocationService;
            _notificationService = notificationService;
            _logger = logger;

            // Start background cleanup task
            _ = Task.Run(CleanupInactiveConnections);
        }

        public async Task HandleWebSocketAsync(HttpContext context, int userId, UserType userType)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid().ToString();

            var connection = new WebSocketConnection
            {
                UserId = userId,
                UserType = userType,
                Socket = socket
            };

            _connections.TryAdd(connectionId, connection);

            try
            {
                await SendConnectionConfirmation(connectionId);
                await HandleMessages(connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection for user {UserId}", userId);
            }
            finally
            {
                _connections.TryRemove(connectionId, out _);
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
            }
        }

        private async Task HandleMessages(string connectionId)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            var buffer = new byte[4096];

            while (connection.Socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await connection.Socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessage(connectionId, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    connection.LastPingAt = DateTime.UtcNow;
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning(ex, "WebSocket exception for connection {ConnectionId}", connectionId);
                    break;
                }
            }
        }

        private async Task ProcessMessage(string connectionId, string message)
        {
            try
            {
                var wsMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);
                if (wsMessage == null) return;

                if (!_connections.TryGetValue(connectionId, out var connection))
                    return;

                switch (wsMessage.Type)
                {
                    case WebSocketMessageTypes.LOCATION_UPDATE:
                        await HandleLocationUpdate(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.RIDE_REQUEST:
                        await HandleRideRequest(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.RIDE_ACCEPTED:
                        await HandleRideAccepted(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.RIDE_CANCELLED:
                        await HandleRideCancelled(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.DRIVER_ARRIVED:
                        await HandleDriverArrived(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.TRIP_STARTED:
                        await HandleTripStarted(connection, wsMessage);
                        break;

                    case WebSocketMessageTypes.TRIP_COMPLETED:
                        await HandleTripCompleted(connection, wsMessage);
                        break;

                    default:
                        _logger.LogWarning("Unknown message type: {MessageType}", wsMessage.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
                await SendErrorMessage(connectionId, "Error processing message");
            }
        }

        private async Task HandleLocationUpdate(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Driver) return;

            var locationData = JsonSerializer.Deserialize<LocationUpdateData>(message.Data?.ToString() ?? "");
            if (locationData == null) return;

            // Update driver location in database
            await _driverLocationService.UpdateLocationAsync(
                connection.UserId,
                locationData.Latitude,
                locationData.Longitude,
                locationData.Speed,
                locationData.Heading);

            // Update user's current location
            await _userService.UpdateLocationAsync(connection.UserId, locationData.Latitude, locationData.Longitude);

            // Broadcast location to customers with active rides with this driver
            await BroadcastDriverLocationToCustomers(connection.UserId, locationData);
        }

        private async Task HandleRideRequest(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Customer) return;

            var rideData = JsonSerializer.Deserialize<RideRequestData>(message.Data?.ToString() ?? "");
            if (rideData == null) return;

            // Send ride request to nearby drivers
            var nearbyDrivers = await _userService.GetNearbyDriversAsync(
                rideData.PickupLat, rideData.PickupLng, 5);

            var requestMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.RIDE_REQUEST,
                Data = rideData,
                UserId = connection.UserId,
                RideId = rideData.RideId
            };

            foreach (var driver in nearbyDrivers)
            {
                await SendToUserAsync(driver.Id, requestMessage);

                // Send push notification
                await _notificationService.SendRideNotificationAsync(
                    driver.Id,
                    NotificationType.RideRequest,
                    rideData.RideId,
                    "New Ride Request",
                    $"New ride request from {rideData.PickupAddress} to {rideData.DropoffAddress}");
            }
        }

        private async Task HandleRideAccepted(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Driver) return;

            var rideId = message.RideId;
            if (!rideId.HasValue) return;

            var success = await _rideService.AcceptRideAsync(rideId.Value, connection.UserId);
            if (!success) return;

            var ride = await _rideService.GetRideByIdAsync(rideId.Value);
            if (ride == null) return;

            // Notify customer
            var acceptedMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.RIDE_ACCEPTED,
                Data = new
                {
                    RideId = rideId.Value,
                    DriverId = connection.UserId,
                    DriverName = (await _userService.GetUserByIdAsync(connection.UserId))?.FullName,
                    DriverPhone = (await _userService.GetUserByIdAsync(connection.UserId))?.PhoneNumber,
                    Car = await GetDriverCarInfo(connection.UserId),
                    EstimatedArrival = DateTime.UtcNow.AddMinutes(10) // Calculate based on distance
                },
                UserId = connection.UserId,
                RideId = rideId.Value
            };

            await SendToUserAsync(ride.CustomerId, acceptedMessage);

            // Send notification
            await _notificationService.SendRideNotificationAsync(
                ride.CustomerId,
                NotificationType.RideAccepted,
                rideId.Value,
                "Ride Accepted",
                "Your ride has been accepted by a driver");
        }

        private async Task HandleRideCancelled(WebSocketConnection connection, WebSocketMessage message)
        {
            var rideId = message.RideId;
            if (!rideId.HasValue) return;

            var reason = message.Data?.ToString() ?? "No reason provided";
            var success = await _rideService.CancelRideAsync(rideId.Value, reason);
            if (!success) return;

            var ride = await _rideService.GetRideByIdAsync(rideId.Value);
            if (ride == null) return;

            var cancelMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.RIDE_CANCELLED,
                Data = new { RideId = rideId.Value, Reason = reason, CancelledBy = connection.UserId },
                UserId = connection.UserId,
                RideId = rideId.Value
            };

            // Notify the other party
            var targetUserId = connection.UserType == UserType.Customer ? ride.DriverId : ride.CustomerId;
            if (targetUserId.HasValue)
            {
                await SendToUserAsync(targetUserId.Value, cancelMessage);
                await _notificationService.SendRideNotificationAsync(
                    targetUserId.Value,
                    NotificationType.RideCancelled,
                    rideId.Value,
                    "Ride Cancelled",
                    $"The ride has been cancelled. Reason: {reason}");
            }
        }

        private async Task HandleDriverArrived(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Driver) return;

            var rideId = message.RideId;
            if (!rideId.HasValue) return;

            var success = await _rideService.DriverArrivedAsync(rideId.Value);
            if (!success) return;

            var ride = await _rideService.GetRideByIdAsync(rideId.Value);
            if (ride == null) return;

            var arrivedMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.DRIVER_ARRIVED,
                Data = new { RideId = rideId.Value, ArrivedAt = DateTime.UtcNow },
                UserId = connection.UserId,
                RideId = rideId.Value
            };

            await SendToUserAsync(ride.CustomerId, arrivedMessage);

            await _notificationService.SendRideNotificationAsync(
                ride.CustomerId,
                NotificationType.DriverArrived,
                rideId.Value,
                "Driver Arrived",
                "Your driver has arrived at the pickup location");
        }

        private async Task HandleTripStarted(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Driver) return;

            var rideId = message.RideId;
            if (!rideId.HasValue) return;

            var success = await _rideService.StartRideAsync(rideId.Value);
            if (!success) return;

            var ride = await _rideService.GetRideByIdAsync(rideId.Value);
            if (ride == null) return;

            var startedMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.TRIP_STARTED,
                Data = new { RideId = rideId.Value, StartedAt = DateTime.UtcNow },
                UserId = connection.UserId,
                RideId = rideId.Value
            };

            await SendToUserAsync(ride.CustomerId, startedMessage);

            await _notificationService.SendRideNotificationAsync(
                ride.CustomerId,
                NotificationType.TripStarted,
                rideId.Value,
                "Trip Started",
                "Your trip has started");
        }

        private async Task HandleTripCompleted(WebSocketConnection connection, WebSocketMessage message)
        {
            if (connection.UserType != UserType.Driver) return;

            var rideId = message.RideId;
            if (!rideId.HasValue) return;

            var tripData = JsonSerializer.Deserialize<dynamic>(message.Data?.ToString() ?? "");
            var distanceKm = (double)(tripData?.GetProperty("distanceKm").GetDouble() ?? 0);
            var durationMinutes = (int)(tripData?.GetProperty("durationMinutes").GetInt32() ?? 0);

            var success = await _rideService.CompleteRideAsync(rideId.Value, distanceKm, durationMinutes);
            if (!success) return;

            var ride = await _rideService.GetRideByIdAsync(rideId.Value);
            if (ride == null) return;

            var completedMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.TRIP_COMPLETED,
                Data = new
                {
                    RideId = rideId.Value,
                    CompletedAt = DateTime.UtcNow,
                    DistanceKm = distanceKm,
                    DurationMinutes = durationMinutes,
                    Fare = ride.ActualFare
                },
                UserId = connection.UserId,
                RideId = rideId.Value
            };

            await SendToUserAsync(ride.CustomerId, completedMessage);

            await _notificationService.SendRideNotificationAsync(
                ride.CustomerId,
                NotificationType.TripCompleted,
                rideId.Value,
                "Trip Completed",
                $"Your trip has been completed. Fare: {ride.ActualFare:C}");
        }

        // Public methods for external use
        public async Task SendToUserAsync(int userId, WebSocketMessage message)
        {
            var userConnections = _connections.Values
                .Where(c => c.UserId == userId && c.IsAlive)
                .ToList();

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            foreach (var connection in userConnections)
            {
                try
                {
                    if (connection.Socket.State == WebSocketState.Open)
                    {
                        await connection.Socket.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to user {UserId}", userId);
                    connection.IsAlive = false;
                }
            }
        }

        public async Task SendToDriversAsync(IEnumerable<int> driverIds, WebSocketMessage message)
        {
            foreach (var driverId in driverIds)
            {
                await SendToUserAsync(driverId, message);
            }
        }

        public async Task BroadcastToAllAsync(WebSocketMessage message)
        {
            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            var tasks = _connections.Values
                .Where(c => c.IsAlive && c.Socket.State == WebSocketState.Open)
                .Select(async connection =>
                {
                    try
                    {
                        await connection.Socket.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting message to user {UserId}", connection.UserId);
                        connection.IsAlive = false;
                    }
                });

            await Task.WhenAll(tasks);
        }

        private async Task BroadcastDriverLocationToCustomers(int driverId, LocationUpdateData locationData)
        {
            // Find customers with active rides with this driver
            var activeRides = await _rideService.GetActiveRidesAsync();
            var customerIds = activeRides
                .Where(r => r.DriverId == driverId)
                .Select(r => r.CustomerId)
                .ToList();

            var locationMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.LOCATION_UPDATE,
                Data = locationData,
                UserId = driverId
            };

            foreach (var customerId in customerIds)
            {
                await SendToUserAsync(customerId, locationMessage);
            }
        }

        private async Task SendConnectionConfirmation(string connectionId)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            var confirmationMessage = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.CONNECTION_STATUS,
                Data = new { Status = "Connected", UserId = connection.UserId },
                UserId = connection.UserId
            };

            var messageJson = JsonSerializer.Serialize(confirmationMessage);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            try
            {
                await connection.Socket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending connection confirmation to user {UserId}", connection.UserId);
            }
        }

        private async Task SendErrorMessage(string connectionId, string errorMessage)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            var errorMsg = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.ERROR,
                Data = new { Message = errorMessage },
                UserId = connection.UserId
            };

            var messageJson = JsonSerializer.Serialize(errorMsg);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            try
            {
                if (connection.Socket.State == WebSocketState.Open)
                {
                    await connection.Socket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending error message to user {UserId}", connection.UserId);
            }
        }

        private async Task<object?> GetDriverCarInfo(int driverId)
        {
            // This would be implemented in a car service
            // Return car details for the driver
            return new { Make = "Toyota", Model = "Camry", Color = "White", PlateNumber = "ABC123" };
        }

        private async Task CleanupInactiveConnections()
        {
            while (true)
            {
                try
                {
                    var cutoff = DateTime.UtcNow.AddMinutes(-5);
                    var inactiveConnections = _connections
                        .Where(kvp => kvp.Value.LastPingAt < cutoff || !kvp.Value.IsAlive)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var connectionId in inactiveConnections)
                    {
                        if (_connections.TryRemove(connectionId, out var connection))
                        {
                            try
                            {
                                if (connection.Socket.State == WebSocketState.Open)
                                {
                                    await connection.Socket.CloseAsync(
                                        WebSocketCloseStatus.NormalClosure,
                                        "Inactive connection",
                                        CancellationToken.None);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error closing inactive connection for user {UserId}", connection.UserId);
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cleanup task");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        public int GetActiveConnectionsCount() => _connections.Count;

        public IEnumerable<int> GetConnectedUserIds() => _connections.Values.Select(c => c.UserId).Distinct();
    }

    // WebSocket Server Implementation
    public class WebSocketServerImpl : IWebSocketService
    {
        private readonly WebSocketHandler _webSocketHandler;
        private readonly ILogger<WebSocketServerImpl> _logger;

        public WebSocketServerImpl(WebSocketHandler webSocketHandler, ILogger<WebSocketServerImpl> logger)
        {
            _webSocketHandler = webSocketHandler;
            _logger = logger;
        }

        public async Task SendToUserAsync(int userId, object message)
        {
            var wsMessage = new WebSocketMessage
            {
                Type = "custom_message",
                Data = message,
                UserId = userId
            };

            await _webSocketHandler.SendToUserAsync(userId, wsMessage);
        }

        public async Task SendToDriversAsync(IEnumerable<int> driverIds, object message)
        {
            var wsMessage = new WebSocketMessage
            {
                Type = "driver_message",
                Data = message
            };

            await _webSocketHandler.SendToDriversAsync(driverIds, wsMessage);
        }

        public async Task BroadcastToAllAsync(object message)
        {
            var wsMessage = new WebSocketMessage
            {
                Type = "broadcast_message",
                Data = message
            };

            await _webSocketHandler.BroadcastToAllAsync(wsMessage);
        }

        public async Task NotifyRideStatusChange(int rideId, RideStatus status)
        {
            var message = new WebSocketMessage
            {
                Type = "ride_status_change",
                Data = new { RideId = rideId, Status = status.ToString() },
                RideId = rideId
            };

            await _webSocketHandler.BroadcastToAllAsync(message);
        }

        public async Task NotifyLocationUpdate(int driverId, double lat, double lng)
        {
            var locationData = new LocationUpdateData
            {
                UserId = driverId,
                Latitude = lat,
                Longitude = lng
            };

            var message = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.LOCATION_UPDATE,
                Data = locationData,
                UserId = driverId
            };

            await _webSocketHandler.BroadcastToAllAsync(message);
        }

        public async Task NotifyNewRideRequest(int driverId, int rideId)
        {
            var message = new WebSocketMessage
            {
                Type = WebSocketMessageTypes.RIDE_REQUEST,
                Data = new { DriverId = driverId, RideId = rideId },
                UserId = driverId,
                RideId = rideId
            };

            await _webSocketHandler.SendToUserAsync(driverId, message);
        }
    }
}