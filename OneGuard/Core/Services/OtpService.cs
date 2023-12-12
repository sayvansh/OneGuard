using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Core.Services.Exceptions;
using OneGuard.Infrastructure;

namespace OneGuard.Core.Services;

internal sealed class OtpService : IOtpService
{
    private readonly ISecretService _secretService;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOtpRequest _otpRequest;
    private const string ApiUrl = "notifications/send";

    public OtpService(ISecretService secretService, IDistributedCache cache, IHttpClientFactory clientFactory, ApplicationDbContext dbContext, IOtpRequest otpRequest)
    {
        _secretService = secretService;
        _cache = cache;
        _clientFactory = clientFactory;
        _dbContext = dbContext;
        _otpRequest = otpRequest;
    }

    public async Task<OtpResponse> SendAsync(string otpRequestId, CancellationToken cancellationToken = default)
    {
        var otpRequest = await _otpRequest.GetAsync(otpRequestId, cancellationToken);
        var otpModelValue = await _cache.GetStringAsync($"{otpRequestId}_SENT_OTP", token: cancellationToken);
        if (otpModelValue is not null)
        {
            var cachedOtpModel = JsonSerializer.Deserialize<OtpModel>(otpModelValue)!;
            return new OtpResponse
            {
                TtlInSeconds = (int)(cachedOtpModel.ExpiresAtUtc - DateTime.Now).TotalSeconds,
                ExpireAtUtc = cachedOtpModel.ExpiresAtUtc
            };
        }

        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == otpRequest.EndpointId, cancellationToken: cancellationToken);
        if (endpoint is null) throw new EndpointNotRegisteredException();

        var otp = Random.Shared.RandomNumber(endpoint.Length);

        string[] to = { otpRequest.PhoneNumber };
        var client = _clientFactory.CreateClient("Bellman");
        var sendOtpResponseMessage = await client.PostAsJsonAsync(ApiUrl, new
        {
            Content = otpRequest.MessageData is null ? string.Format(endpoint.Content, otp) : string.Format(endpoint.Content, otp, otpRequest.MessageData),
            To = to,
            Type = "sms",
        }, cancellationToken: cancellationToken);

        if (!sendOtpResponseMessage.IsSuccessStatusCode) throw new OtpFailedToSendException();

        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(endpoint.OtpTtl));
        var otpModel = new OtpModel
        {
            PhoneNumber = otpRequest.PhoneNumber,
            Code = otp,
            EndpointId = endpoint.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(endpoint.OtpTtl)
        };
        await _cache.SetStringAsync($"{otpRequestId}_SENT_OTP", JsonSerializer.Serialize(otpModel), options, cancellationToken);

        return new OtpResponse
        {
            TtlInSeconds = endpoint.OtpTtl,
            ExpireAtUtc = otpModel.ExpiresAtUtc
        };
    }


    public async Task<SecretResponse> VerifyAsync(string otpRequestId, string otp, CancellationToken cancellationToken = default)
    {
        var record = await _cache.GetStringAsync($"{otpRequestId}_SENT_OTP", token: cancellationToken);
        if (record is null) throw new InvalidOtpRequestIdException();
        var otpModel = JsonSerializer.Deserialize<OtpModel>(record);
        if (otpModel is null) throw new InvalidOtpRequestIdException();
        if (otpModel.Code != otp) throw new OtpVerificationFailedException();
        await _otpRequest.VerifyAsync(otpRequestId, cancellationToken);
        var secret = await _secretService.GenerateAsync(otpModel.PhoneNumber, otp, otpModel.EndpointId, cancellationToken);
        await _cache.RemoveAsync($"{otpRequestId}_SENT_OTP", cancellationToken);
        return secret;
    }

    private sealed record OtpModel
    {
        public string Code { get; set; } = default!;

        public Guid EndpointId { get; set; }

        public string PhoneNumber { get; set; } = default!;

        public DateTime ExpiresAtUtc { get; set; }
    }
}