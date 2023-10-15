namespace OneGuard.Core.Services.Exceptions;

internal sealed class EndpointNotRegisteredException : CoreException
{
    private const int DefaultCode = 404;

    private const string DefaultMessage = "Endpoint Has not registered";

    public EndpointNotRegisteredException() : base(DefaultCode, DefaultMessage)
    {
    }
}