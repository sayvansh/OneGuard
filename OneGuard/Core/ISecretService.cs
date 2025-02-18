namespace OneGuard.Core;

public interface ISecretService
{
    Task<SecretResponse> GenerateAsync(string phoneNumber, string otp, Guid endpointId, CancellationToken cancellationToken = default);

    Task VerifyAsync(string secret, string phoneNumber, Guid endpointId, CancellationToken cancellationToken = default);
}