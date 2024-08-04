using System.Text.Json.Serialization;

public class Principal
{
    [JsonPropertyName("AWS")]
    public List<string> AWS { get; set; } = [];
}

public class Statement
{
    [JsonPropertyName("Effect")]
    public string Effect { get; set; } = "";

    [JsonPropertyName("Principal")]
    public Principal Principal { get; set; } = new Principal();

    [JsonPropertyName("Action")]
    public List<string> Action { get; set; } = [];

    [JsonPropertyName("Resource")]
    public List<string> Resource { get; set; } = [];
}

public class Policy
{
    [JsonPropertyName("Version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("Statement")]
    public List<Statement> Statement { get; set; } = [];
}