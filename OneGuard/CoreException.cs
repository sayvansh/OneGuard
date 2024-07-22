namespace OneGuard;

public class CoreException : ApplicationException
{
    public CoreException(int code,string? message, string clientMessage) : base(message)
    {
        Code = code;
        ClientMessage = clientMessage;
    }
    
    public int Code { get; set; }
    
    public string ClientMessage { get; set; }
}