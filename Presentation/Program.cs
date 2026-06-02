using Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// this already moved to extension method in Persistence project
// but this PR created before then PR with repos and uow merged
var postgresSqlConnectionString = builder.Configuration.GetConnectionString(ConnectionStrings.PostgresConnection);
builder.Services.AddDbContext<TaskTrackerDbContext>(options => options.UseNpgsql(postgresSqlConnectionString));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

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
