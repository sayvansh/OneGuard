namespace OneGuard.Core;

public class Endpoint
{
    protected Endpoint()
    {
    }

    public Endpoint(string url, string body)
    {
        Url = url;
        Content = body;
    }

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = default!;

    public int OtpTtl { get; set; } = 120;

    public int SecretTtl { get; set; } = 900;

    public int Length { get; set; } = 4;

    public string Content { get; set; } = default!;
}