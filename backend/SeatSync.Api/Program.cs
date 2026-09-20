using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Options;
using SeatSync.Core.Services;
using SeatSync.Infrastructure.Data;
using SeatSync.Infrastructure.Repositories;
using SeatSync.Infrastructure.Security;
using SeatSync.Core.Entities;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/seatsync-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// --- Configuration binding ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

// --- Database ---
builder.Services.AddDbContext<SeatSyncDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- DI: Core interfaces -> Infrastructure implementations ---
// This block is the whole point of the architecture: Api only knows about
// SeatSync.Core interfaces when writing controllers; it only knows about
// SeatSync.Infrastructure implementations right here, in one place.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<ISeatLockService, SeatSync.Infrastructure.Redis.RedisSeatLockService>();
builder.Services.AddScoped<IHoldService, HoldService>();
builder.Services.AddHostedService<SeatSync.Infrastructure.Redis.SeatExpiryListenerService>();

builder.Services.Configure<SeatSync.Infrastructure.Messaging.RabbitMqOptions>(
    builder.Configuration.GetSection(SeatSync.Infrastructure.Messaging.RabbitMqOptions.SectionName));

builder.Services.AddSingleton<IEventPublisher, SeatSync.Infrastructure.Messaging.RabbitMqEventPublisher>();
builder.Services.AddScoped<IBookingRepository, SeatSync.Infrastructure.Repositories.BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddHostedService<SeatSync.Infrastructure.Messaging.BookingConsumerService>();

builder.Services.AddSignalR();
builder.Services.AddScoped<ISeatMapNotifier, SeatSync.Api.Hubs.SignalRSeatMapNotifier>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IReceiptGenerator, SeatSync.Infrastructure.Documents.QuestPdfReceiptGenerator>();
// --- Authentication ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// --- Controllers ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SeatSync.Api.Middleware.RequestLoggingMiddleware>();
app.MapControllers();
app.MapHub<SeatSync.Api.Hubs.SeatMapHub>("/hubs/seatmap");

try
{
    Log.Information("SeatSync API starting up");
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

app.Run();