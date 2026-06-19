using EventosVivos.Application.Interfaces;
using EventosVivos.Application.Services;
using EventosVivos.Infrastructure.Common;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Repositories;
using EventosVivos.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// Database — PostgreSQL via Npgsql EF Core
// -----------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("EventosVivos.Infrastructure")));

// -----------------------------------------------------------------------
// Repositories & Unit of Work
// -----------------------------------------------------------------------
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// -----------------------------------------------------------------------
// Application Services
// -----------------------------------------------------------------------
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ReservationService>();

// -----------------------------------------------------------------------
// API
// -----------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EventosVivos API", Version = "v1" });
});

// -----------------------------------------------------------------------
// CORS — permite peticiones desde Angular dev server y producción
// -----------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200"])
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// -----------------------------------------------------------------------
// Middleware pipeline
// -----------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventosVivos API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.MapControllers();

// -----------------------------------------------------------------------
// Auto-apply migrations and seed on startup (dev/staging convenience)
// -----------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Expose Program for integration test WebApplicationFactory
public partial class Program { }
