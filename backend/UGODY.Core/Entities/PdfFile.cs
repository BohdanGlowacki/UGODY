namespace UGODY.Core.Entities;

public class PdfFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
    public string Hash { get; set; } = string.Empty; // SHA256 hash for deduplication
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }

    // Navigation property
    public virtual ICollection<OcrResult> OcrResults { get; set; } = new List<OcrResult>();
}
