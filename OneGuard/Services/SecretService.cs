using Core.Hashing;
using Microsoft.Extensions.Caching.Distributed;
using OneGuard.Exceptions;

namespace OneGuard.Services;

internal sealed class SecretService : ISecretService
{
    private readonly IDistributedCache _cache;
    private readonly IHashService _hashService;


    public SecretService(IDistributedCache cache, IHashService hashService)
    {
        _cache = cache;
        _hashService = hashService;
    }

    public async Task<string> GenerateAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {
        var secret = _hashService.Hash(phoneNumber, otp);
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
        await _cache.SetStringAsync(secret, phoneNumber, options, cancellationToken);
        return secret;
    }

    public async Task VerifyAsync(string secret, CancellationToken cancellationToken = default)
    {
        if (await _cache.GetStringAsync(secret, cancellationToken) is null) throw new SecretNotVerifiedException();
        await _cache.RemoveAsync(secret, cancellationToken);
    }
}