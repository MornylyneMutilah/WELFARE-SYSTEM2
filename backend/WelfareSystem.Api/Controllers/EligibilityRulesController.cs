using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Models.DTOs.EligibilityRule;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EligibilityRulesController : ControllerBase
{
    private readonly IEligibilityRuleService _eligibilityRuleService;

    public EligibilityRulesController(IEligibilityRuleService eligibilityRuleService)
    {
        _eligibilityRuleService = eligibilityRuleService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRuleById(int id)
    {
        var rule = await _eligibilityRuleService.GetRuleByIdAsync(id);
        if (rule == null)
        {
            return NotFound(new { message = "Eligibility rule not found" });
        }

        return Ok(rule);
    }

    [HttpGet("program/{programId}")]
    public async Task<IActionResult> GetRulesByProgramId(int programId)
    {
        var rules = await _eligibilityRuleService.GetRulesByProgramIdAsync(programId);
        return Ok(rules);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateRule([FromBody] CreateEligibilityRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var rule = await _eligibilityRuleService.CreateRuleAsync(request);
        if (rule == null)
        {
            return BadRequest(new { message = "Rule creation failed. Welfare program may not exist." });
        }

        return CreatedAtAction(nameof(GetRuleById), new { id = rule.Id }, rule);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] UpdateEligibilityRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var rule = await _eligibilityRuleService.UpdateRuleAsync(id, request);
        if (rule == null)
        {
            return BadRequest(new { message = "Update failed. Rule not found." });
        }

        return Ok(rule);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateRule(int id)
    {
        var success = await _eligibilityRuleService.DeactivateRuleAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Eligibility rule not found" });
        }

        return Ok(new { message = "Eligibility rule deactivated successfully" });
    }
}
