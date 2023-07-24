using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Exceptions;

namespace OneGuard.Services;

internal sealed class OtpService : IOtpService
{
    private readonly ISecretService _secretService;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _clientFactory;
    private const string SendOtpApiUrl = "SecureOtp/OtpRequest";
    private const string VerifyOtpApiUrl = "SecureOtp/VerifyOtp";


    public OtpService(ISecretService secretService, IDistributedCache cache, IHttpClientFactory clientFactory)
    {
        _secretService = secretService;
        _cache = cache;
        _clientFactory = clientFactory;
    }

    public async Task SendAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.CreateClient("Otp");
        var sendOtpResponseMessage = await client.PostAsJsonAsync(SendOtpApiUrl, new
        {
            PhoneNumber = phoneNumber,
        }, cancellationToken: cancellationToken);


        var sendOtpResponse =
            await sendOtpResponseMessage.Content.ReadFromJsonAsync<GetSendOtpResponse>(
                cancellationToken: cancellationToken);

        if (sendOtpResponse is null)
        {
            throw new OtpFailedToSendException();
        }

        var recordId = sendOtpResponse.Id;
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(sendOtpResponse.Ttl));
        await _cache.SetStringAsync(phoneNumber, recordId, options, cancellationToken);
    }

    public async Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {
        var recordId = await _cache.GetStringAsync(phoneNumber, token: cancellationToken);

        if (recordId is null)
        {
            throw new OtpNotVerifiedException();
        }

        var client = _clientFactory.CreateClient("Otp");
        var verifyOtpResponseMessage = await client.PostAsJsonAsync(VerifyOtpApiUrl, new
        {
            PhoneNumber = phoneNumber,
            Otp = otp,
            RecordId = recordId
        }, cancellationToken: cancellationToken);
        var verifyOtpResponse =
            await verifyOtpResponseMessage.Content.ReadFromJsonAsync<GetVerifyOtpResponse>(
                cancellationToken: cancellationToken);

        if (verifyOtpResponse is { IsValid: false })
        {
            throw new OtpNotVerifiedException();
        }

        var secret = await _secretService.GenerateAsync(phoneNumber, otp, cancellationToken);
        await _cache.RemoveAsync(phoneNumber, cancellationToken);
        return secret;
    }
}

internal sealed record GetSendOtpResponse
{
    [JsonPropertyName("_id")] public string Id { get; init; }

    public int Ttl { get; set; }
}

internal sealed record GetVerifyOtpResponse
{
    public bool IsValid { get; set; }
}
