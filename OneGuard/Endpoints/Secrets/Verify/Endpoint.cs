using FastEndpoints;
using FluentValidation;

namespace OneGuard.Endpoints.Secrets.Verify;

file sealed class Endpoint : Endpoint<Request>
{
    private readonly ISecretService _secretService;

    public Endpoint(ISecretService secretService)
    {
        _secretService = secretService;
    }

    public override void Configure()
    {
        Post("otp/secrets/verify");
        AllowAnonymous();
        // Permissions("one_guard_secrets_verify");
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        await _secretService.VerifyAsync(request.Secret, request.PhoneNumber, request.Endpoint, ct);
        await SendOkAsync(ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Verify secret in the system";
        Description = "Verify secret in the system";
        Response(200, "Secret was successfully verified");
    }
}

file sealed class RequestValidator : Validator<Request>
{
    public RequestValidator()
    {
        RuleFor(request => request.Secret)
            .NotEmpty().WithMessage("Add Secret")
            .NotNull().WithMessage("Add Secret");
    }
}

file sealed record Request
{
    public string Secret { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public Guid Endpoint { get; set; } = default!;
}