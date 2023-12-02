namespace OneGuard.Core;

public interface IOtpRequest
{
    Task<OtpRequest> RequestAsync(Guid endpointId, string phoneNumber, string? messageData = null, CancellationToken cancellationToken = default);

    Task<OtpRequest> GetAsync(string otpRequest, CancellationToken cancellationToken = default);

    Task VerifyAsync(string otpRequestId, CancellationToken cancellationToken = default);
}

public sealed record OtpRequest
{
    public string OtpRequestId { get; set; } = default!;

    public Guid EndpointId { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public string? MessageData { get; set; }

    public DateTime ExpireAtUtc { get; set; }
}