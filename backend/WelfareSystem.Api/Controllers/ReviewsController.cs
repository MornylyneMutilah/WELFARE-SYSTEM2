using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.Review;
using WelfareSystem.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly AppDbContext _context;

    public ReviewsController(IReviewService reviewService, AppDbContext context)
    {
        _reviewService = reviewService;
        _context = context;
    }

    private int ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<bool> CanRead(int applicationId)
    {
        var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == ActorId && u.IsActive);
        if (actor == null) return false;
        return await _context.WelfareApplications.AnyAsync(a => a.Id == applicationId &&
            (a.UserId == actor.Id || actor.Role.Name == "Administrator" || actor.Role.Name == "CommitteeMember" ||
             actor.Role.Name == "Management" || (actor.Role.Name == "HROfficer" && a.User.DepartmentId == actor.DepartmentId)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReviewById(int id)
    {
        var review = await _reviewService.GetReviewByIdAsync(id);
        if (review == null || !await CanRead(review.ApplicationId))
        {
            return NotFound(new { message = "Review not found" });
        }

        return Ok(review);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<IActionResult> GetReviewsByApplicationId(int applicationId)
    {
        if (!await CanRead(applicationId)) return NotFound();
        var reviews = await _reviewService.GetReviewsByApplicationIdAsync(applicationId);
        return Ok(reviews);
    }

    [HttpPost("application/{applicationId}")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> CreateReview(int applicationId, [FromBody] CreateReviewRequest request)
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

        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
        {
            return Unauthorized(new { message = "Invalid user role" });
        }

        var review = await _reviewService.CreateReviewAsync(applicationId, userId, roleClaim.Value, request);
        if (review == null)
        {
            return BadRequest(new { message = "Review not saved. Check that this application is awaiting your role, belongs to your department (HR), is not your own, and has not already been reviewed at this stage." });
        }

        return CreatedAtAction(nameof(GetReviewById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
    {
        var existing = await _reviewService.GetReviewByIdAsync(id);
        if (existing == null || existing.ReviewerId != ActorId || !await CanRead(existing.ApplicationId)) return NotFound();
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var review = await _reviewService.UpdateReviewAsync(id, request);
        if (review == null)
        {
            return BadRequest(new { message = "Recorded decisions and review notes cannot be overwritten. The original confirmation is retained for audit." });
        }

        return Ok(review);
    }
}
