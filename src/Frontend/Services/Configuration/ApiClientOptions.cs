namespace Services.Configuration;

public class ApiClientOptions
{
    public const string SectionName = "ApiClientOptions";
    public string BaseUrl { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = [];
}