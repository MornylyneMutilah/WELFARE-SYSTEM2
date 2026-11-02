using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;

namespace WelfareSystem.Api.Services;

public static class AccountTokenValidator
{
    public static async Task<bool> IsCurrentAsync(ClaimsPrincipal? principal, AppDbContext context)
    {
        if (principal == null || principal.FindFirstValue("type") == "refresh" ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return false;
        var versionText = principal.FindFirstValue("ver");
        if (versionText != null && !int.TryParse(versionText, out _)) return false;
        var version = versionText == null ? 0 : int.Parse(versionText);
        return await context.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive && u.TokenVersion == version &&
            u.Role.Name == principal.FindFirstValue(ClaimTypes.Role));
    }
}
