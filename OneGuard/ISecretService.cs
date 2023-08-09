namespace OneGuard;

public interface ISecretService
{
    Task<SecretResponse> GenerateAsync(string phoneNumber, string otp, Guid endpointId, CancellationToken cancellationToken = default);

    Task VerifyAsync(string secret, string phoneNumber, Guid endpointId, CancellationToken cancellationToken = default);
}

public sealed record SecretResponse
{
    public string Secret { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}