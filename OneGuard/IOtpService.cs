namespace OneGuard;

public interface IOtpService
{
    Task SendAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<string> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
}