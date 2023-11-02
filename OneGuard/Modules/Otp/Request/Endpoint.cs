using FastEndpoints;
using FluentValidation;
using OneGuard.Core;

namespace OneGuard.Modules.Otp.Request;

file sealed class Endpoint : Endpoint<Request, OtpRequest>
{
    private readonly IOtpRequest _otpRequest;

    public Endpoint(IOtpRequest otpRequest)
    {
        _otpRequest = otpRequest;
    }

    public override void Configure()
    {
        Post("otp/request");
        AllowAnonymous();
        Version(1);
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var response = await _otpRequest.RequestAsync(request.EndpointId, request.PhoneNumber, ct);
        await SendOkAsync(response, ct);
    }
}

file sealed class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Request otp";
        Description = "Request to generate otp request";
        Response<OtpRequest>(200, "Successful");
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