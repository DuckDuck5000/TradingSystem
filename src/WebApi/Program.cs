using System.Text.Json.Serialization;
using Domain.Services;
using WebApi.Services;
using Confluent.Kafka;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<MatchingEngine>();
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = "host.docker.internal:9092", // or "localhost:9092" if appropriate
        SecurityProtocol = SecurityProtocol.Plaintext
    };
    return new ProducerBuilder<Null, string>(config).Build();
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This converter allows string "Buy"/"Sell" to parse into the enum
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSingleton<IOrderProducer>(sp =>
{
    // The "localhost:9092" matches your docker Kafka instance
    return new KafkaOrderProducer("localhost:9092");
});

builder.Services.AddHostedService<OrderConsumerWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();
app.Use(async (context, next) =>
{
    Console.WriteLine($"Received request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});
//app.UseHttpsRedirection();


// Make UI and shit!

app.MapGet("/weatherforecast", () =>
{
    var forecast =  "Hello, world, chat?";
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();