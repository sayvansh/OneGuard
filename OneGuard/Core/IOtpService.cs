namespace OneGuard.Core;

public interface IOtpService
{
    Task<OtpResponse> SendAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default);

    Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
}

public sealed record OtpResponse
{
    public int TtlInSeconds { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}