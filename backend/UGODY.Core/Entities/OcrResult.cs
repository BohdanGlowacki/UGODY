namespace UGODY.Core.Entities;

public enum ProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public class OcrResult
{
    public int Id { get; set; }
    public int PdfFileId { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public DateTime ProcessedDate { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation property
    public virtual PdfFile PdfFile { get; set; } = null!;
}
