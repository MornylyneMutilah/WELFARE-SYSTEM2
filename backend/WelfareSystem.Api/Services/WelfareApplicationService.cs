using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.Application;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;
using WelfareSystem.Api.Services.Rules;
using WelfareSystem.Api.Services.AnomalyDetection;
using System.Text.Json;

namespace WelfareSystem.Api.Services;

public class WelfareApplicationService : IWelfareApplicationService
{
    private readonly IWelfareApplicationRepository _welfareApplicationRepository;
    private readonly IWelfareProgramRepository _welfareProgramRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRuleEngineService _ruleEngineService;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly NotificationTriggerService _notificationTriggerService;
    private readonly AppDbContext _context;

    public WelfareApplicationService(
        IWelfareApplicationRepository welfareApplicationRepository,
        IWelfareProgramRepository welfareProgramRepository,
        IUserRepository userRepository,
        IRuleEngineService ruleEngineService,
        IAnomalyDetectionService anomalyDetectionService,
        NotificationTriggerService notificationTriggerService,
        AppDbContext context)
    {
        _welfareApplicationRepository = welfareApplicationRepository;
        _welfareProgramRepository = welfareProgramRepository;
        _userRepository = userRepository;
        _ruleEngineService = ruleEngineService;
        _anomalyDetectionService = anomalyDetectionService;
        _notificationTriggerService = notificationTriggerService;
        _context = context;
    }

    public async Task<ApplicationResponse?> GetApplicationByIdAsync(int id)
    {
        var application = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (application == null)
        {
            return null;
        }

        return MapToResponse(application);
    }

    public async Task<IEnumerable<ApplicationResponse>> GetApplicationsByUserIdAsync(int userId)
    {
        var applications = await _welfareApplicationRepository.GetByUserIdAsync(userId);
        return applications.Select(MapToResponse);
    }

    public async Task<IEnumerable<ApplicationResponse>> GetAllApplicationsAsync()
    {
        var applications = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
        
        return applications.Select(MapToResponse);
    }

    public async Task<ApplicationResponse?> CreateApplicationAsync(int userId, CreateApplicationRequest request)
    {
        await using var transaction = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
        // Validate that the welfare program exists and is active
        var program = await _welfareProgramRepository.GetByIdAsync(request.WelfareProgramId);
        if (program == null || !program.IsActive)
        {
            return null;
        }

        // Get user with navigation properties
        var user = await _userRepository.GetByIdWithIncludesAsync(userId);
        
        if (user == null)
        {
            return null;
        }

        // Run rule engine evaluation
        var ruleContext = new RuleEvaluationContext
        {
            User = user,
            Program = program,
            RequestedAmount = request.RequestedAmount,
            ApplicationDate = DateTime.UtcNow,
            UserApplications = user.Applications?.ToList() ?? new List<WelfareApplication>()
        };

        var ruleEngineResult = _ruleEngineService.EvaluateEligibility(ruleContext);

        // Combine warning messages from rule engine
        var allWarnings = new List<string>();
        allWarnings.AddRange(ruleEngineResult.WarningMessages);

        var application = new WelfareApplication
        {
            UserId = userId,
            WelfareProgramId = request.WelfareProgramId,
            Status = 1, // Draft
            RequestedAmount = request.RequestedAmount,
            Reason = request.Reason,
            IsEligible = ruleEngineResult.IsEligible,
            AnomalyFlags = JsonSerializer.Serialize(allWarnings),
            RuleEvaluationResult = JsonSerializer.Serialize(new
            {
                RuleEngine = ruleEngineResult
            }),
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdApplication = await _welfareApplicationRepository.AddAsync(application);
        await Reassess(createdApplication, "DraftCreated");
        _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory { ApplicationId = createdApplication.Id, Status = 1, ChangedBy = userId, Notes = "Draft created" });
        await _context.SaveChangesAsync();
        
        // Fetch with navigation properties
        var applicationWithNav = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == createdApplication!.Id);
        
