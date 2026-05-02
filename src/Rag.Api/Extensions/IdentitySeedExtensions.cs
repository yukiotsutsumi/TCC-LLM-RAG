using Microsoft.AspNetCore.Identity;
using Rag.Core.Domain.Models;

namespace Rag.Api.Extensions;

public static class IdentitySeedExtensions
{
    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var roles = new[]
        {
            new Role
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrador do sistema"
            },
            new Role
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Usuário padrão"
            }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Erro ao criar role {role.Name}: {errors}");
                }
            }
        }

        const string adminEmail = "admin@local.test";
        const string adminPassword = "Admin123@";
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador Padrão",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao criar usuário admin: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Admin");
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao vincular admin à role Admin: {errors}");
            }
        }

        const string userEmail = "user@local.test";
        const string userPassword = "User123@";
        var user = await userManager.FindByEmailAsync(userEmail);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userEmail,
                Email = userEmail,
                FullName = "Usuário Padrão",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, userPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao criar usuário padrão: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, "User"))
        {
            var result = await userManager.AddToRoleAsync(user, "User");
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao vincular user à role User: {errors}");
            }
        }
    }
}