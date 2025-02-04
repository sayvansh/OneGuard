using FastEndpoints;
using FluentValidation;
using OneGuard.Core;

namespace OneGuard.Modules.Otp.Secrets.Verify.V2;

file sealed class Endpoint : Endpoint<Request, Response>
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
        Version(2);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var response = await _secretService.VerifyAsync(request.Secret, request.PhoneNumber, request.EndpointId, ct);
        await SendOkAsync(new Response
        {
            Otp = response.Otp,
        }, ct);
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
            .NotEmpty().WithMessage("Enter Valid Secret")
            .NotNull().WithMessage("Enter Secret");

        RuleFor(request => request.PhoneNumber)
            .NotEmpty().WithMessage("Enter Valid PhoneNumber")
            .NotNull().WithMessage("Enter PhoneNumber");

        RuleFor(request => request.EndpointId)
            .NotEmpty().WithMessage("Enter Valid EndpointId")
            .NotNull().WithMessage("Enter EndpointId");
    }
}

file sealed record Request
{
    public string Secret { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public Guid EndpointId { get; set; } = default!;
}

file sealed record Response
{
    public string Otp { get; set; } = default!;
}