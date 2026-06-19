using EventosVivos.Application.Services;
using EventosVivos.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IReservationService, ReservationService>();

            return services;
        }
    }
}
