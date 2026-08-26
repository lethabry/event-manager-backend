using Auth.Application.Services.UserService;
using Auth.Application.Services.UserValidator;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserValidator, UserValidator>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
