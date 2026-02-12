using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;
using Rag.Core.Domain.Models;
using Rag.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rag.Infrastructure.Services;

public class AuthService(
    UserManager<User> userManager,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var userExists = await userManager.FindByNameAsync(request.Username);
        if (userExists != null) return new AuthResponse(false, "Usuário já existe.");

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

        return new AuthResponse(true, "Usuário registrado com sucesso!");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null) return new AuthResponse(false, "Credenciais inválidas.");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) return new AuthResponse(false, "Credenciais inválidas.");

        var token = await GenerateJwtToken(user);
        return new AuthResponse(true, "Login realizado!", token);
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName ?? "")
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationMinutes"] ?? "60")),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}