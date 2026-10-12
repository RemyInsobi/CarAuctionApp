using Meilisearch;
using SearchService.data;
using SearchService.Endpoints;
using SearchService.Models;
using SearchService.services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    return new MeilisearchClient(
        config["Meilisearch:Url"],
        config["Meilisearch:ApiKey"]
    );
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
    options.Retry.MaxRetryAttempts = 5;
    options.Retry.Delay = TimeSpan.FromSeconds(10);
    options.Retry.OnRetry = args =>
    {
        Console.WriteLine(
            $"Auction svc unavailable. Retry {args.AttemptNumber +1}/5"
        );
        return default;
    };
});

var app = builder.Build();

app.MapGet("/api/search", SearchEndpoints.GetSearchResults);
app.MapGet("/api/search/{id}", SearchEndpoints.GetAuctionById);

try
{
    await DBInitializer.ConfigureIndex(app);
}
catch( Exception e)
{
    Console.WriteLine($"Failed to seed search: {e.Message}");
}

_ = Task.Run(async () => 
{
    try
    {
        await DBInitializer.FetchMissingAuctions(app);
    }
    catch( Exception e)
    {
        Console.WriteLine($"Failed to seed search: {e.Message}");
    }
});

// Configure the HTTP request pipeline.



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



app.Run();


