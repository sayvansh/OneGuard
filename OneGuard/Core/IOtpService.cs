namespace OneGuard.Core;

public interface IOtpService
{
    Task<OtpResponse> SendAsync(string otpRequestId, CancellationToken cancellationToken = default);

    Task<SecretResponse> VerifyAsync(string otpRequestId, string otp, CancellationToken cancellationToken = default);
}

public sealed record OtpResponse
{
    public int TtlInSeconds { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}