        if (transaction != null) await transaction.CommitAsync();
        return MapToResponse(applicationWithNav!);
    }

    public async Task<ApplicationResponse?> UpdateApplicationAsync(int id, UpdateApplicationRequest request)
    {
        await using var transaction = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"WelfareApplications\" WHERE \"Id\" = {id} FOR UPDATE");
        var application = await _welfareApplicationRepository.GetByIdAsync(id);
        if (application == null)
        {
            return null;
        }

        await _context.Entry(application).ReloadAsync();
        // Only allow updating draft applications
        if (application.Status != 1) // Draft
        {
            return null;
        }

        application.RequestedAmount = request.RequestedAmount;
        application.Reason = request.Reason;
        application.UpdatedAt = DateTime.UtcNow;

        await Reassess(application, "DraftUpdated");

        await _welfareApplicationRepository.UpdateAsync(application);
        
        // Fetch with navigation properties
        var applicationWithNav = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (transaction != null) await transaction.CommitAsync();
        return MapToResponse(applicationWithNav!);
    }

    public async Task<ApplicationResponse?> SubmitApplicationAsync(int id)
    {
        await using var transaction = _context.Database.CurrentTransaction == null
            ? await _context.Database.BeginTransactionAsync() : null;
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"WelfareApplications\" WHERE \"Id\" = {id} FOR UPDATE");
        var application = await _welfareApplicationRepository.GetByIdAsync(id);
        if (application == null)
        {
            return null;
        }

        await _context.Entry(application).ReloadAsync();
        var activeProgram = await _context.WelfarePrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == application.WelfareProgramId);
        if (activeProgram == null || !activeProgram.IsActive) return null;
        if (!await _context.Users.AnyAsync(u => u.Id == application.UserId && u.IsActive)) return null;

        // Only allow submitting draft applications
        if (application.Status != 1) // Draft
        {
            return null;
        }

        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542002)");
        await Reassess(application, "SubmissionCheck");
        var blocking = await SubmissionGuard.Check(_context, application);
        if (blocking.Count > 0) {
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            throw new SubmissionValidationException(blocking);
        }
        var submittedAt = DateTime.UtcNow;
        var changed = await _context.WelfareApplications.Where(a => a.Id == id && a.Status == 1)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, 2)
                .SetProperty(a => a.SubmittedAt, submittedAt).SetProperty(a => a.UpdatedAt, submittedAt));
        if (changed == 0) return null;
        application.Status = 2; // Submitted
        application.SubmittedAt = submittedAt;
        application.UpdatedAt = DateTime.UtcNow;

        await _welfareApplicationRepository.UpdateAsync(application);
        
        // Create status history entry
        var statusHistory = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = 2, // Submitted
            ChangedBy = application.UserId,
            ChangedAt = DateTime.UtcNow,
            Notes = "Application submitted"
        };
        
        _context.ApplicationStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync();

        await _anomalyDetectionService.DetectAllAnomaliesForApplicationAsync(id);
        
        // Fetch with navigation properties for notification
        var applicationWithNav = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        // Trigger notification
        if (applicationWithNav != null)
        {
            await _notificationTriggerService.TriggerApplicationSubmittedAsync(
                applicationWithNav.UserId,
                applicationWithNav.Id,
                applicationWithNav.WelfareProgram.Name
            );
        }
        
        if (transaction != null) await transaction.CommitAsync();
        return MapToResponse(applicationWithNav!);
    }

    public async Task<bool> CancelApplicationAsync(int id)
    {
        var application = await _welfareApplicationRepository.GetByIdAsync(id);
        if (application == null)
        {
            return false;
        }

        // Only allow canceling draft applications
        if (application.Status != 1) // Draft
        {
            return false;
        }

        _welfareApplicationRepository.Delete(application);
        return true;
    }

    private async Task Reassess(WelfareApplication application, string stage)
    {
        var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == application.UserId);
        var program = await _context.WelfarePrograms.AsNoTracking().FirstAsync(p => p.Id == application.WelfareProgramId);
        var at = DateTime.UtcNow;
        var result = _ruleEngineService.EvaluateEligibility(new RuleEvaluationContext {
            User = user, Program = program, RequestedAmount = application.RequestedAmount, ApplicationDate = at,
            UserApplications = await _context.WelfareApplications.AsNoTracking().Where(a => a.UserId == user.Id && a.Id != application.Id && a.Status != 10).ToListAsync()
        });
        var requirements = await SubmissionGuard.Check(_context, application);
        result.RuleResults.Add(new RuleEvaluationResult { RuleName = "Mandatory submission requirements", IsPassed = requirements.Count == 0,
            Message = requirements.Count == 0 ? "Active account/program, verified employment, service, amount and required evidence checks passed." : string.Join(" ", requirements), Details = new() { ["Mandatory"] = true } });
        result.IsEligible = result.IsEligible && requirements.Count == 0;
        result.WarningMessages.AddRange(requirements);
        application.IsEligible = result.IsEligible;
        application.AnomalyFlags = JsonSerializer.Serialize(result.WarningMessages);
        application.RuleEvaluationResult = JsonSerializer.Serialize(new {
            RuleEngine = result, AssessedAt = at, Stage = stage,
            Inputs = new { user.HireDate, user.IsActive, user.EmploymentVerifiedAt, application.RequestedAmount, ProgramId = program.Id, program.Name,
                program.MaxAmount, program.MinServicePeriod, program.RequiredDocuments, ProgramUpdatedAt = program.UpdatedAt }
        });
        _context.AuditLogs.Add(new AuditLog { UserId = application.UserId, Action = "EligibilityAssessed", EntityType = "WelfareApplication",
            EntityId = application.Id.ToString(), Details = application.RuleEvaluationResult });
    }

    private ApplicationResponse MapToResponse(WelfareApplication application)
    {
        string statusName = application.Status switch
        {
            1 => "Draft",
            2 => "Submitted",
            3 => "Under HR Review",
            4 => "Under Committee Review",
            5 => "Approved by Committee",
            6 => "Rejected by Committee",
            7 => "Pending Management Approval",
            8 => "Approved",
            9 => "Rejected",
            10 => "Cancelled",
            _ => "Unknown"
        };

        return new ApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            UserName = $"{application.User?.FirstName} {application.User?.LastName}",
            WelfareProgramId = application.WelfareProgramId,
            ProgramName = application.WelfareProgram?.Name ?? string.Empty,
            Status = application.Status,
            StatusName = statusName,
            RequestedAmount = application.RequestedAmount,
            Reason = application.Reason,
            IsEligible = application.IsEligible,
            AnomalyFlags = application.AnomalyFlags,
            RuleEvaluationResult = application.RuleEvaluationResult,
            SubmittedAt = application.SubmittedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}
