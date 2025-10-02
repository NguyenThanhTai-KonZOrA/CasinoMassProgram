using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class ImportCellErrorRepository : GenericRepository<ImportCellError>, IImportCellErrorRepository
    {
        public ImportCellErrorRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
