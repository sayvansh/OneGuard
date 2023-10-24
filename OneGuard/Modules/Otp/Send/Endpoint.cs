using FastEndpoints;
using FluentValidation;
using OneGuard.Core;

namespace OneGuard.Modules.Otp.Send;

file sealed class Endpoint : Endpoint<Request,OtpResponse>
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
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var response = await _otpService.SendAsync(request.EndpointId, request.PhoneNumber, ct);
        await SendOkAsync(response,ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Send otp in the system";
        Description = "Send otp in the system";
        Response<OtpResponse>(200, "Otp was successfully sent");
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
        
        RuleFor(request => request.EndpointId)
            .NotEmpty().WithMessage("Enter Valid EndpointId")
            .NotNull().WithMessage("Enter EndpointId");
    }
}

file sealed record Request
{
    public string PhoneNumber { get; set; } = default!;

    public Guid EndpointId { get; set; } = default!;
}