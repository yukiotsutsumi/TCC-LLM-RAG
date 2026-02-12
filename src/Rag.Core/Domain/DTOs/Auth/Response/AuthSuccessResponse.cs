namespace Rag.Core.Domain.DTOs.Auth.Response
{
    public record AuthSuccessResponse(
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc
    );
}
