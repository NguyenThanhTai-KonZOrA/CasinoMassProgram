using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;

namespace Implement.Repositories
{
    public class MemberRepository : GenericRepository<Member>, IMemberRepository
    {
        public MemberRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
