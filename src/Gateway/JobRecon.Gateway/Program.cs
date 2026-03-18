using JobRecon.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

app.ConfigurePipeline();
app.MapEndpoints();

app.Run();
