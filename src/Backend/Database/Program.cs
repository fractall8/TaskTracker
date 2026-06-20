using System.Reflection;
using Database;
using DbUp;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting migration...");

    var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    var connectionString = configuration.GetConnectionString(ConnectionStrings.PostgresConnection);

    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Error("Error: ConnectionString is null or empty");
        return -1;
    }

    EnsureDatabase.For.PostgresqlDatabase(connectionString);

    var upgrader = DeployChanges.To
        .PostgresqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
        .LogTo(new SerilogUpgradeLog(Log.Logger))
        .Build();

    var result = upgrader.PerformUpgrade();

    if (!result.Successful)
    {
        Log.Error($"An error occurred while migrating the database: {result.Error}");
        return -1;
    }

    Log.Information("Migration succeeded!");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while migrating the database.");
    return -1;
}
finally
{
    Log.CloseAndFlush();
}
