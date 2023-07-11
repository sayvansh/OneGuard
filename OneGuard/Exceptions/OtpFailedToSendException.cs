namespace OneGuard.Exceptions;

internal sealed class OtpFailedToSendException : CoreException
{
    private const int DefaultCode = 400;

    private const string DefaultMessage = "Failed to send otp";

    public OtpFailedToSendException() : base(DefaultCode, DefaultMessage)
    {
    }
}