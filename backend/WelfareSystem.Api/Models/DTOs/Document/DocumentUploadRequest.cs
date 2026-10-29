namespace WelfareSystem.Api.Models.DTOs.Document;

public class DocumentUploadRequest
{
    public int ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
