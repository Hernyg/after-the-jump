using backend.Application.Dtos.Authentication;

using backend.Application.Interfaces;
using backend.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace backend.Infrastructure.Data;

public static class AuthDbSeeder
{
    /// <summary>
    /// Seed the authentication database with roles and default admin user.
    /// The method also applies any pending migrations to the database.
    /// </summary>
    public static async Task SeedAsync(
        AuthDbContext context,
        IServiceProvider serviceProvider,
        Serilog.ILogger logger)
    {
        // Apply migrations
        await context.Database.MigrateAsync();

        // Resolve required services
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager =
            serviceProvider.GetRequiredService<UserManager<AuthUser>>();
        var userService =
            serviceProvider.GetRequiredService<IUserService>();

        // Seed roles
        var roles = new[] { "player", "admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.Information("Created role: {Role}", role);
            }
        }

        // Register through the same service as a player so both databases get a user record.
        if (await userManager.FindByNameAsync("admin") is null)
        {
            await userService.RegisterAccount(new RegisterUserDto
            {
                Username = "admin",
                Password = "Admin123!"
            });

            var admin = await userManager.FindByNameAsync("admin")
                ?? throw new InvalidOperationException("Default admin account was not created.");
            var result = await userManager.AddToRoleAsync(admin, "admin");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Could not assign the default admin role.");
            }

            logger.Information("Created the default admin user");
        }
    }
}
