namespace UGODY.Application.Services;

public interface IOcrQueueService
{
    void EnqueuePdfFile(int pdfFileId);
}
