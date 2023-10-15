namespace OneGuard.Core.Services.Exceptions;

internal sealed class SecretNotVerifiedException : CoreException
{
    private const int DefaultCode = 400;

    private const string DefaultMessage = "Secret not verified";

    public SecretNotVerifiedException() : base(DefaultCode, DefaultMessage)
    {
    }
}