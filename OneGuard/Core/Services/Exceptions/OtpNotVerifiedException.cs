namespace OneGuard.Core.Services.Exceptions;

internal sealed class OtpVerificationFailedException : CoreException
{
    private const int DefaultCode = 400;
    private const string DefaultMessage = "Otp verification failed";
    private const string DefaultClientMessage = "رمز یکبار مصرف نادرست است";


    public OtpVerificationFailedException() : base(DefaultCode, DefaultMessage, DefaultClientMessage)
    {
    }
}