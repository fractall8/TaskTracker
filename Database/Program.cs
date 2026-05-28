using System.Reflection;
using DbUp;

Console.WriteLine("Starting migration...");

var connectionString = args.FirstOrDefault() ?? "here connection string"; // local docker move to .env or smth 

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