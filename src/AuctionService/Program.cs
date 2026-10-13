using AuctionService.Data;
using AuctionService.Errors;
using Contracts;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception ("Connection string is empty");

builder.Services.AddDbContextWithWolverineIntegration<AuctionDBContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(connectionString,"auctions_rmq");

    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.UseRabbitMq(rabbit =>
    {
        rabbit.HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    })
    .DeclareExchange("auction-created", ex => ex.ExchangeType = ExchangeType.Fanout)
    .DeclareExchange("auction-updated", ex => ex.ExchangeType = ExchangeType.Fanout)
    .DeclareExchange("auction-deleted", ex => ex.ExchangeType = ExchangeType.Fanout)
    .AutoProvision();

    opts.PublishMessage<AuctionCreated>().ToRabbitExchange("auction-created");
    opts.PublishMessage<AuctionCreated>().ToRabbitExchange("auction-updated");
    opts.PublishMessage<AuctionCreated>().ToRabbitExchange("auction-deleted");

    opts.ListenToRabbitQueue("wolverine-dead-letter-queue");
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
try
{
    DBInitializer.InitDB(app);
}
catch( Exception e)
{
    Console.WriteLine($"Error initalizing database: {e.Message}");
}

app.Run();
