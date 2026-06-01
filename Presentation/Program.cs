using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Constants;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<TaskTrackerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ConnectionStrings.PostgresConnection)));
builder.Services.AddSingleton(x => new BlobServiceClient(ConnectionStrings.AzureBlobStorageConnection));

builder.Services.AddScoped<IFileService, BlobStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
