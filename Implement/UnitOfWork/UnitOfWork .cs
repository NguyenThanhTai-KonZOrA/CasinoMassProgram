using Common.Repository;
using Implement.ApplicationDbContext;
using Implement.EntityModels;
using Implement.Repositories;

namespace Implement.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CasinoMassProgramDbContext _context;
        public UnitOfWork(CasinoMassProgramDbContext context)
        {
            _context = context;
            AwardSettlement = new GenericRepository<AwardSettlement>(_context);
            ImportBatch = new GenericRepository<ImportBatch>(_context);
            ImportCellError = new GenericRepository<ImportCellError>(_context);
            ImportRow = new GenericRepository<ImportRow>(_context);
            Member = new GenericRepository<Member>(_context);
            TeamRepresentative = new GenericRepository<TeamRepresentative>(_context);
            TeamRepresentativeMember = new GenericRepository<TeamRepresentativeMember>(_context);
        }
        public IGenericRepository<AwardSettlement> AwardSettlement { get; }

        public IGenericRepository<ImportBatch> ImportBatch { get; }

        public IGenericRepository<ImportCellError> ImportCellError { get; }

        public IGenericRepository<ImportRow> ImportRow { get; }

        public IGenericRepository<Member> Member { get; }

        public IGenericRepository<TeamRepresentative> TeamRepresentative { get; }

        public IGenericRepository<TeamRepresentativeMember> TeamRepresentativeMember { get; }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
