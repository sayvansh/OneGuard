using FastEndpoints;
using FluentValidation;

namespace OneGuard.Endpoints.Send;

file sealed class Endpoint : Endpoint<Request>
{
    private readonly IOtpService _otpService;

    public Endpoint(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public override void Configure()
    {
        Post("otp");
        AllowAnonymous();
        // Permissions("one_guard_send_otp");
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        await _otpService.SendAsync(request.EndpointId, request.PhoneNumber, ct);
        await SendOkAsync(ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Send otp in the system";
        Description = "Send otp in the system";
        Response(200, "Otp was successfully sent");
    }
}

file sealed class RequestValidator : Validator<Request>
{
    public RequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty().WithMessage("Add PhoneNumber")
            .NotNull().WithMessage("Add PhoneNumber")
            .MinimumLength(10)
            .MaximumLength(14);
    }
}

file sealed record Request
{
    public string PhoneNumber { get; set; } = default!;

    public Guid EndpointId { get; set; } = default!;
}