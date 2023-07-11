using FastEndpoints;
using FluentValidation;

namespace OneGuard.Endpoints.Verify;

file sealed class Endpoint : Endpoint<Request, Response>
{
    private readonly IOtpService _otpService;
    private readonly ISecretService _secretService;

    public Endpoint(ISecretService secretService, IOtpService otpService)
    {
        _secretService = secretService;
        _otpService = otpService;
    }

    public override void Configure()
    {
        Post("otp/verify");
        AllowAnonymous();
        // Permissions("one_guard_verify_otp");
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var verifyOtp = await _otpService.VerifyAsync(request.PhoneNumber, request.Otp, ct);

        var response = new Response()
        {
            Secret = verifyOtp
        };
        await SendOkAsync(response, ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Verify otp in the system";
        Description = "Verify otp in the system";
        Response<Response>(200, "Verify was successfully sent");
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

        RuleFor(request => request.Otp)
            .NotEmpty().WithMessage("Add Otp")
            .NotNull().WithMessage("Add Otp");
    }
}

file sealed record Request
{
    public string PhoneNumber { get; set; } = default!;

    public string Otp { get; set; } = default!;
}

file sealed record Response
{
    public string Secret { get; set; } = default!;
}