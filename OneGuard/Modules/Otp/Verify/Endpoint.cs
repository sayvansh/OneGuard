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
        Post("otp/{requestId}/verify");
        AllowAnonymous();
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var verifyOtp = await _otpService.VerifyAsync(request.RequestId, request.Otp, ct);
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
        RuleFor(request => request.RequestId)
            .NotEmpty().WithMessage("Enter Valid RequestId")
            .NotNull().WithMessage("Enter RequestId");

        RuleFor(request => request.Otp)
            .NotEmpty().WithMessage("Enter Valid Otp")
            .NotNull().WithMessage("Enter Otp");
    }
}

file sealed record Request
{
    public string RequestId { get; set; } = default!;

    public string Otp { get; set; } = default!;
}