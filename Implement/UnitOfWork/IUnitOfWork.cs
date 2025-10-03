using Common.Repository;
using Implement.EntityModels;

namespace Implement.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<AwardSettlement> AwardSettlement { get; }
        IGenericRepository<ImportBatch> ImportBatch { get; }
        IGenericRepository<ImportCellError> ImportCellError { get; }
        IGenericRepository<ImportRow> ImportRow { get; }
        IGenericRepository<Member> Member { get; }
        IGenericRepository<TeamRepresentative> TeamRepresentative { get; }
        IGenericRepository<TeamRepresentativeMember> TeamRepresentativeMember { get; }
        IGenericRepository<PaymentTeamRepresentative> PaymentTeamRepresentative { get; }
        Task<int> CompleteAsync();
        void Update();
        void UpdateRange();
    }
}
