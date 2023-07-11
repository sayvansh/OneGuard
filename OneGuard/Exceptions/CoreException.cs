namespace OneGuard.Exceptions;

public class CoreException : ApplicationException
{
    public int Code { get; set; }

    public CoreException(int code,string? message) : base(message)
    {
        Code = code;
    }
}