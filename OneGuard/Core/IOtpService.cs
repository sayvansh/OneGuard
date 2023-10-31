namespace OneGuard.Core;

public interface IOtpService
{
    Task<OtpRequestResponse> RequestAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default);

    Task<OtpResponse> SendAsync(string otpRequestId, CancellationToken cancellationToken = default);

    Task<SecretResponse> VerifyAsync(string otpRequestId, string otp, CancellationToken cancellationToken = default);
}

public sealed record OtpRequestResponse
{
    public string OtpRequestId { get; set; } = default!;

    public Guid EndpointId { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}

public sealed record OtpResponse
{
    public int TtlInSeconds { get; set; } = default!;

    public DateTime ExpireAtUtc { get; set; }
}