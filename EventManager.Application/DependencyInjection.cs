using EventManager.Application.Services.BookingService;
using EventManager.Application.Services.EventService;
using EventManager.Application.Services.ValidationService;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
