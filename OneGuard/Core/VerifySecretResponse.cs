namespace OneGuard.Core;

public sealed record VerifySecretResponse
{
    public string Otp { get; set; } = null!;
}