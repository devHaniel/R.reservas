using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Reservas.Data;
using Reservas.Hubs;
using Reservas.Middleware;
using Reservas.Models.Entities;
using Reservas.Services;
using Reservas.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); 

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new Reservas.Common.Json.TimeSpanJsonConverter());
    });
builder.Services.AddSignalR();
builder.Services.AddIdentityCore<Usuario>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer no configurado");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience no configurado");
var accessTokenMinutes = builder.Configuration.GetValue<int>("Jwt:AccessTokenMinutes");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("JWT Key debe tener al menos 32 bytes");

if (accessTokenMinutes <= 0)
    throw new InvalidOperationException("Jwt:AccessTokenMinutes debe ser mayor que cero");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
    throw new InvalidOperationException("Configure al menos un origen en Cors:AllowedOrigins");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen();
// Services 

builder.Services.AddScoped<ITipoServicioService, TipoServicioService>();
builder.Services.AddScoped<IRecursoReservableService, RecursoReservableService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRealtimeNotificacionService, RealtimeNotificacionService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("fixed-policy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(10),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    foreach (var roleName in new[] { "Admin", "Vendedor" })
    {
        if (await roleManager.RoleExistsAsync(roleName))
            continue;

        var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"No se pudo crear el rol {roleName}: {errors}");
        }
    }

    if (!(await userManager.GetUsersInRoleAsync("Admin")).Any())
    {
        var adminEmail = builder.Environment.IsDevelopment()
            ? "admin@reservas.local"
            : builder.Configuration["BootstrapAdmin:Email"];
        var adminPassword = builder.Environment.IsDevelopment()
            ? "Admin1234@"
            : builder.Configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException(
                "No existe un Admin. Configure BootstrapAdmin:Email y BootstrapAdmin:Password mediante secretos.");

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new Usuario
            {
                Nombre = "admin",
                Email = adminEmail,
                UserName = builder.Environment.IsDevelopment() ? "admin" : adminEmail
            };
            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"No se pudo crear el Admin inicial: {errors}");
            }
        }
        else if (!await userManager.CheckPasswordAsync(admin, adminPassword))
        {
            throw new InvalidOperationException(
                "La contraseña configurada para el Admin existente no coincide.");
        }

        var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(", ", addRoleResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"No se pudo asignar el rol Admin: {errors}");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapSwagger();
    app.MapSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Este es el endpoint que expone SignalR
app.MapHub<NotificacionHub>("/hubs/notificaciones");

app.Run();
