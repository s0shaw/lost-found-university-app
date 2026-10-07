using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using UniversityLostFound.Api.Auth;
using UniversityLostFound.Api.Errors;
using UniversityLostFound.Application;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Infrastructure;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;
using UniversityLostFound.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

// Read again here (also read in AddInfrastructure): the bearer options need the same key.
var jwt = JwtSettings.Read(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // keep "sub" and "role" as they were issued
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.Key,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = StaffClaims.Role,
            NameClaimType = StaffClaims.UniversityId,
        };

        // Token lives in the cookie; the Authorization header still works for Scalar.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies[StaffCookie.Name];
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// The frontend's rewrite proxy (and the platform load balancer) sit in front of the api. Trust their
// forwarded headers only when the connection comes from one of these networks.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = builder.Configuration.GetValue("Proxy:ForwardLimit", 1);
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var network in builder.Configuration.GetSection("Proxy:TrustedNetworks").Get<string[]>() ?? [])
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

var permitsPerHour = builder.Configuration.GetValue("RateLimiting:PermitsPerHour", 150);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter =
            TimeSpan.FromHours(1).TotalSeconds.ToString(CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // The platform health check must never be throttled; it shares its caller's ip with nothing else we serve.
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        // The caller's ip comes from UseForwardedHeaders, which only honours X-Forwarded-For from the
        // trusted proxy networks below; a direct caller's forged header is ignored.
        // TestServer leaves RemoteIpAddress null, so every test request shares one partition.
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerHour,
                Window = TimeSpan.FromHours(1),
            });
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    // The seed creates staff accounts. Outside development it must not do so with the public demo password.
    var seedPassword = app.Configuration["Seed:StaffPassword"];
    if (!app.Environment.IsDevelopment()
        && (string.IsNullOrWhiteSpace(seedPassword) || seedPassword.Length < 12 || seedPassword == "staffdemo123"))
    {
        throw new InvalidOperationException(
            "Database:SeedOnStartup outside Development needs Seed:StaffPassword set to a private value of at least 12 characters.");
    }

    using var scope = app.Services.CreateScope();
    await DatabaseSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<IClock>().UtcNow,
        app.Configuration["Seed:StaffPassword"]);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    headers.XFrameOptions = "DENY";

    // A JSON api serves no markup; the interactive docs (development only) need their own scripts.
    if (!app.Environment.IsDevelopment())
    {
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    }

    await next();
});

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();      // interactive API docs at /scalar
}

app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

/// <summary>Exposes the implicit Program class to the integration test project.</summary>
public sealed partial class Program;
