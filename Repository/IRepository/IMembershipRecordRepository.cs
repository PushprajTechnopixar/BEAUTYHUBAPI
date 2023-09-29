using BeautyHubAPI.Models;
using System.Linq.Expressions;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IMembershipRecordRepository : IRepository<MembershipRecord>
    {
        Task<List<MembershipRecord>> UpdateMembershipRecord(List<MembershipRecord> entity);
    }
}
