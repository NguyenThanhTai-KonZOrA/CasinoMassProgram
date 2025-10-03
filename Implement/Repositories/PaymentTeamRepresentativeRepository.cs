using Common.Repository;
using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implement.Repositories
{
    public class PaymentTeamRepresentativeRepository : GenericRepository<PaymentTeamRepresentative>, IPaymentTeamRepresentativeRepository
    {
        public PaymentTeamRepresentativeRepository(CasinoMassProgramDbContext context) : base(context)
        {
        }
    }
}
