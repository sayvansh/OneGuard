namespace OneGuard;

public interface IOtpService
{
    Task SendAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
}