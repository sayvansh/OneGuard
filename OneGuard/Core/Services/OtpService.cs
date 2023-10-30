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
        return otpRequestResponse;
    }

    public async Task<OtpResponse> SendAsync(string otpRequestId, CancellationToken cancellationToken = default)
    {
        var otpRequestValue = await _cache.GetStringAsync(otpRequestId, token: cancellationToken);
        if (otpRequestValue is null) throw new InvalidOtpRequestIdException();
        var otpRequestResponse = JsonSerializer.Deserialize<OtpRequestResponse>(otpRequestValue);
        if (otpRequestResponse is null) throw new InvalidOtpRequestIdException();

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
        await _cache.SetStringAsync(otpRequestResponse.PhoneNumber, $"{otp},{endpoint.Id}", options, cancellationToken);

        return new OtpResponse
        {
            TtlInSeconds = endpoint.OtpTtl,
            ExpireAtUtc = DateTime.UtcNow.AddSeconds(endpoint.OtpTtl)
        };
    }


    public async Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {
        var record = await _cache.GetStringAsync(phoneNumber, token: cancellationToken);
        if (record is null) throw new OtpVerificationFailedException();
        var generatedOtp = record.Split(",")[0];
        var endpointId = Guid.Parse(record.Split(",")[1]);
        if (generatedOtp != otp) throw new OtpVerificationFailedException();
        var secret = await _secretService.GenerateAsync(phoneNumber, otp, endpointId, cancellationToken);
        await _cache.RemoveAsync(phoneNumber, cancellationToken);
        return secret;
    }
}