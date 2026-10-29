using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WelfareSystem.Api.Models.DTOs.Document;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocumentById(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        return Ok(document);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<IActionResult> GetDocumentsByApplicationId(int applicationId)
    {
        var documents = await _documentService.GetDocumentsByApplicationIdAsync(applicationId);
        return Ok(documents);
    }

    [HttpPost("application/{applicationId}")]
    public async Task<IActionResult> UploadDocument(int applicationId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file size (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds 10MB limit" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { message = "Invalid file type. Allowed types: PDF, DOC, DOCX, JPG, JPEG, PNG" });
        }

        // Generate unique file name
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine("uploads", uniqueFileName);

        // Save file (in production, this would use cloud storage)
        try
        {
            // For now, we'll just store the metadata
            // In a real implementation, you would save the file to disk or cloud storage
            var document = await _documentService.UploadDocumentAsync(
                applicationId,
                file.FileName,
                file.ContentType,
                file.Length,
                filePath
            );

            if (document == null)
            {
                return BadRequest(new { message = "Document upload failed" });
            }

            return CreatedAtAction(nameof(GetDocumentById), new { id = document.Id }, document);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var success = await _documentService.DeleteDocumentAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Document not found" });
        }

        return Ok(new { message = "Document deleted successfully" });
    }
}
