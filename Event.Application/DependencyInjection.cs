using Event.Application.Interfaces;
using Event.Application.Services.BookingMessagesProcess;
using Event.Application.Services.EventService;
using Event.Application.Services.EventValidatorService;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventValidatorService, EventValidatorService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingMessagesProcess, BookingMessagesProcess>();

        return services;
    }
}
