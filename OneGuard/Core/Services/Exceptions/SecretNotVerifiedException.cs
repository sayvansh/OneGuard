namespace OneGuard.Core.Services.Exceptions;

internal sealed class SecretVerificationFailedException : CoreException
{
    private const int DefaultCode = 400;
    private const string DefaultMessage = "Secret not verified";
    private const string DefaultClientMessage = "شناسه یکتا نادرست است";


    public SecretVerificationFailedException() : base(DefaultCode, DefaultMessage, DefaultClientMessage)
    {
    }
}