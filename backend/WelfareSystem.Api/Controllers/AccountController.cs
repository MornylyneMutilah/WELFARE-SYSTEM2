using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Services;
namespace WelfareSystem.Api.Controllers;
[ApiController, Route("api/account")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController(AppDbContext db, AccountRecoveryService recovery, IAccountMail mail, ILogger<AccountController> logger) : ControllerBase {
    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<IActionResult> Send(string email, string purpose) {
        var minimum = Task.Delay(200);
        try { await recovery.Issue(email, purpose); await minimum; return Ok(new { message = "If an eligible account exists, an account email has been requested.", deliveryMode = mail.Mode }); }
        catch (Exception error) when (error is System.Net.Mail.SmtpException or IOException or InvalidOperationException) { logger.LogWarning("Account delivery unavailable ({FailureType})", error.GetType().Name); return StatusCode(503, new { message = "Account email delivery is unavailable. Contact your administrator." }); }
    }
    [HttpPost("forgot-password"), AllowAnonymous, EnableRateLimiting("account-actions")] public Task<IActionResult> Forgot(EmailRequest request) => Send(request.Email, "reset");
    [HttpPost("verify-email"), Authorize, EnableRateLimiting("account-actions")] public async Task<IActionResult> Verify() {
        var email = await db.Users.Where(u => u.Id == ActorId && u.IsActive).Select(u => u.Email).FirstOrDefaultAsync(); return email == null ? Unauthorized() : await Send(email, "verify");
    }
    [HttpPost("complete"), AllowAnonymous, EnableRateLimiting("account-actions")] public async Task<IActionResult> Complete(ActionRequest request) => await recovery.Complete(request.Token, request.Purpose, request.Password) ? Ok(new { message = "Account action completed. Sign in again; previous sessions have been revoked." }) : BadRequest(new { message = "This link is invalid, expired, already used, or the password does not meet the requirements." });
    [HttpPost("change-password"), Authorize, EnableRateLimiting("account-actions")] public async Task<IActionResult> Change(PasswordRequest request) => await recovery.ChangePassword(ActorId, request.CurrentPassword, request.NewPassword) ? Ok(new { message = "Password changed. Sign in again with your new password." }) : BadRequest(new { message = "Check your current password. Use a different password of 12–128 characters containing letters and numbers." });
    [HttpGet("security"), Authorize] public async Task<IActionResult> Security() => Ok(await db.Users.Where(u => u.Id == ActorId && u.IsActive).Select(u => new { u.EmailVerified, u.MustChangePassword, u.HireDate, u.EmploymentVerifiedAt, deliveryMode = mail.Mode }).FirstAsync());
    [HttpPost("invite/{id:int}"), Authorize(Policy = "AdminOnly"), EnableRateLimiting("account-actions")] public async Task<IActionResult> Invite(int id) {
        if (!await db.Users.AnyAsync(u => u.Id == ActorId && u.IsActive && u.Role.Name == "Administrator")) return Forbid();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive && u.MustChangePassword); if (user == null) return BadRequest(new { message = "Invitations are only for active accounts requiring an initial password change." });
        var result = await Send(user.Email, "invite"); if (result is OkObjectResult) { db.AuditLogs.Add(new() { UserId = ActorId, Action = "InvitationRequested", EntityType = "User", EntityId = id.ToString(), Details = "{}" }); await db.SaveChangesAsync(); } return result;
    }
    public class EmailRequest { [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = ""; }
    public class ActionRequest { [Required, MaxLength(128)] public string Token { get; set; } = ""; [Required, MaxLength(20)] public string Purpose { get; set; } = ""; [MaxLength(128)] public string? Password { get; set; } }
    public class PasswordRequest { [Required, MaxLength(1024)] public string CurrentPassword { get; set; } = ""; [Required, MaxLength(128)] public string NewPassword { get; set; } = ""; }
}
