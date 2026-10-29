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
        
        // Fetch with navigation properties
        var applicationWithNav = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == createdApplication!.Id);
        
        return MapToResponse(applicationWithNav!);
    }

    public async Task<ApplicationResponse?> UpdateApplicationAsync(int id, UpdateApplicationRequest request)
    {
        var application = await _welfareApplicationRepository.GetByIdAsync(id);
        if (application == null)
        {
            return null;
        }

        // Only allow updating draft applications
        if (application.Status != 1) // Draft
        {
            return null;
        }

        application.RequestedAmount = request.RequestedAmount;
        application.Reason = request.Reason;
        application.UpdatedAt = DateTime.UtcNow;

        await _welfareApplicationRepository.UpdateAsync(application);
        
        // Fetch with navigation properties
        var applicationWithNav = await _context.WelfareApplications
            .Include(a => a.User)
            .Include(a => a.WelfareProgram)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        return MapToResponse(applicationWithNav!);
    }

    public async Task<ApplicationResponse?> SubmitApplicationAsync(int id)
    {
        var application = await _welfareApplicationRepository.GetByIdAsync(id);
        if (application == null)
        {
            return null;
        }

        // Only allow submitting draft applications
        if (application.Status != 1) // Draft
        {
            return null;
        }

        application.Status = 2; // Submitted
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
