using Application.Interfaces;
using Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Persistence.Repositories;
using Persistence.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<TaskTrackerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(ConnectionStrings.PostgresConnection)));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
