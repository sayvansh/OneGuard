using FastEndpoints;
using FluentValidation;
using OneGuard.Core;

namespace OneGuard.Modules.Otp.Verify;

file sealed class Endpoint : Endpoint<Request, SecretResponse>
{
    private readonly IOtpService _otpService;

    public Endpoint(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public override void Configure()
    {
        Post("otp/verify");
        AllowAnonymous();
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var verifyOtp = await _otpService.VerifyAsync(request.PhoneNumber, request.Otp, ct);

        await SendOkAsync(verifyOtp, ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Verify otp in the system";
        Description = "Verify otp in the system";
        Response<SecretResponse>(200, "Verify was successfully sent");
    }
}

file sealed class RequestValidator : Validator<Request>
{
    public RequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .NotEmpty().WithMessage("Enter Valid PhoneNumber")
            .NotNull().WithMessage("Enter PhoneNumber")
            .MinimumLength(10).WithMessage("Minimum Length of is 10")
            .MaximumLength(11).WithMessage("Maximum Length of is 11");

        RuleFor(request => request.Otp)
            .NotEmpty().WithMessage("Enter Valid Otp")
            .NotNull().WithMessage("Enter Otp");
    }
}

file sealed record Request
{
    public string PhoneNumber { get; set; } = default!;

    public string Otp { get; set; } = default!;
}