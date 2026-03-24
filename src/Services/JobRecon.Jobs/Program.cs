using JobRecon.Jobs.Endpoints;
using JobRecon.Jobs.Extensions;
using JobRecon.Jobs.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJobsServices(builder.Configuration);
builder.Services.AddJobsHangfire(builder.Configuration);
builder.Services.AddJobsAuthentication(builder.Configuration);
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<JobRecon.Jobs.Grpc.ApiKeyInterceptor>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "JobRecon Jobs API";
    config.Version = "v1";
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<JobRecon.Jobs.Infrastructure.JobsDbContext>("database");

var app = builder.Build();

app.ConfigurePipeline();
app.MapJobEndpoints();
app.MapJobSourceEndpoints();
app.MapGrpcService<JobsGrpcService>();

await app.MigrateDatabaseAsync();
app.ConfigureRecurringJobs();

app.Run();
