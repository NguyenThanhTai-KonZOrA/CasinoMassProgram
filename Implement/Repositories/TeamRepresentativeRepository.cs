using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class TeamRepresentativeRepository : GenericRepository<TeamRepresentative>, ITeamRepresentativeRepository
    {
        public TeamRepresentativeRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
