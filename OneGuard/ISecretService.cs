namespace OneGuard;

public interface ISecretService
{
    Task<SecretResponse> GenerateAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);

    Task VerifyAsync(string secret, CancellationToken cancellationToken = default);
}

public sealed record SecretResponse
{
    public string Secret { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}