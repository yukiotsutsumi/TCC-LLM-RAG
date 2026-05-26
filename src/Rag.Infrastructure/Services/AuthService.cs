using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Domain.Models;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Rag.Infrastructure.Services;

public class AuthService(
    UserManager<User> userManager,
    IRefreshTokenRepository refreshTokenRepo,
    IRevokedTokenRepository revokedTokenRepo,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await userManager.FindByNameAsync(request.Username) != null)
            return new AuthResponse(false, "Username já existe.");

        if (await userManager.FindByEmailAsync(request.Email) != null)
            return new AuthResponse(false, "Email já cadastrado.");

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

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return new AuthResponse(false, $"Usuário criado, mas houve erro ao vincular a role padrão: {errors}");
        }

        return new AuthResponse(true, "Usuário registrado com sucesso.");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var user = await userManager.FindByNameAsync(request.Username);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            return new AuthResponse(false, "Credenciais inválidas.");

        var familyId = Guid.NewGuid();

        return await GenerateFullAuthResponseAsync(user, familyId, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return new AuthResponse(false, "Refresh token ausente.");

        var stored = await refreshTokenRepo.GetByTokenAsync(refreshToken);

        if (stored == null)
            return new AuthResponse(false, "Refresh token inválido.");

        // ⚠️ REUSE DETECTION
        // Token já foi revogado → alguém está tentando reutilizar
        // Invalida TODA a família de tokens (possível roubo de token)
        if (stored.IsRevoked)
        {
            await refreshTokenRepo.RevokeAllByFamilyAsync(
                stored.FamilyId,
                "Reuse detection — possível roubo de token");
            await refreshTokenRepo.SaveChangesAsync();
            return new AuthResponse(false, "Refresh token comprometido. Faça login novamente.");
        }

        if (stored.IsExpired)
            return new AuthResponse(false, "Refresh token expirado.");

        stored.RevokedAt = DateTime.UtcNow;

        var response = await GenerateFullAuthResponseAsync(stored.User, stored.FamilyId, ipAddress);

        if (response.Data != null)
            stored.ReplacedByToken = response.Data.RefreshToken;

        await refreshTokenRepo.SaveChangesAsync();

        return response;
    }

    public async Task<AuthResponse> LogoutAsync(Guid userId, string? jti = null)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return new AuthResponse(false, "Usuário não encontrado.");

        await refreshTokenRepo.RevokeAllByUserAsync(userId);
        await refreshTokenRepo.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(jti))
        {
            var minutes = double.Parse(configuration["Jwt:ExpirationMinutes"] ?? "30");

            var revokedToken = new RevokedToken
            {
                Jti = jti,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(minutes),
                RevokedAt = DateTime.UtcNow
            };

            await revokedTokenRepo.AddAsync(revokedToken);

            await revokedTokenRepo.PurgeExpiredAsync();
            await revokedTokenRepo.SaveChangesAsync();
        }

        return new AuthResponse(true, "Logout realizado.");
    }

    private async Task<AuthResponse> GenerateFullAuthResponseAsync(
        User user,
        Guid familyId,
        string? ipAddress)
    {
        var (accessToken, jti, accessExpiresAt) = await GenerateJwtAsync(user);

        var refreshDays = int.Parse(configuration["Jwt:RefreshTokenDays"] ?? "7");

        var newRefreshToken = new RefreshToken
        {
            Token = GenerateRefreshTokenString(),
            UserId = user.Id,
            FamilyId = familyId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedByIp = ipAddress
        };

        await refreshTokenRepo.AddAsync(newRefreshToken);
        await refreshTokenRepo.SaveChangesAsync();

        var data = new AuthSuccessResponse(
            accessToken,
            accessExpiresAt,
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt
        );

        return new AuthResponse(true, "Sucesso", data);
    }

    private async Task<(string token, string jti, DateTime expiresAtUtc)> GenerateJwtAsync(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var secretKey = jwt["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey missing");
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        var minutes = double.Parse(jwt["ExpirationMinutes"] ?? "30");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, jti),
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

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, expiresAt);
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}