using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using WelfareSystem.Api.Services.Rules;

static class EvidenceRecordsChecks
{
    static ControllerContext Actor(User user) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, "Administrator")
    }, "Regression")) } };
    static JsonElement Json(IActionResult result) => JsonSerializer.SerializeToElement(((OkObjectResult)result).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    public static async Task Run(AppDbContext db, User employee, User other, User hr, User committee, User manager, User admin,
        WelfareApplicationService applications, ReviewService reviews, Action<bool, string> check)
    {
        var root = Path.Combine(Path.GetTempPath(), "welfare-evidence-regression-" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Documents:StoragePath"] = root }).Build();
        var storage = new PrivateDocumentStore(new TestEnvironment(), config);
        DocumentsController Docs(User actor) => new(db, storage, NullLogger<DocumentsController>.Instance) { ControllerContext = Actor(actor) };
        RecordsController Records(User actor) => new(db) { ControllerContext = Actor(actor) };
        AuditLogsController Audit(User actor) => new(db) { ControllerContext = Actor(actor) };
        var program = new WelfareProgram { Name = "Evidence fixture " + Guid.NewGuid(), Category = "Evidence", Description = "Private test", RequiredDocuments = "[]", MinServicePeriod = 6, MaxAmount = 1000, IsActive = true };
        db.WelfarePrograms.Add(program); await db.SaveChangesAsync();
        var rule = new ServicePeriodRule();
        var exampleUser = new User { HireDate = new DateTime(2026, 1, 20) };
        var context = new RuleEvaluationContext { User = exampleUser, Program = new WelfareProgram { MinServicePeriod = 6 }, ApplicationDate = new DateTime(2026, 7, 19) };
        check(!rule.Evaluate(context).IsPassed, "Service eligibility requires completed months before the anniversary");
        context.ApplicationDate = new DateTime(2026, 7, 20);
        check(rule.Evaluate(context).IsPassed, "Service eligibility passes on the exact completed-month anniversary");
        exampleUser.HireDate = new DateTime(2024, 8, 31); context.Program.MinServicePeriod = 6; context.ApplicationDate = new DateTime(2025, 2, 28);
        check(rule.Evaluate(context).IsPassed, "End-of-month service anniversaries use calendar AddMonths semantics");
        check(storage.Resolve("../../outside.pdf") == null && storage.Resolve("uploads/legacy.pdf") == null, "Storage rejects traversal and legacy arbitrary paths");
        check(PrivateDocumentStore.ValidateContent(".pdf", Encoding.ASCII.GetBytes("%PDF-1.4\nfixture")) == "application/pdf", "PDF signature is validated independently of browser MIME type");
        bool rejected = false; try { PrivateDocumentStore.ValidateContent(".pdf", Encoding.ASCII.GetBytes("<script>malicious</script>")); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "A renamed HTML file cannot pass the PDF signature check");
        byte[] WordArchive(bool valid, bool macro) {
            using var buffer = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Create, true)) {
                foreach (var name in valid ? new[] { "[Content_Types].xml", "word/document.xml" } : new[] { "unrelated.txt" }) {
                    using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write("<fixture />");
                }
                if (macro) { using var writer = new StreamWriter(zip.CreateEntry("word/vbaProject.bin").Open()); writer.Write("rejected fixture"); }
            }
            return buffer.ToArray();
        }
        check(PrivateDocumentStore.ValidateContent(".docx", WordArchive(true, false)).Contains("wordprocessingml"), "DOCX validation recognizes required Word archive entries");
        bool InvalidWord(byte[] bytes) { try { PrivateDocumentStore.ValidateContent(".docx", bytes); return false; } catch (InvalidDataException) { return true; } }
        check(InvalidWord(WordArchive(false, false)), "A generic ZIP renamed to DOCX is rejected");
        check(InvalidWord(WordArchive(true, true)), "DOCX archives containing VBA projects are rejected");
        check(CsvExport.Cell(" \t=SUM(1,2)").StartsWith("\"'"), "CSV formula injection with leading whitespace is neutralized");
        check(CsvExport.Cell("line,\"quote\"\nnext") == "\"line,\"\"quote\"\"\nnext\"", "CSV quotes, separators and newlines are escaped");
        check(!AuditLogsController.Redact("{\"passwordHash\":\"sensitive\",\"nested\":{\"accessToken\":\"sensitive\"},\"reason\":\"kept\"}").Contains("sensitive"), "Audit API redacts nested credential fields");
        try {
            var draft = (await applications.CreateApplicationAsync(employee.Id, new() { WelfareProgramId = program.Id, RequestedAmount = 100, Reason = "Evidence fixture request" }))!;
            var foreignDraft = (await applications.CreateApplicationAsync(other.Id, new() { WelfareProgramId = program.Id, RequestedAmount = 100, Reason = "Foreign evidence fixture" }))!;
            var before = await applications.UpdateApplicationAsync(draft.Id, new() { RequestedAmount = 200, Reason = "Updated draft evidence" });
            check(before != null && before.RuleEvaluationResult.Contains("DraftUpdated"), "Draft edits recalculate and record eligibility");
            var pdf = Encoding.ASCII.GetBytes("%PDF-1.4\nRegression evidence only\n%%EOF");
            FormFile File(string name = "evidence.pdf", byte[]? data = null) { var bytes = data ?? pdf; return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "text/html" }; }
            check(await Docs(other).UploadDocument(draft.Id, File()) is NotFoundResult, "Employees cannot attach files to another employee's draft");
            check(await Docs(hr).UploadDocument(draft.Id, File()) is NotFoundResult, "HR cannot replace employee evidence even in its department");
            var upload = await Docs(employee).UploadDocument(draft.Id, File());
            var doc = (DocumentResponse)((CreatedAtActionResult)upload).Value!;
            check(doc.ContentAvailable && doc.FilePath == "" && doc.FileType == "application/pdf", "Upload persists private content without exposing a storage path");
            check(doc.ScanStatus == "Unscanned development", "Development uploads never claim a malware scan that did not run");
            var persisted = await db.ApplicationDocuments.AsNoTracking().SingleAsync(d => d.Id == doc.Id);
            check(storage.Exists(persisted.FilePath) && (await System.IO.File.ReadAllBytesAsync(storage.Resolve(persisted.FilePath)!)).SequenceEqual(pdf), "Stored file bytes exactly match the uploaded evidence");
            check(await Docs(other).Download(doc.Id) is NotFoundResult, "Employees cannot download another employee's evidence");
            check(await Docs(hr).GetDocumentsByApplicationId(foreignDraft.Id) is NotFoundResult, "HR cannot list documents in another department");
            var download = (FileStreamResult)await Docs(manager).Download(doc.Id);
            using (var copied = new MemoryStream()) { await download.FileStream.CopyToAsync(copied); await download.FileStream.DisposeAsync(); check(copied.ToArray().SequenceEqual(pdf), "Authorized reviewers receive the stored document bytes"); }
            check(await Docs(employee).UploadDocument(draft.Id, File("fake.pdf", Encoding.ASCII.GetBytes("fake"))) is BadRequestObjectResult, "Upload rejects file-extension and content mismatches");
            var oversized = new FormFile(new MemoryStream(pdf), 0, PrivateDocumentStore.MaximumSize + 1, "file", "too-large.pdf");
            check(await Docs(employee).UploadDocument(draft.Id, oversized) is BadRequestObjectResult, "Upload rejects files exceeding the size limit");
            var legacy = new ApplicationDocument { ApplicationId = draft.Id, FileName = "legacy.pdf", FileType = "application/pdf", FileSize = 100, FilePath = "uploads/legacy.pdf" };
            db.ApplicationDocuments.Add(legacy); await db.SaveChangesAsync();
            check(await Docs(employee).Download(legacy.Id) is NotFoundObjectResult, "Metadata-only legacy uploads return an honest content-unavailable response");
            check(await Docs(other).DeleteDocument(doc.Id) is NotFoundResult, "Other employees cannot remove draft evidence");
            check(await Docs(employee).DeleteDocument(legacy.Id) is OkObjectResult, "Owners can remove legacy metadata from their draft");

            // Change the source data after draft creation. Submission must use the new facts.
            program.MaxAmount = 150; employee.HireDate = DateTime.UtcNow.AddMonths(-1); await db.SaveChangesAsync();
            bool blocked = false; try { await applications.SubmitApplicationAsync(draft.Id); } catch (SubmissionValidationException) { blocked = true; }
            var checkedDraft = (await applications.GetApplicationByIdAsync(draft.Id))!;
            using var assessment = JsonDocument.Parse(checkedDraft.RuleEvaluationResult);
            check(blocked && checkedDraft.Status == 1 && !checkedDraft.IsEligible && assessment.RootElement.GetProperty("Inputs").GetProperty("MaxAmount").GetDecimal() == 150,
                "Submission reassesses current program limits and records the inputs used");
            var results = assessment.RootElement.GetProperty("RuleEngine").GetProperty("RuleResults").EnumerateArray().ToArray();
            check(results.Any(r => r.GetProperty("RuleName").GetString() == "Service Period" && !r.GetProperty("IsPassed").GetBoolean()), "Submission reassesses corrected hire dates");
            check(results.Where(r => r.GetProperty("RuleName").GetString() is "Application Frequency" or "Duplicate Application").All(r => r.GetProperty("IsPassed").GetBoolean()), "An application is excluded from its own frequency and duplicate assessment");
            check(await db.AuditLogs.CountAsync(a => a.EntityId == draft.Id.ToString() && a.Action == "EligibilityAssessed") == 3, "Creation, draft editing and blocked submission assessments are retained separately");
            program.MaxAmount = 1000; employee.HireDate = DateTime.UtcNow.AddYears(-3); employee.EmploymentVerifiedAt = DateTime.UtcNow; await db.SaveChangesAsync();
            var submitted = (await applications.SubmitApplicationAsync(draft.Id))!;
            check(await Docs(employee).UploadDocument(draft.Id, File()) is BadRequestObjectResult, "Submitted application evidence cannot be replaced");
            check(await Docs(employee).DeleteDocument(doc.Id) is BadRequestObjectResult, "Submitted application evidence cannot be deleted");
            check(await applications.UpdateApplicationAsync(draft.Id, new() { RequestedAmount = 1, Reason = "Should fail" }) == null, "Submitted application details cannot overwrite the saved assessment");

            var hrReview = (await reviews.CreateReviewAsync(draft.Id, hr.Id, "Administrator", new() { Recommendation = "Approve", Comments = "=SUM(1,2)" }))!;
            var committeeReview = (await reviews.CreateReviewAsync(draft.Id, committee.Id, "Employee", new() { Recommendation = "Approve", Comments = new string('C', 1800) }))!;
            var finalReview = (await reviews.CreateReviewAsync(draft.Id, manager.Id, "Employee", new() { Recommendation = "Approve", Comments = "Final confirmation" }))!;
            check(finalReview != null && committeeReview.Comments.Length == 1800, "Full review notes are retained without exceeding timeline-note storage limits");
            check(await reviews.UpdateReviewAsync(hrReview.Id, new() { Recommendation = "Approve", Comments = "Silently change the evidence" }) == null, "Recorded review notes cannot be silently overwritten");
            var hrRows = Json(await Records(hr).List(search: draft.Id.ToString()));
            check(hrRows.GetProperty("items").EnumerateArray().Any(r => r.GetProperty("id").GetInt32() == hrReview.Id), "HR decisions remain in the register after final management approval");
            check(hrRows.GetProperty("items").EnumerateArray().All(r => r.GetProperty("stage").GetString() == "HROfficer"), "HR register cannot expand to another stage using browser parameters");
            var committeeRows = Json(await Records(committee).List(search: draft.Id.ToString()));
            check(committeeRows.GetProperty("items").EnumerateArray().Any(r => r.GetProperty("id").GetInt32() == committeeReview.Id), "Committee endorsements remain in the register after final approval");
            check(Json(await Records(manager).List(search: draft.Id.ToString())).GetProperty("total").GetInt32() == 1, "Management can retrieve its finalized approval confirmation");
            check(Json(await Records(admin).List(search: draft.Id.ToString())).GetProperty("total").GetInt32() == 3, "Administrators can inspect all three recorded decision stages");
            check(await Records(employee).List() is ForbidResult, "A spoofed administrator claim does not grant employee access to the decision register");
            var receipt = Json(await Records(employee).Receipt(hrReview.Id));
            check(receipt.GetProperty("historicalSnapshotAvailable").GetBoolean() && receipt.GetProperty("snapshot").GetProperty("comments").GetString() == "=SUM(1,2)", "Employees can view confirmation of decisions on their own applications");
            check(receipt.GetProperty("snapshot").GetProperty("documents").GetArrayLength() == 1, "Decision snapshots retain the supporting-document manifest");
            var originalName = receipt.GetProperty("snapshot").GetProperty("employeeName").GetString();
            employee.FirstName = "Changed after decision"; program.Name = "Changed program after decision"; await db.SaveChangesAsync();
            var frozen = Json(await Records(admin).Receipt(hrReview.Id));
            check(frozen.GetProperty("snapshot").GetProperty("employeeName").GetString() == originalName && frozen.GetProperty("snapshot").GetProperty("programName").GetString() != program.Name,
                "Decision confirmations preserve names and program details from decision time");
            check(await Records(other).Receipt(hrReview.Id) is NotFoundResult && await Records(other).Timeline(draft.Id) is NotFoundResult, "Other employees cannot read confirmations or timelines");
            check(Json(await Records(employee).Timeline(draft.Id)).GetProperty("history").GetArrayLength() == 5, "Application timeline retains draft creation, submission and each of three review transitions");
            var csv = (FileContentResult)await Records(hr).Export(search: draft.Id.ToString());
            check(Encoding.UTF8.GetString(csv.FileContents).Contains("'" + "=SUM(1,2)"), "Decision register exports neutralize formula-like review notes");
            check(await Audit(hr).List() is ForbidResult && await Audit(employee).Export() is ForbidResult, "Audit browsing and export require the current database administrator role");
            check(Json(await Audit(admin).List(action: "ApplicationReview")).GetProperty("total").GetInt32() >= 3, "Administrator audit log exposes recorded application decisions");
            var ownCsv = (FileContentResult)await Records(other).ApplicationsCsv();
            check(!Encoding.UTF8.GetString(ownCsv.FileContents).Contains("Changed after decision") && !(await ApplicationAccess.Visible(db, other.Id)).Any(a => a.Id == draft.Id), "Application reports enforce employee ownership on the backend");
            var scopedReport = new ReportService(db, new HttpContextAccessor { HttpContext = Actor(hr).HttpContext });
            var byDepartment = await scopedReport.GetApplicationsByDepartmentReportAsync(null, null);
            check(byDepartment.DepartmentCounts.All(d => d.DepartmentId == hr.DepartmentId), "Existing aggregate report endpoints also enforce HR department scope");
            check(await db.AuditLogs.AnyAsync(a => a.Action == "DocumentDownloaded" && a.UserId == manager.Id), "Evidence downloads record the authenticated reviewer for audit");
        }
        finally {
            // Only this test's freshly created private folder is cleaned; no existing uploads are touched.
            if (Directory.Exists(root)) { foreach (var path in Directory.EnumerateFiles(root)) System.IO.File.Delete(path); Directory.Delete(root); }
        }
    }
    private sealed class TestEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = "Development"; public string ApplicationName { get; set; } = "EvidenceRegression";
        public string ContentRootPath { get; set; } = Path.GetTempPath(); public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
