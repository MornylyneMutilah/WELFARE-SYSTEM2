using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;
using WelfareSystem.Api.Services.Rules;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EligibilityController : ControllerBase
{
    private readonly IRuleEngineService _ruleEngineService;
    private readonly AppDbContext _context;

    public EligibilityController(
        IRuleEngineService ruleEngineService,
        AppDbContext context)
    {
        _ruleEngineService = ruleEngineService;
        _context = context;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckEligibility([FromBody] EligibilityCheckRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // Get user with navigation properties
        var user = await _context.Users
            .Include(u => u.Applications)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Get program
        var program = await _context.WelfarePrograms
            .FirstOrDefaultAsync(p => p.Id == request.WelfareProgramId);
        
        if (program == null)
        {
            return NotFound(new { message = "Welfare program not found" });
        }

        // Create evaluation context
        var context = new RuleEvaluationContext
        {
            User = user,
            Program = program,
            RequestedAmount = request.RequestedAmount,
            ApplicationDate = DateTime.UtcNow,
            UserApplications = user.Applications?.ToList() ?? new List<WelfareApplication>()
        };

        // Evaluate eligibility
        var result = _ruleEngineService.EvaluateEligibility(context);

        return Ok(result);
    }
}

public class EligibilityCheckRequest
{
    public int WelfareProgramId { get; set; }
    public decimal RequestedAmount { get; set; }
}
