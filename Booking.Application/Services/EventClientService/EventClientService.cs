using System.Net.Http.Json;
using EventManager.Common.DTOs;
using Polly;
namespace Booking.Application.Services.EventClientService;

public class EventClientService : IEventClientService
{
    public async Task<EventResponseDTO?> GetEventByIdAsync(Guid id)
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
            BaseAddress = new Uri("http://localhost:5018")
        };
        var responseMessage = await retryPolicy.ExecuteAsync(async () =>
            await httpClient.GetAsync($"Events/{id}"));
        return await responseMessage.Content.ReadFromJsonAsync<EventResponseDTO>();
    }

}
