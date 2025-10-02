using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class TeamRepresentativeMemberRepository : GenericRepository<TeamRepresentativeMember>, ITeamRepresentativeMemberRepository
    {
        public TeamRepresentativeMemberRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
