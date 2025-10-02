using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class ImportBatchRepository : GenericRepository<ImportBatch>, IImportBatchRepository
    {
        public ImportBatchRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
