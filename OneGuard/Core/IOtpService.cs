namespace OneGuard.Core;

public interface IOtpService
{
    Task SendAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default);

    Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
}