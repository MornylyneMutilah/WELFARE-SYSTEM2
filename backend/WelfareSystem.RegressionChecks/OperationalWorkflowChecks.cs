using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using WelfareSystem.Api.Controllers;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Models.DTOs.Document;
using WelfareSystem.Api.Services;

static class OperationalWorkflowChecks {
    static ControllerContext Actor(User user) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, "Administrator") }, "Regression")) } };
    public static async Task Run(AppDbContext db, User employee, User other, User hr, User committee, User manager, User admin, WelfareApplicationService apps, ReviewService reviews, Action<bool, string> check) {
        var hasher = new PasswordHasher(); var clock = new TestClock(); var mail = new FakeMail();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Frontend:BaseUrl"] = "http://localhost:3000", ["Authentication:BackendBaseUrl"] = "http://localhost:5249" }).Build();
        var settings = new ExternalAuthSettings(config, new TestEnvironment()); var recovery = new AccountRecoveryService(db, hasher, mail, settings, clock);
        async Task<User> MakeAccount(string suffix, bool initial = false) {
            var key = Guid.NewGuid().ToString("N"); var user = new User { Email = $"operational-{suffix}-{key}@example.invalid", EmployeeId = key, FirstName = "Operational", LastName = suffix, DepartmentId = employee.DepartmentId, RoleId = employee.RoleId, PasswordHash = hasher.HashPassword("InitialPassword123!"), HireDate = DateTime.UtcNow.AddYears(-3), EmploymentVerifiedAt = DateTime.UtcNow, IsActive = true, MustChangePassword = initial };
            db.Users.Add(user); await db.SaveChangesAsync(); return user;
        }
        string Token() => QueryHelpers.ParseQuery(new Uri(mail.Messages.Last().Link).Query)["token"].ToString();
        var account = await MakeAccount("reset");
        await recovery.Issue(account.Email, "reset"); var reset = Token();
        check(await db.AccountActionTokens.AnyAsync(t => t.UserId == account.Id && t.TokenHash == ExternalAuthFlowStore.Hash(reset)) && !await db.AccountActionTokens.AnyAsync(t => t.TokenHash == reset), "Account reset tokens are stored as hashes, never plaintext");
        check(!await recovery.Complete(reset, "verify", null), "Reset tokens cannot be reused for another account action");
        check(!await recovery.Complete(reset, "reset", "short"), "Account recovery enforces new password requirements");
        var oldVersion = account.TokenVersion;
        check(await recovery.Complete(reset, "reset", "ReplacementPassword123!"), "A valid reset link replaces the password");
        check(account.TokenVersion == oldVersion + 1 && account.EmailVerified && hasher.VerifyPassword("ReplacementPassword123!", account.PasswordHash), "Reset revokes old sessions and records email ownership");
        check(!await recovery.Complete(reset, "reset", "AnotherPassword123!"), "Account action links are single-use");
        var beforeUnknown = mail.Messages.Count; await recovery.Issue("not-an-account@example.invalid", "reset");
        check(mail.Messages.Count == beforeUnknown, "Unknown recovery addresses do not create accounts or email links");
        var expiry = await MakeAccount("expiry"); await recovery.Issue(expiry.Email, "verify"); var expired = Token(); clock.Advance(TimeSpan.FromHours(25));
        check(!await recovery.Complete(expired, "verify", null), "Email verification links expire");
        await recovery.Issue(expiry.Email, "verify"); var fresh = Token(); check(await recovery.Complete(fresh, "verify", null) && expiry.EmailVerified, "A fresh verification link verifies the bound email");
        var binding = await MakeAccount("binding"); await recovery.Issue(binding.Email, "reset"); var bound = Token(); binding.Email = "changed-" + binding.Email; await db.SaveChangesAsync();
        check(!await recovery.Complete(bound, "reset", "ReplacementPassword123!"), "Recovery links cannot act on a changed email address");
        var suspended = await MakeAccount("suspended"); await recovery.Issue(suspended.Email, "reset"); var suspendedLink = Token(); suspended.IsActive = false; await db.SaveChangesAsync();
        check(!await recovery.Complete(suspendedLink, "reset", "ReplacementPassword123!"), "Suspension invalidates an outstanding recovery link");
        var initial = await MakeAccount("invitation", true); await recovery.Issue(initial.Email, "invite"); var invitation = Token();
        check(await recovery.Complete(invitation, "invite", "PrivateChosenPassword123!") && !initial.MustChangePassword, "Invited users choose a private password and clear the initial-password requirement");
        var existingCount = mail.Messages.Count; await recovery.Issue(account.Email, "invite"); check(mail.Messages.Count == existingCount, "Setup invitations do not replace established account passwords");
        var change = await MakeAccount("change", true);
        check(!await recovery.ChangePassword(change.Id, "wrong", "NewPrivatePassword123!"), "Password changes require the existing password");
        check(await recovery.ChangePassword(change.Id, "InitialPassword123!", "NewPrivatePassword123!") && !change.MustChangePassword, "Initial password change is enforced and then cleared");
        var limited = await MakeAccount("rate"); var countBefore = mail.Messages.Count; for (var i = 0; i < 5; i++) await recovery.Issue(limited.Email, "reset");
        check(mail.Messages.Count == countBefore + 3, "Account email requests are capped per user as well as HTTP rate limits");
        check(!await db.AuditLogs.AnyAsync(a => a.EntityId == account.Id.ToString() && (a.Details.Contains(reset) || a.Details.Contains("ReplacementPassword123!"))), "Account audits never store passwords or action tokens");

        var program = new WelfareProgram { Name = "Mandatory and budgets " + Guid.NewGuid(), Description = "Test fixture", Category = "Regression", MaxAmount = 1000, MinServicePeriod = 6, RequiredDocuments = "[\"Supporting letter\"]", IsActive = true };
        db.WelfarePrograms.Add(program); await db.SaveChangesAsync();
        var owner = await MakeAccount("evidence"); var draft = (await apps.CreateApplicationAsync(owner.Id, new() { WelfareProgramId = program.Id, RequestedAmount = 100, Reason = "Mandatory fixture" }))!;
        var appEntity = await db.WelfareApplications.FirstAsync(a => a.Id == draft.Id);
        var issues = await SubmissionGuard.Check(db, appEntity); check(issues.Any(s => s.Contains("Supporting letter")), "A missing required document blocks submission");
        owner.EmploymentVerifiedAt = null; await db.SaveChangesAsync(); check((await SubmissionGuard.Check(db, appEntity)).Any(s => s.Contains("verify")), "Unverified employment dates block submission");
        owner.EmploymentVerifiedAt = DateTime.UtcNow; owner.HireDate = DateTime.UtcNow.AddMonths(-1); await db.SaveChangesAsync(); check((await SubmissionGuard.Check(db, appEntity)).Any(s => s.Contains("Service period")), "Insufficient completed service blocks submission");
        owner.HireDate = DateTime.UtcNow.AddYears(-3); appEntity.RequestedAmount = 1001; await db.SaveChangesAsync(); check((await SubmissionGuard.Check(db, appEntity)).Any(s => s.Contains("amount")), "Over-limit requested amounts block submission");
        appEntity.RequestedAmount = 100; await db.SaveChangesAsync();
        var root = Path.Combine(Path.GetTempPath(), "welfare-operational-" + Guid.NewGuid().ToString("N"));
        var storageConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Documents:StoragePath"] = root }).Build(); var storage = new PrivateDocumentStore(new TestEnvironment(), storageConfig);
        DocumentsController Docs(User actor) => new(db, storage, NullLogger<DocumentsController>.Instance) { ControllerContext = Actor(actor) };
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4\noperational fixture\n%%EOF");
        FormFile File() => new(new MemoryStream(pdf), 0, pdf.Length, "file", "support.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
        try {
            check(await Docs(owner).UploadDocument(draft.Id, File(), "Unknown category") is BadRequestObjectResult, "Browsers cannot invent document-requirement categories");
            var upload = (DocumentResponse)((CreatedAtActionResult)await Docs(owner).UploadDocument(draft.Id, File(), "Supporting letter")).Value!;
            check(upload.RequirementName == "Supporting letter" && (await SubmissionGuard.Check(db, appEntity)).Count == 0, "Categorized stored evidence satisfies the configured requirement");
            check((await apps.SubmitApplicationAsync(draft.Id))?.Status == 2, "A draft passing mandatory requirements submits successfully");
            var repeated = (await apps.CreateApplicationAsync(owner.Id, new() { WelfareProgramId = program.Id, RequestedAmount = 100, Reason = "Repeated evidence for investigation" }))!;
            await Docs(owner).UploadDocument(repeated.Id, File(), "Supporting letter");
            check((await apps.SubmitApplicationAsync(repeated.Id))?.Status == 2, "Duplicate and frequency concerns remain advisory after mandatory checks pass");
            check(await db.Anomalies.AnyAsync(a => a.ApplicationId == repeated.Id && a.AnomalyType == "RepeatedDocument"), "Identical document bytes across submitted applications create an investigation flag");
            var hrReview = (await reviews.CreateReviewAsync(draft.Id, hr.Id, "Employee", new() { Recommendation = "Approve", Comments = "Original HR notes" }))!;
            await reviews.CreateReviewAsync(draft.Id, committee.Id, "Employee", new() { Recommendation = "Approve", Comments = "Committee notes" });
            var rejection = (await reviews.CreateReviewAsync(draft.Id, manager.Id, "Employee", new() { Recommendation = "Reject", Comments = "Original final rejection" }))!;
            AmendmentsController Amend(User actor) => new(db, apps) { ControllerContext = Actor(actor) };
            var appealRequest = new AmendmentsController.ChangeRequest { ReviewId = rejection.Id, Kind = "Appeal", Reason = "Please reconsider this evidence" };
            check(await Amend(other).Create(appealRequest) is NotFoundResult, "Employees cannot appeal someone else's application");
            var appeal = (DecisionAmendment)((OkObjectResult)await Amend(owner).Create(appealRequest)).Value!;
            check(await Amend(owner).Create(appealRequest) is ConflictObjectResult, "Repeated pending appeals are rejected");
            check(await Amend(manager).Resolve(appeal.Id, new() { Approve = true, Reason = "Self decision should fail" }) is ConflictObjectResult, "The original decision maker cannot adjudicate their own appeal");
            check(await Amend(hr).Resolve(appeal.Id, new() { Approve = true, Reason = "Not an authorized adjudicator" }) is ForbidResult, "A spoofed admin claim does not grant appeal resolution rights");
            check(await Amend(admin).Resolve(appeal.Id, new() { Approve = true, Reason = "Independent reconsideration authorized" }) is OkObjectResult && appeal.ReconsiderationApplicationId.HasValue, "Independent appeal acceptance creates a reconsideration draft");
            check((await db.WelfareApplications.FirstAsync(a => a.Id == draft.Id)).Status == 9 && (await db.ApplicationReviews.FirstAsync(r => r.Id == rejection.Id)).Comments == "Original final rejection", "Accepted appeals preserve the original rejected application and decision");
            check(await db.WelfareApplications.AnyAsync(a => a.Id == appeal.ReconsiderationApplicationId && a.Status == 1 && a.UserId == owner.Id), "Reconsideration starts at draft and cannot bypass normal review");
            var correction = (DecisionAmendment)((OkObjectResult)await Amend(hr).Create(new() { ReviewId = hrReview.Id, Kind = "Correction", Reason = "Clarify the recorded review notes", ProposedNotes = "Additional factual clarification" })).Value!;
            check(await Amend(admin).Resolve(correction.Id, new() { Approve = true, Reason = "Independent addendum accepted" }) is OkObjectResult && (await db.ApplicationReviews.FirstAsync(r => r.Id == hrReview.Id)).Comments == "Original HR notes", "Approved correction addenda do not overwrite original review notes");
        } finally { if (Directory.Exists(root)) { foreach (var file in Directory.EnumerateFiles(root)) System.IO.File.Delete(file); Directory.Delete(root); } }

        BudgetsController Budget(User actor) => new(db) { ControllerContext = Actor(actor) };
        var proposal = new BudgetsController.BudgetRequest { ProgramId = program.Id, Year = DateTime.UtcNow.Year, Amount = 5000, Reason = "Regression annual budget" };
        check(await Budget(employee).Propose(proposal) is ForbidResult && await Budget(manager).Propose(proposal) is ForbidResult, "Only current administrators can propose budgets");
        var allocation = (ProgramBudget)((OkObjectResult)await Budget(admin).Propose(proposal)).Value!;
        check(allocation.AllocatedAmount == 0 && allocation.ProposalStatus == "Pending", "Proposed allocations are not treated as approved budgets");
        check(await Budget(admin).Decide(allocation.Id, new() { Approve = true, Reason = "Self approval" }) is ForbidResult, "Budget proposals require management approval");
        check(await Budget(manager).Decide(allocation.Id, new() { Approve = true, Reason = "Independent budget approval" }) is OkObjectResult && allocation.AllocatedAmount == 5000, "Management approves the recorded annual allocation");
        check(await Budget(manager).Decide(allocation.Id, new() { Approve = false, Reason = "Stale repeated decision" }) is ConflictObjectResult, "Finalized budget proposals cannot be decided twice");
        proposal.Amount = 1000; await Budget(admin).Propose(proposal); await Budget(manager).Decide(allocation.Id, new() { Approve = false, Reason = "Keep the existing approved allocation" });
        check(allocation.AllocatedAmount == 5000, "Rejected budget changes preserve the previously approved allocation");
        check(await Budget(employee).List() is ForbidResult, "Budget registers are restricted to authorized management and administrators");

        var normal = Enumerable.Range(0, 100).Select(i => 100d + i).ToArray();
        check(!AmountNoveltyModel.Evaluate(normal.Take(49), 10000).Ready, "Experimental model abstains when program history is insufficient");
        check(!AmountNoveltyModel.Evaluate(normal, 150).IsNovel, "Nearest-neighbour fixture model accepts an ordinary historical-range amount");
        check(AmountNoveltyModel.Evaluate(normal, 10000).IsNovel, "Nearest-neighbour fixture model detects a distant amount");
        check(AmountNoveltyModel.Evaluate(normal, 10000) == AmountNoveltyModel.Evaluate(normal, 10000), "Nearest-neighbour model evaluation is deterministic");
        var gate = new MaintenanceGate(); var lease = gate.TryWrite()!; var drain = gate.Set(true, CancellationToken.None);
        check(gate.TryWrite() == null, "Maintenance rejects new write requests immediately"); lease.Dispose();
        check(await drain, "Maintenance waits for in-flight writes to finish"); await gate.Set(false, CancellationToken.None); using var resumed = gate.TryWrite(); check(resumed != null, "Write access resumes after maintenance ends");
        await ScannerChecks(check);
    }
    private static async Task ScannerChecks(Action<bool, string> check) {
        async Task<bool> Scan(string response) {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var server = Task.Run(async () => { using var client = await listener.AcceptTcpClientAsync(); await using var stream = client.GetStream(); var command = new byte[10]; await stream.ReadExactlyAsync(command); var length = new byte[4]; while (true) { await stream.ReadExactlyAsync(length); var size = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(length); if (size == 0) break; var buffer = new byte[size]; await stream.ReadExactlyAsync(buffer); } await stream.WriteAsync(Encoding.ASCII.GetBytes(response + "\0")); });
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Documents:ScannerHost"] = "127.0.0.1", ["Documents:ScannerPort"] = port.ToString() }).Build();
            try { await new FileScanner(config, new TestEnvironment()).Check(Encoding.ASCII.GetBytes("fixture"), CancellationToken.None); await server; return true; }
            catch (InvalidDataException) { await server; return false; } finally { listener.Stop(); }
        }
        check(await Scan("stream: OK"), "Scanner integration accepts an explicit clean INSTREAM response");
        check(!await Scan("stream: Regression.Test FOUND"), "Scanner integration rejects a malware detection response");
        bool productionBlocked = false;
        try { await new FileScanner(new ConfigurationBuilder().Build(), new TestEnvironment { EnvironmentName = "Production" }).Check([], CancellationToken.None); } catch (IOException) { productionBlocked = true; }
        check(productionBlocked, "Production uploads fail closed when scanning is unconfigured");
    }
    private sealed class FakeMail : IAccountMail { public string Mode => "Test outbox"; public List<(string Email, string Purpose, string Link)> Messages { get; } = []; public Task Send(string email, string purpose, string link) { Messages.Add((email, purpose, link)); return Task.CompletedTask; } }
    private sealed class TestClock : TimeProvider { private DateTimeOffset current = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => current; public void Advance(TimeSpan value) => current += value; }
    private sealed class TestEnvironment : IHostEnvironment { public string EnvironmentName { get; set; } = "Development"; public string ApplicationName { get; set; } = "OperationsChecks"; public string ContentRootPath { get; set; } = Path.GetTempPath(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); }
}
