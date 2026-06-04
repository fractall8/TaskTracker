using Application;
using Application.Settings;
using Infrastructure;
using Infrastructure.DI;
using Microsoft.AspNetCore.Http.Features;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
