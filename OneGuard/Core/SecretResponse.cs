namespace OneGuard.Core;

public sealed record SecretResponse
{
    public string Secret { get; set; } = default!;

    public int TtlInSeconds { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}