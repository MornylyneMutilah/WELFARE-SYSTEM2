using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WelfareSystem.Api.Models.DTOs.Review;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReviewById(int id)
    {
        var review = await _reviewService.GetReviewByIdAsync(id);
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        return Ok(review);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<IActionResult> GetReviewsByApplicationId(int applicationId)
    {
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
            return BadRequest(new { message = "Review creation failed. Application may not exist or you may have already reviewed this application." });
        }

        return CreatedAtAction(nameof(GetReviewById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "HRAndAbove")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var review = await _reviewService.UpdateReviewAsync(id, request);
        if (review == null)
        {
            return BadRequest(new { message = "Update failed. Review not found." });
        }

        return Ok(review);
    }
}
