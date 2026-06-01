using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Constants;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Application;

var builder = WebApplication.CreateBuilder(args);

var postgresSqlConnectionString = builder.Configuration.GetConnectionString(ConnectionStrings.PostgresConnection);
builder.Services.AddDbContext<TaskTrackerDbContext>(options => options.UseNpgsql(postgresSqlConnectionString));

var blobConnectionString = builder.Configuration.GetConnectionString(ConnectionStrings.AzureBlobStorageConnection);
builder.Services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));

builder.Services.AddScoped<IFileService, BlobStorageService>();

builder.Services.AddApplicationServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
