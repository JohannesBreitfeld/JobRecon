using JobRecon.Identity.Extensions;
using JobRecon.Identity.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddGrpc();

var app = builder.Build();

app.ConfigurePipeline();
app.MapEndpoints();
app.MapGrpcService<IdentityGrpcService>();

await app.SeedDataAsync();

app.Run();
