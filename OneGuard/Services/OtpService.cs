using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Exceptions;

namespace OneGuard.Services;

internal sealed class OtpService : IOtpService
{
    private readonly ISecretService _secretService;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ApplicationDbContext _dbContext;
    private const string ApiUrl = "notifications/send";


    public OtpService(ISecretService secretService, IDistributedCache cache, IHttpClientFactory clientFactory, ApplicationDbContext dbContext)
    {
        _secretService = secretService;
        _cache = cache;
        _clientFactory = clientFactory;
        _dbContext = dbContext;
    }

    public async Task SendAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken: cancellationToken);
        if (endpoint is null)
        {
            throw new EndpointNotRegisteredException();
        }

        var otp = GenerateRandomNumber(endpoint.Length);
        var body = endpoint.Content + "\n" + otp;
        string[] to = { phoneNumber };

        var client = _clientFactory.CreateClient("Bellman");
        var sendOtpResponseMessage = await client.PostAsJsonAsync(ApiUrl, new
        {
            Content = body,
            To = to,
            Type = "sms",
            Provider = "persiafava",
        }, cancellationToken: cancellationToken);

        if (!sendOtpResponseMessage.IsSuccessStatusCode)
        {
            throw new OtpFailedToSendException();
        }

        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(endpoint.OtpTtl));
        await _cache.SetStringAsync(phoneNumber, $"{otp},{endpoint.Id}", options, cancellationToken);
    }


    public async Task<SecretResponse> VerifyAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {
        var record = await _cache.GetStringAsync(phoneNumber, token: cancellationToken);

        if (record is null)
        {
            throw new OtpNotVerifiedException();
        }

        var generatedOtp = record.Split(",")[0];
        var endpointId = Guid.Parse(record.Split(",")[1]);

        if (generatedOtp != otp)
        {
            throw new OtpNotVerifiedException();
        }

        var secret = await _secretService.GenerateAsync(phoneNumber, otp, endpointId, cancellationToken);
        await _cache.RemoveAsync(phoneNumber, cancellationToken);
        return secret;
    }

    private static string GenerateRandomNumber(int length)
    {
        if (length <= 0)
        {
            throw new OtpNotVerifiedException();
        }

        var random = new Random();
        var digits = "0123456789".ToCharArray();

        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = digits[random.Next(0, digits.Length)];
        }

        return new string(result);
    }
}