using EventosVivos.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<EventService>();
            services.AddScoped<ReservationService>();

            return services;
        }
    }
}
