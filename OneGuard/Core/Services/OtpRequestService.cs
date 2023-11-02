using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Core.Services.Exceptions;
using OneGuard.Infrastructure;

namespace OneGuard.Core.Services;

internal sealed class OtpRequestService : IOtpRequest
{
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHashService _hashService;


    public OtpRequestService(IDistributedCache cache, ApplicationDbContext dbContext, IHashService hashService)
    {
        _cache = cache;
        _dbContext = dbContext;
        _hashService = hashService;
    }

    public async Task<OtpRequest> RequestAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var otpRequest = await IsRequestedAsync(endpointId, phoneNumber, cancellationToken);
        if (otpRequest is not null) return otpRequest;

        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken: cancellationToken);
        if (endpoint is null) throw new EndpointNotRegisteredException();

        var expireAtUtc = DateTime.UtcNow.AddMinutes(10);
        var otpRequestId = _hashService.Hash(endpointId.ToString(), phoneNumber, Random.Shared.RandomCharsAndNumbers(6)).ToLower();
        otpRequest = new OtpRequest
        {
            OtpRequestId = otpRequestId,
            ExpireAtUtc = expireAtUtc,
            EndpointId = endpointId,
            PhoneNumber = phoneNumber
        };
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds((expireAtUtc - DateTime.UtcNow).TotalSeconds));
        await _cache.SetStringAsync(otpRequestId, JsonSerializer.Serialize(otpRequest), options, cancellationToken);
        await _cache.SetStringAsync($"{endpointId}.{phoneNumber}", otpRequestId, options, cancellationToken);

        return otpRequest;
    }

    public async Task<OtpRequest> GetAsync(string otpRequestId, CancellationToken cancellationToken = default)
    {
        var otpRequestValue = await _cache.GetStringAsync(otpRequestId, token: cancellationToken);
        if (otpRequestValue is null) throw new InvalidOtpRequestIdException();
        var otpRequest = JsonSerializer.Deserialize<OtpRequest>(otpRequestValue);
        if (otpRequest is null) throw new InvalidOtpRequestIdException();
        return otpRequest;
    }

    public async Task VerifyAsync(string otpRequestId, CancellationToken cancellationToken = default)
    {
        var otpRequest = await GetAsync(otpRequestId, cancellationToken);
        await _cache.RemoveAsync($"{otpRequest.EndpointId}.{otpRequest.PhoneNumber}", cancellationToken);
    }

    private async Task<OtpRequest?> IsRequestedAsync(Guid endpointId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var cachedOtpRequestId = await _cache.GetStringAsync($"{endpointId}.{phoneNumber}", token: cancellationToken);
        if (cachedOtpRequestId is null) return null;
        var cachedOtpRequestValue = await _cache.GetStringAsync(cachedOtpRequestId, token: cancellationToken);
        if (cachedOtpRequestValue is null) return null;
        return JsonSerializer.Deserialize<OtpRequest>(cachedOtpRequestValue);
    }
}