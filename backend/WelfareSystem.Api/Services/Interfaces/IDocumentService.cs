using WelfareSystem.Api.Models.DTOs.Document;

namespace WelfareSystem.Api.Services.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponse?> GetDocumentByIdAsync(int id);
    Task<IEnumerable<DocumentResponse>> GetDocumentsByApplicationIdAsync(int applicationId);
    Task<DocumentResponse?> UploadDocumentAsync(int applicationId, string fileName, string fileType, long fileSize, string filePath);
    Task<bool> DeleteDocumentAsync(int id);
}
