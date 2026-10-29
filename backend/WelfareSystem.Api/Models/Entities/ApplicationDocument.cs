namespace WelfareSystem.Api.Models.Entities;

public class ApplicationDocument
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public WelfareApplication Application { get; set; } = null!;
}
