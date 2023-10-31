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
    private readonly IHashService _hashService;
    private const string ApiUrl = "notifications/send";


    public OtpService(ISecretService secretService, IDistributedCache cache, IHttpClientFactory clientFactory, ApplicationDbContext dbContext, IHashService hashService)
    {
        _secretService = secretService;
        _cache = cache;
        _clientFactory = clientFactory;
        _dbContext = dbContext;
        _hashService = hashService;
    }

    public async Task<OtpRequestResponse> RequestAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var alreadyRequestedResponse = await IsRequestedAsync(endpointId, phoneNumber, cancellationToken);
        if (alreadyRequestedResponse is not null) return alreadyRequestedResponse;

        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken: cancellationToken);
        if (endpoint is null) throw new EndpointNotRegisteredException();

        var expireAtUtc = DateTime.UtcNow.AddMinutes(10);
        var otpRequestId = _hashService.Hash(endpointId.ToString(), phoneNumber, Random.Shared.RandomCharsAndNumbers(6)).ToLower();
        var otpRequestResponse = new OtpRequestResponse
        {
            OtpRequestId = otpRequestId,
            ExpireAtUtc = expireAtUtc,
            EndpointId = endpointId,
            PhoneNumber = phoneNumber
        };
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds((expireAtUtc - DateTime.UtcNow).TotalSeconds));
        await _cache.SetStringAsync(otpRequestId, JsonSerializer.Serialize(otpRequestResponse), options, cancellationToken);
        await _cache.SetStringAsync($"{endpointId}.{phoneNumber}", otpRequestId, cancellationToken);
        return otpRequestResponse;
    }

    private async Task<OtpRequestResponse?> IsRequestedAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var cachedOtpRequestId = await _cache.GetStringAsync($"{endpointId}.{phoneNumber}", token: cancellationToken);
        if (cachedOtpRequestId is null) return null;
        var cachedOtpRequestValue = await _cache.GetStringAsync(cachedOtpRequestId, token: cancellationToken);
        if (cachedOtpRequestValue is null) return null;
        return JsonSerializer.Deserialize<OtpRequestResponse>(cachedOtpRequestValue);
    }

    public async Task<OtpResponse> SendAsync(string otpRequestId, CancellationToken cancellationToken = default)
    {
        var otpRequestValue = await _cache.GetStringAsync(otpRequestId, token: cancellationToken);
        if (otpRequestValue is null) throw new InvalidOtpRequestIdException();
        var otpRequestResponse = JsonSerializer.Deserialize<OtpRequestResponse>(otpRequestValue);
        if (otpRequestResponse is null) throw new InvalidOtpRequestIdException();

        var otpModelValue = await _cache.GetStringAsync($"{otpRequestId}_SENT_OTP", token: cancellationToken);
        if (otpModelValue is not null)
        {
            var cachedOtpModel = JsonSerializer.Deserialize<OtpModel>(otpModelValue);
            if (cachedOtpModel is not null)
            {
                return new OtpResponse
                {
                    TtlInSeconds = (int)(cachedOtpModel.ExpiresAtUtc - DateTime.Now).TotalSeconds,
                    ExpireAtUtc = cachedOtpModel.ExpiresAtUtc
                };
            }
        }

        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == otpRequestResponse.EndpointId, cancellationToken: cancellationToken);
        if (endpoint is null) throw new EndpointNotRegisteredException();

        var otp = Random.Shared.RandomNumber(endpoint.Length);
        var body = string.Format(endpoint.Content, otp);
        string[] to = { otpRequestResponse.PhoneNumber };
        var client = _clientFactory.CreateClient("Bellman");
        var sendOtpResponseMessage = await client.PostAsJsonAsync(ApiUrl, new
        {
            Content = body,
            To = to,
            Type = "sms",
            Provider = "persiafava",
        }, cancellationToken: cancellationToken);

        if (!sendOtpResponseMessage.IsSuccessStatusCode) throw new OtpFailedToSendException();

        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(endpoint.OtpTtl));
        var otpModel = new OtpModel
        {
            PhoneNumber = otpRequestResponse.PhoneNumber,
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
        if (record is null) throw new OtpVerificationFailedException();
        var otpModel = JsonSerializer.Deserialize<OtpModel>(record);
        if (otpModel is null) throw new InvalidOtpRequestIdException();
        if (otpModel.Code != otp) throw new OtpVerificationFailedException();
        var secret = await _secretService.GenerateAsync(otpModel.PhoneNumber, otp, otpModel.EndpointId, cancellationToken);
        await _cache.RemoveAsync($"{otpRequestId}_SENT_OTP", cancellationToken);
        await _cache.RemoveAsync($"{otpModel.EndpointId}.{otpModel.PhoneNumber}", cancellationToken);
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