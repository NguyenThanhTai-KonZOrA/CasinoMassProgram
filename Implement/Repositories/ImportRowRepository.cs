using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class ImportRowRepository : GenericRepository<ImportRow>, IImportRowRepository
    {
        public ImportRowRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
