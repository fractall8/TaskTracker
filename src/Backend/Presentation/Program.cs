using Application.DI;
using Application.Settings;
using Domain.Constants;
using Infrastructure.DI;
using Microsoft.AspNetCore.Http.Features;
using Persistence.DI;
using Presentation.Configuration;
using Presentation.Extensions;
using Presentation.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options => options.Conventions.Add(new PrefixConventionConfigurator("api")));
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

builder.Services.AddPresentationCors(builder.Configuration);
builder.Services.AddGlobalErrorHandling();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .Destructure.With<SensitiveDataDestructuringPolicy>() 
    
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var fileSettings = builder.Configuration.GetSection("FileSettings").Get<FileSettings>();
if (fileSettings != null)
{
    var maxAllowedBytes = (fileSettings.Attachments.MaxSizeMb * 1024 * 1024) + (1 * 1024 * 1024);
    
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxAllowedBytes;
    });

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = maxAllowedBytes;
    });
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

app.MapControllers();

app.Run();
