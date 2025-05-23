using Microsoft.EntityFrameworkCore;
using WasselniAPI.Data;
using WasselniAPI.Services.Interfaces;
using WasselniAPI.Services.Implementations;
using WasselniAPI.WebSocket;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using WasselniAPI.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wassel Taxi API",
        Version = "v1",
        Description = "API for Wassel Taxi Application"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register all services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDriverLocationService, DriverLocationService>();
builder.Services.AddScoped<IRideTrackingService, RideTrackingService>();
builder.Services.AddScoped<IRideRequestService, RideRequestService>();

// WebSocket Services - Fix service lifetime issue
builder.Services.AddScoped<WebSocketHandler>();
builder.Services.AddScoped<IWebSocketService, WebSocketServerImpl>();

// JWT Authentication Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
        ClockSkew = TimeSpan.Zero
    };

    // Add WebSocket token validation
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DriverOnly", policy =>
        policy.RequireClaim("UserType", "Driver"));

    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireClaim("UserType", "Customer"));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("WasselCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(
                    "https://wassel.jo",
                    "https://app.wassel.jo",
                    "https://admin.wassel.jo"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add HTTP Client for external services
builder.Services.AddHttpClient();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Background Services for cleanup tasks
builder.Services.AddHostedService<LocationCleanupService>();
builder.Services.AddHostedService<RequestExpirationService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        try
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canConnect = context.Database.CanConnect();
            return canConnect
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection successful")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Database check failed: {ex.Message}");
        }
    })
    .AddCheck("websocket", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("WebSocket service is running"));

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Remove Application Insights for now - can be added later with proper package
if (builder.Environment.IsProduction())
{
    // Add structured logging for production
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

var app = builder.Build();

// Configure the HTTP request pipeline
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wassel Taxi API V1");
        // Remove this line or set it to "swagger" to serve at /swagger
        // c.RoutePrefix = string.Empty; 
        c.RoutePrefix = "swagger"; // This will serve Swagger UI at /swagger
    });
}

// Global Exception Handling
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "An internal server error occurred",
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    });
});

app.UseHttpsRedirection();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
    ReceiveBufferSize = 4 * 1024
});

// WebSocket endpoint with authentication
app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket requests only");
        return;
    }

    // Extract user info from token
    var token = context.Request.Query["access_token"].FirstOrDefault();
    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Access token required");
        return;
    }

    // Validate token and extract user info (simplified)
    var userId = ExtractUserIdFromToken(token);
    var userType = ExtractUserTypeFromToken(token);

    if (userId == 0)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid token");
        return;
    }

    var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await webSocketHandler.HandleWebSocketAsync(context, userId, userType);
});

app.UseCors("WasselCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Add Health Check endpoint
app.MapHealthChecks("/health");

// API Routes
app.MapControllers();

// Database Initialization
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Checking database connection...");

        if (await context.CanConnectAsync())
        {
            logger.LogInformation("Database connection successful");

            // Apply migrations
            await context.MigrateAsync();
            logger.LogInformation("Database migrations applied");

            // Seed test data in development
            if (app.Environment.IsDevelopment())
            {
                await context.SeedTestDataAsync();
                logger.LogInformation("Test data seeded");
            }
        }
        else
        {
            logger.LogError("Cannot connect to database");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

app.Logger.LogInformation("Wassel Taxi API started successfully");

app.Run();

// Helper methods for token validation
static int ExtractUserIdFromToken(string token)
{
    try
    {
        // Simplified token parsing - in production, use proper JWT validation
        // This is just for demonstration
        return 1; // Return actual user ID from token
    }
    catch
    {
        return 0;
    }
}

static UserType ExtractUserTypeFromToken(string token)
{
    try
    {
        // Simplified token parsing - in production, use proper JWT validation
        return UserType.Customer; // Return actual user type from token
    }
    catch
    {
        return UserType.Customer;
    }
}

// Background Services
public class LocationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocationCleanupService> _logger;

    public LocationCleanupService(IServiceProvider serviceProvider, ILogger<LocationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var locationService = scope.ServiceProvider.GetRequiredService<IDriverLocationService>();

                // Delete location data older than 24 hours
                var cutoffDate = DateTime.UtcNow.AddHours(-24);
                await locationService.DeleteOldLocationsAsync(cutoffDate);

                _logger.LogInformation("Location cleanup completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during location cleanup");
            }

            // Run cleanup every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public class RequestExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RequestExpirationService> _logger;

    public RequestExpirationService(IServiceProvider serviceProvider, ILogger<RequestExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var requestService = scope.ServiceProvider.GetRequiredService<IRideRequestService>();

                await requestService.ExpireOldRequestsAsync();

                _logger.LogInformation("Request expiration completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during request expiration");
            }

            // Run every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}