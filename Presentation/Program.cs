using Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Application;
using Application.Settings;
using Infrastructure;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// this already moved to extension method in Persistence project
// but this PR created before then PR with repos and uow merged
var postgresSqlConnectionString = builder.Configuration.GetConnectionString(ConnectionStrings.PostgresConnection);
builder.Services.AddDbContext<TaskTrackerDbContext>(options => options.UseNpgsql(postgresSqlConnectionString));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

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

app.MapControllers();

app.Run();
