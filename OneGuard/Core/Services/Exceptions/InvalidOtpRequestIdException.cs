namespace OneGuard.Core.Services.Exceptions;

internal sealed class InvalidOtpRequestIdException : CoreException
{
    private const int DefaultCode = 400;
    private const string DefaultMessage = "Invalid otp request id";
    private const string DefaultClientMessage = "درخواست ارسال رمز نامعتبر است";


    public InvalidOtpRequestIdException() : base(DefaultCode, DefaultMessage, DefaultClientMessage)
    {
    }
}