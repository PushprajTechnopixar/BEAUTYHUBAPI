using BeautyHubAPI.Models;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IServiceRepository
    {
        Task<List<serviceDetailDTO>> GetSalonServiceDetail(int serviceId, string? serviceType);
    }
}
