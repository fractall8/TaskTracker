using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting migration...");

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Error: ConnectionString is null or empty");
    return -1;
}

EnsureDatabase.For.PostgresqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();
    
var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.WriteLine($"An error occurred while migrating the database: {result.Error}");
    return -1;
}    

Console.WriteLine("Migration succeeded!");
return 0;