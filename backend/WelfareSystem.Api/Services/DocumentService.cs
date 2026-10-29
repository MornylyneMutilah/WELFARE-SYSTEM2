using WelfareSystem.Api.Models.DTOs.Document;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Repositories.Interfaces;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly IApplicationDocumentRepository _documentRepository;

    public DocumentService(IApplicationDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<DocumentResponse?> GetDocumentByIdAsync(int id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            return null;
        }

        return MapToResponse(document);
    }

    public async Task<IEnumerable<DocumentResponse>> GetDocumentsByApplicationIdAsync(int applicationId)
    {
        var documents = await _documentRepository.GetByApplicationIdAsync(applicationId);
        return documents.Select(MapToResponse);
    }

    public async Task<DocumentResponse?> UploadDocumentAsync(int applicationId, string fileName, string fileType, long fileSize, string filePath)
    {
        var document = new ApplicationDocument
        {
            ApplicationId = applicationId,
            FileName = fileName,
            FileType = fileType,
            FileSize = fileSize,
            FilePath = filePath,
            UploadedAt = DateTime.UtcNow
        };

        var createdDocument = await _documentRepository.AddAsync(document);
        return MapToResponse(createdDocument);
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            return false;
        }

        _documentRepository.Delete(document);
        return true;
    }

    private DocumentResponse MapToResponse(ApplicationDocument document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            ApplicationId = document.ApplicationId,
            FileName = document.FileName,
            FileType = document.FileType,
            FileSize = document.FileSize,
            FilePath = document.FilePath,
            UploadedAt = document.UploadedAt
        };
    }
}
