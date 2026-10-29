using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Models.DTOs.WelfareProgram;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WelfareProgramsController : ControllerBase
{
    private readonly IWelfareProgramService _welfareProgramService;

    public WelfareProgramsController(IWelfareProgramService welfareProgramService)
    {
        _welfareProgramService = welfareProgramService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPrograms()
    {
        var programs = await _welfareProgramService.GetAllProgramsAsync();
        return Ok(programs);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActivePrograms()
    {
        var programs = await _welfareProgramService.GetActiveProgramsAsync();
        return Ok(programs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgramById(int id)
    {
        var program = await _welfareProgramService.GetProgramByIdAsync(id);
        if (program == null)
        {
            return NotFound(new { message = "Welfare program not found" });
        }

        return Ok(program);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateProgram([FromBody] CreateWelfareProgramRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var program = await _welfareProgramService.CreateProgramAsync(request);
        if (program == null)
        {
            return BadRequest(new { message = "Program creation failed" });
        }

        return CreatedAtAction(nameof(GetProgramById), new { id = program.Id }, program);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateProgram(int id, [FromBody] UpdateWelfareProgramRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var program = await _welfareProgramService.UpdateProgramAsync(id, request);
        if (program == null)
        {
            return BadRequest(new { message = "Update failed. Program not found." });
        }

        return Ok(program);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateProgram(int id)
    {
        var success = await _welfareProgramService.DeactivateProgramAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Welfare program not found" });
        }

        return Ok(new { message = "Welfare program deactivated successfully" });
    }
}
