using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Domain.Models;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Rag.Infrastructure.Services;

public class AuthService(
    UserManager<User> userManager,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var byName = await userManager.FindByNameAsync(request.Username);
        if (byName != null) return new AuthResponse(false, "Username já existe.");

        var byEmail = await userManager.FindByEmailAsync(request.Email);
        if (byEmail != null) return new AuthResponse(false, "Email já cadastrado.");

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResponse(false, $"Erro ao criar usuário: {errors}");
        }

        return new AuthResponse(true, "Usuário registrado com sucesso.");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            return new AuthResponse(false, "Credenciais inválidas.");

        return await GenerateFullAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return new AuthResponse(false, "Refresh token ausente.");

        var user = userManager.Users.SingleOrDefault(u => u.RefreshToken == refreshToken);
        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            return new AuthResponse(false, "Refresh token inválido ou expirado.");

        return await GenerateFullAuthResponse(user);
    }

    private async Task<AuthResponse> GenerateFullAuthResponse(User user)
    {
        var (accessToken, accessExpiresAt) = await GenerateJwtAsync(user);

        var refreshToken = GenerateRefreshToken();
        var refreshDays = int.Parse(configuration["Jwt:RefreshTokenDays"] ?? "7");
        var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshDays);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshExpiresAt;
        await userManager.UpdateAsync(user);

        var data = new AuthSuccessResponse(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt
        );

        return new AuthResponse(true, "Sucesso", data);
    }

    public async Task<AuthResponse> LogoutAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return new AuthResponse(false, "Usuário não encontrado.");

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await userManager.UpdateAsync(user);

        return new AuthResponse(true, "Logout realizado.");
    }

    private async Task<(string token, DateTime expiresAtUtc)> GenerateJwtAsync(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var secretKey = jwt["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey missing");
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        var minutes = double.Parse(jwt["ExpirationMinutes"] ?? "10");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName ?? "")
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenStr, expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}