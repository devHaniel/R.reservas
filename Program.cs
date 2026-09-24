using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Reservas.Data;
using Reservas.Hubs;
using Reservas.Models.Entities;
using Reservas.Services;
using Reservas.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); 

builder.Services.AddControllers();
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
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
builder.Services.AddScoped<IRealtimeNotificacionService, RealtimeNotificacionService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapSwagger();
    app.MapSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Este es el endpoint que expone SignalR
app.MapHub<NotificacionHub>("/hubs/notificaciones");

app.Run();
