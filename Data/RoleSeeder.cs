using Microsoft.AspNetCore.Identity;

namespace BloodDonationManagement.Data;

public static class RoleSeeder
{
    private const string AdminEmail    = "susam.gbit@gmail.com";
    private const string AdminPhone    = "01714496737";
    private const string DefaultPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure roles exist
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Find or create admin account
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName     = AdminEmail,
                Email        = AdminEmail,
                EmailConfirmed = true,
                PhoneNumber  = AdminPhone
            };
            var result = await userManager.CreateAsync(admin, DefaultPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create admin user: {errors}");
            }
        }

        // Ensure admin has Admin role
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
