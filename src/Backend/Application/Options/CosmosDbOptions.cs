namespace Application.Options;


public class CosmosDbOptions
{
    public const string SectionName = "CosmosDB";
    public required string DatabaseName { get; set; }
    public required CosmosDbContainers Containers { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            throw new InvalidOperationException("CosmosDB:DatabaseName is not configured.");
        }

        if (Containers == null || string.IsNullOrWhiteSpace(Containers.BoardExport))
        {
            throw new InvalidOperationException("CosmosDB:Containers:BoardExport is not configured.");
        }
    }
}

public class CosmosDbContainers
{
    public required string BoardExport { get; set; }
}
