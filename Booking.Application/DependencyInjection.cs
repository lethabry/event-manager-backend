using Booking.Application.Services.BookingConfirmationService;
using Booking.Application.Services.BookingMessagesProcess;
using Booking.Application.Services.BookingService;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingConfirmationService, BookingConfirmationService>();
        services.AddScoped<IBookingMessagesProcess, BookingMessagesProcess>();

        return services;
    }
}
