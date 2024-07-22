namespace OneGuard.Core.Services.Exceptions;

internal sealed class OtpFailedToSendException : CoreException
{
    private const int DefaultCode = 400;
    private const string DefaultMessage = "Failed to send otp";
    private const string DefaultClientMessage = "خطا در ارسال رمز یکبار مصرف";


    public OtpFailedToSendException() : base(DefaultCode, DefaultMessage, DefaultClientMessage)
    {
    }
}