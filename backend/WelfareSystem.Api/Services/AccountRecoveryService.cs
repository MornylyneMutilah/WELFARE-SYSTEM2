using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;
namespace WelfareSystem.Api.Services;

public interface IAccountMail { Task Send(string email, string purpose, string link); string Mode { get; } }
public sealed class AccountMail(IConfiguration config, IHostEnvironment environment, ILogger<AccountMail> logger) : BackgroundService, IAccountMail {
    public string Mode => !string.IsNullOrWhiteSpace(config["Mail:Host"]) ? "SMTP" : environment.IsDevelopment() ? "Development outbox" : "Unavailable";
    public async Task Send(string email, string purpose, string link) {
        if (Mode == "Unavailable") throw new InvalidOperationException("Configure the server mail service.");
        var body = $"Welfare account {purpose}\n\nOpen this link to continue:\n{link}\n\nIf you did not request this, ignore this message. Never share this link.";
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", Mode == "Development outbox" ? "MailOutbox" : "MailPending"); Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".eml");
        await File.WriteAllTextAsync(destination + ".tmp", $"To: {new MailAddress(email).Address}\nSubject: Welfare account {purpose}\n\n{body}"); File.Move(destination + ".tmp", destination);
    }
    protected override async Task ExecuteAsync(CancellationToken ct) {
        if (Mode != "SMTP") return;
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "MailPending"); Directory.CreateDirectory(folder);
        while (!ct.IsCancellationRequested) {
            foreach (var path in Directory.EnumerateFiles(folder, "*.eml")) {
                try {
                    var content = await File.ReadAllTextAsync(path, ct); var lines = content.Split('\n');
                    using var smtp = new SmtpClient(config["Mail:Host"], config.GetValue("Mail:Port", 587)) { EnableSsl = true, Timeout = 15000, Credentials = new NetworkCredential(config["Mail:Username"], config["Mail:Password"]) };
                    using var message = new MailMessage(config["Mail:From"] ?? throw new InvalidOperationException("Configure Mail:From."), lines[0][4..].Trim(), lines[1][9..].Trim(), string.Join('\n', lines.Skip(3)));
                    await smtp.SendMailAsync(message, ct); File.Delete(path);
                } catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch (Exception error) { logger.LogWarning("Queued account email remains undelivered ({FailureType}); check mail configuration.", error.GetType().Name); }
            }
            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch (OperationCanceledException) { return; }
        }
    }
}
public sealed class AccountRecoveryService(AppDbContext db, IPasswordHasher hasher, IAccountMail mail, ExternalAuthSettings settings, TimeProvider clock) {
    public async Task Issue(string email, string purpose) {
        if (purpose is not ("reset" or "verify" or "invite")) throw new ArgumentException("Invalid account action.");
        if (mail.Mode == "Unavailable") throw new InvalidOperationException("Account email delivery is unavailable.");
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        var user = await db.Users.FirstOrDefaultAsync(u => u.IsActive && u.Email.ToLower() == email.Trim().ToLower());
        if (user == null || (purpose == "invite" && !user.MustChangePassword) || (purpose == "verify" && user.EmailVerified)) return;
        var now = clock.GetUtcNow().UtcDateTime;
        if (await db.AccountActionTokens.CountAsync(t => t.UserId == user.Id && t.CreatedAt > now.AddMinutes(-15)) >= 3) return;
        var token = ExternalAuthFlowStore.RandomToken();
        var row = new AccountActionToken { TokenHash = ExternalAuthFlowStore.Hash(token), UserId = user.Id, Purpose = purpose, Email = user.Email, TokenVersion = user.TokenVersion, CreatedAt = now, ExpiresAt = now.AddMinutes(purpose == "reset" ? 30 : 1440) };
        db.AccountActionTokens.Add(row); await db.SaveChangesAsync();
        try { await mail.Send(user.Email, purpose, $"{settings.FrontendBase}/account/action?purpose={purpose}&token={Uri.EscapeDataString(token)}"); if (tx != null) await tx.CommitAsync(); }
        catch { row.ConsumedAt = now; await db.SaveChangesAsync(); throw; }
    }
    public async Task<bool> Complete(string token, string purpose, string? password) {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128 || purpose is not ("reset" or "verify" or "invite")) return false;
        if (purpose != "verify" && !PasswordValid(password)) return false;
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        var hash = ExternalAuthFlowStore.Hash(token); var row = await db.AccountActionTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (row == null) return false; await db.Entry(row).ReloadAsync(); var now = clock.GetUtcNow().UtcDateTime;
        if (row.Purpose != purpose || row.ConsumedAt != null || row.ExpiresAt <= now) return false;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == row.UserId); if (user == null) return false; await db.Entry(user).ReloadAsync();
        if (!user.IsActive || user.Email != row.Email || user.TokenVersion != row.TokenVersion) return false;
        if (purpose != "verify") { user.PasswordHash = hasher.HashPassword(password!); user.MustChangePassword = false; }
        user.EmailVerified = true; user.TokenVersion++; user.UpdatedAt = now; row.ConsumedAt = now;
        db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = purpose == "verify" ? "EmailVerified" : purpose == "invite" ? "InvitationAccepted" : "PasswordReset", EntityType = "User", EntityId = user.Id.ToString(), Details = "{\"sessionsRevoked\":true}" });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return true;
    }
    public static bool PasswordValid(string? value) => value is { Length: >= 12 and <= 128 } && value.Any(char.IsLetter) && value.Any(char.IsDigit);
    public async Task<bool> ChangePassword(int id, string current, string next) {
        if (!PasswordValid(next) || next == current) return false;
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive); if (user == null) return false; await db.Entry(user).ReloadAsync();
        if (!hasher.VerifyPassword(current, user.PasswordHash)) return false;
        user.PasswordHash = hasher.HashPassword(next); user.MustChangePassword = false; user.TokenVersion++; user.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        db.AuditLogs.Add(new AuditLog { UserId = id, Action = "PasswordChanged", EntityType = "User", EntityId = id.ToString(), Details = "{\"sessionsRevoked\":true}" });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return true;
    }
}
