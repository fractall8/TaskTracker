namespace TaskTracker.Functions.ExternalProviders.CosmosDB;

public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDB";

    public required string DatabaseName { get; init; }

    public required CosmosDbContainerNamesOptions Containers { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            throw new InvalidOperationException($"{SectionName}:DatabaseName is not configured.");
        }

        if (Containers is null)
        {
            throw new InvalidOperationException($"{SectionName}:Containers is not configured.");
        }

        Containers.Validate();
    }
}

public sealed class CosmosDbContainerNamesOptions
{
    public required string BoardExport { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(BoardExport))
        {
            throw new InvalidOperationException($"{CosmosDbOptions.SectionName}:Containers:BoardExport is not configured.");
        }
    }
}
