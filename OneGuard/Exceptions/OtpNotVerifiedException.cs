namespace OneGuard.Exceptions;

internal sealed class OtpNotVerifiedException : CoreException
{
    private const int DefaultCode = 400;

    private const string DefaultMessage = "Otp not verified";

    public OtpNotVerifiedException() : base(DefaultCode, DefaultMessage)
    {
    }
}