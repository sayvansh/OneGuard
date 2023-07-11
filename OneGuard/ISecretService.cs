namespace OneGuard;

public interface ISecretService
{
    Task<string> GenerateAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(string secret, CancellationToken cancellationToken = default);
}