using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Core.Services.Exceptions;
using OneGuard.Infrastructure;

namespace OneGuard.Core.Services;

internal sealed class SecretService : ISecretService
{
    private readonly IDistributedCache _cache;
    private readonly IHashService _hashService;
    private readonly ApplicationDbContext _dbContext;


    public SecretService(IDistributedCache cache, IHashService hashService, ApplicationDbContext dbContext)
    {
        _cache = cache;
        _hashService = hashService;
        _dbContext = dbContext;
    }

    public async Task<SecretResponse> GenerateAsync(string phoneNumber, string otp, Guid endpointId, CancellationToken cancellationToken = default)
    {
        var endpoint = await _dbContext.Endpoints
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken: cancellationToken);
        if (endpoint is null)
        {
            throw new EndpointNotRegisteredException();
        }

        var secret = _hashService.Hash(phoneNumber, otp, endpointId.ToString());
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(endpoint.SecretTtl));
        await _cache.SetStringAsync(secret, $"{phoneNumber},{endpointId}", options, cancellationToken);
        return new SecretResponse
        {
            Secret = secret,
            ExpireAtUtc = DateTime.UtcNow.AddSeconds(endpoint.SecretTtl)
        };
    }

    public async Task VerifyAsync(string secret, string phoneNumber, Guid endpointId, CancellationToken cancellationToken = default)
    {
        var record = await _cache.GetStringAsync(secret, cancellationToken);
        if (record is null)
        {
            throw new SecretNotVerifiedException();
        }

        var cachedPhoneNumber = record.Split(",")[0];
        var cachedEndpointId = Guid.Parse(record.Split(",")[1]);

        if (cachedPhoneNumber != phoneNumber || !cachedEndpointId.Equals(endpointId))
        {
            throw new SecretNotVerifiedException();
        }

        await _cache.RemoveAsync(secret, cancellationToken);
    }
}