using System.Reflection;
using EventManager.BackgroundServices;
using EventManager.Data.BookingRepository;
using EventManager.Data.EventRepository;
using EventManager.Middleware;
using EventManager.Services.BookingService;
using EventManager.Services.EventService;
using EventManager.Services.ValidationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddSingleton<IEventRepository, EventRepository>();
builder.Services.AddSingleton<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddHostedService<BookingConfirmationService>();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseErrorHandling();
app.MapControllers();

app.Run();