using System.Net.Http.Json;
using EventManager.Common.DTOs;
using Polly;
namespace Booking.Application.Services.UserClientService;

public class UserClientService : IUserClientService
{
    public async Task<UserExistingStatus?> CheckIfUserExistAsync(Guid id)
    {
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (_, timespan, retryCount, _) =>
                {
                    Console.WriteLine($"Попытка {retryCount} через {timespan.Seconds} сек.");
                });
        using var httpClient = new HttpClient()
        {
            //TODO в appsettings
            BaseAddress = new Uri("http://localhost:5283")
        };
        var responseMessage = await retryPolicy.ExecuteAsync(async () =>
            await httpClient.GetAsync($"Auth/{id}"));
        return await responseMessage.Content.ReadFromJsonAsync<UserExistingStatus>();
    }

}
