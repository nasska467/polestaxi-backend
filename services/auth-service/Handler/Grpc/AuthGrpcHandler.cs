using Grpc.Core;
using PolesTaxi.AuthService.Service;
using PolesTaxi.Contracts.Auth.V1;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AuthService.Handler.Grpc;

public sealed class AuthGrpcHandler : PolesTaxi.Contracts.Auth.V1.AuthService.AuthServiceBase
{
    private readonly IAuthAppService _service;

    public AuthGrpcHandler(IAuthAppService service) => _service = service;

    public override async Task<TokenResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var pair = await _service.Login(request.Login, request.Password, context.CancellationToken);
        return ToResponse(pair);
    }

    public override async Task<TokenResponse> Refresh(RefreshRequest request, ServerCallContext context)
    {
        var pair = await _service.Refresh(request.RefreshToken, context.CancellationToken);
        return ToResponse(pair);
    }

    public override async Task<LogoutResponse> Logout(LogoutRequest request, ServerCallContext context)
    {
        await _service.Logout(request.AccessToken, request.RefreshToken, context.CancellationToken);
        return new LogoutResponse { Success = true };
    }

    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        var principal = await _service.ValidateToken(request.AccessToken, context.CancellationToken);
        var response = new ValidateTokenResponse { Valid = true, UserId = principal.UserId };
        response.Roles.AddRange(principal.Roles.Select(r => r.ToWire())); // <-- ToWire теперь работает
        return response;
    }

    private static TokenResponse ToResponse(Entity.Service.TokenPair pair) => new()
    {
        AccessToken = pair.AccessToken,
        RefreshToken = pair.RefreshToken,
        AccessExpiresInSeconds = pair.AccessExpiresInSeconds,
        RefreshExpiresInSeconds = pair.RefreshExpiresInSeconds
    };
}