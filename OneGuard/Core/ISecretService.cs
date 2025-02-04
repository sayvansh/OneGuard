namespace OneGuard.Core;

public interface ISecretService
{
    Task<SecretResponse> GenerateAsync(string phoneNumber, string otp, Guid endpointId, CancellationToken cancellationToken = default);

    Task<VerifySecretResponse> VerifyAsync(string secret, string phoneNumber, Guid endpointId, CancellationToken cancellationToken = default);
}