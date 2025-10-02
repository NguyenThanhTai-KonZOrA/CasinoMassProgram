using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class AwardSettlementRepository : GenericRepository<AwardSettlement>, IAwardSettlementRepository
    {
        public AwardSettlementRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
