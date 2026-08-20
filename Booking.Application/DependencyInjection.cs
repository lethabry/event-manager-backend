using Booking.Application.Services.BookingConfirmationService;
using Booking.Application.Services.BookingService;
using Booking.Application.Services.EventClientService;
using Booking.Application.Services.UserClientService;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingConfirmationService, BookingConfirmationService>();
        services.AddScoped<IEventClientService, EventClientService>();
        services.AddScoped<IUserClientService, UserClientService>();

        return services;
    }
}
