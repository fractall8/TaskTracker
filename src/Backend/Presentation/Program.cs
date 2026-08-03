using System.Text.Json.Serialization;
using Application.DI;
using Application.Interfaces.Services;
using Application.Options;
using Application.Settings;
using Hangfire;
using Infrastructure.Boards.Hubs;
using Infrastructure.DI;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Persistence.DI;
using Presentation.Configuration;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Logging;
using Presentation.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options => options.Conventions.Add(new PrefixConventionConfigurator("api")))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPresentationCors(builder.Configuration);
builder.Services.AddGlobalErrorHandling();
builder.Services.AddPresentationRateLimiting(builder.Configuration);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var fileSettings = builder.Configuration.GetSection("FileSettings").Get<FileSettings>();
if (fileSettings != null)
{
    var maxAllowedBytes = (fileSettings.Attachments.MaxSizeMb * 1024 * 1024) + (1 * 1024 * 1024);

    builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = maxAllowedBytes; });

    builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.Limits.MaxRequestBodySize = maxAllowedBytes; });
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(CorsPolicies.DefaultCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// After authentication so the limiter can partition on the caller's identity rather than their IP.
app.UseRateLimiter();

app.UseMiddleware<InternalApiKeyMiddleware>();

app.MapControllers();

app.UseHangfireDashboard();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var schedulerOptions = services.GetRequiredService<IOptions<BoardExportSchedulerOptions>>().Value;
    var recoveryOptions = services.GetRequiredService<IOptions<BoardExportRecoverySchedulerOptions>>().Value;

    RecurringJob.AddOrUpdate<IBoardExportSchedulerJob>(
        "board-export-scheduler",
        job => job.RunAsync(CancellationToken.None),
        schedulerOptions.CronExpression);

    RecurringJob.AddOrUpdate<IBoardExportRecoverySchedulerJob>(
        "board-export-recovery-scheduler",
        job => job.RunAsync(CancellationToken.None),
        recoveryOptions.CronExpression);
}

app.MapHub<BoardExportStatusHub>("/hubs/board-export-status");
app.MapHub<BoardActionsHub>("/hubs/board-actions");

app.Run();
