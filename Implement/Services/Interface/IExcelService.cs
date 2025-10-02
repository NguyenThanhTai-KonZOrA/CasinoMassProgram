using Implement.ViewModels.Response;
using Microsoft.AspNetCore.Http;

namespace Implement.Services.Interface
{
    public interface IExcelService
    {
        Task<ImportSummaryResponse> ImportAndValidateAsync(IFormFile file);
        Task<ApprovedImportResponse> ApprovedImport(Guid batchId);
        Task<ImportSummaryResponse> GetBatchSummaryAsync(Guid batchId);
        Task<(byte[] Content, string FileName, string ContentType)> DownloadAnnotatedAsync(Guid batchId);
        Task<ImportDetailsResponse> GetBatchDetailsAsync(Guid batchId);
        Task<ImportDetailsResponse> GetBatchDetailsPagingAsync(Guid batchId, int page = 1, int pageSize = 50);
    }
}
